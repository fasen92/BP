using Microsoft.EntityFrameworkCore;
using WifiLocator.Core.Mappers;
using WifiLocator.Core.Models;
using WifiLocator.Core.Services.Interfaces;
using WifiLocator.Infrastructure.Entities;
using WifiLocator.Infrastructure.Repositories;
using WifiLocator.Infrastructure.UnitOfWork;
using System.Net;
using System.Collections.Generic;
using WifiLocator.Core.Approximation.Interfaces;


namespace WifiLocator.Core.Services
{
    public class WifiService(
        IUnitOfWorkFactory unitOfWorkFactory,
        CompositeWifiMapper wifiMapper,
        ILocationService locationService,
        IWifiLocationApproximator weightedTrilat) : IWifiService
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory = unitOfWorkFactory;
        private readonly CompositeWifiMapper _wifiMapper = wifiMapper;
        private readonly ILocationService _locationService = locationService;
        private readonly IWifiLocationApproximator _weightedTrilat = weightedTrilat;
        private readonly string[] _navigationPaths = [nameof(WifiEntity.Locations), nameof(WifiEntity.Address)];

        public async Task<WifiModel> SaveAsync(WifiModel model, Guid? addressId)
        {
            WifiEntity entity = _wifiMapper.MapToEntity(model, addressId);
            WifiModel returnModel;

            using IUnitOfWork unitOfWork = _unitOfWorkFactory.CreateUnitOfWork();
            IRepository<WifiEntity> repository = unitOfWork.GetRepository<WifiEntity>();

            if (await repository.ExistsAsync(entity))
            {
                entity = await repository.Update(entity);
                returnModel = _wifiMapper.MapToWifiModel(entity);
            }
            else
            {
                WifiEntity addedEntity = await repository.AddAsync(entity);
                returnModel = _wifiMapper.MapToWifiModel(addedEntity);
            }

            await unitOfWork.SaveChangesAsync();
            return returnModel;
        }

        public async Task ProcessApproximationAsync(IEnumerable<LocationModel> newLocations, IEnumerable<WifiModel> newWifiModels)
        {
            if (newLocations == null || newWifiModels == null)
            {
                throw new ArgumentException("List cannot be empty");
            }

            var groupByWifi = await UpdateWifiModelsInGroup(newLocations, newWifiModels);


            var processWifiResults = groupByWifi
            .AsParallel()
            .Select(wifiGroup =>
            {
                WifiModel wifiModel = wifiGroup.Value.Wifi;
                List<LocationModel> filteredLocations = FilterDuplicateLocations(wifiModel, wifiGroup.Value.Locations);

                if (filteredLocations.Count == 0)
                {
                    return null;
                }

                foreach (LocationModel location in filteredLocations)
                {
                    wifiModel.Locations?.Add(location);
                }

                wifiModel = _weightedTrilat.PerformApproximation(wifiModel);
                wifiModel = CheckAddressId(wifiModel);
                wifiModel = CheckEncryption(wifiModel);

                return new
                {
                    WifiModel = wifiModel,
                    Locations = wifiModel.Locations?.ToList(),
                };
            })
            .Where(result => result != null)
            .ToList();

            List<WifiModel> savedModels = await SaveAllAsync(processWifiResults
                .Where(result => result != null)
                .Select(result => result!.WifiModel)
                .ToList());

            var savedByBssid = savedModels.ToDictionary(model => model.Bssid);

            Dictionary<Guid, List<LocationModel>> wifiLocationMap = processWifiResults
                .Where(entry => entry != null &&
                                entry.Locations != null &&
                                entry.WifiModel != null &&
                                entry.Locations.Count != 0)
                .GroupBy(entry => entry!.WifiModel.Bssid)
                .ToDictionary(
                    group => savedByBssid[group.Key].Id,  
                    group => group.SelectMany(entry => entry!.Locations!).ToList() // merge all locations for BSSID
                );

            await _locationService.SaveAllAsync(wifiLocationMap);
        }


        public async Task<List<WifiDisplayModel>> GetByFiltersAsync(FilterRequest filter)
        {
            using IUnitOfWork unitOfWork = _unitOfWorkFactory.CreateUnitOfWork();
            IRepository<WifiEntity> repository = unitOfWork.GetRepository<WifiEntity>();
            IQueryable<WifiEntity> query = repository.Query();

            query = query.Where(wifi =>
                wifi.ApproximatedLatitude >= filter.OuterBounds.Latitude1 &&
                wifi.ApproximatedLatitude <= filter.OuterBounds.Latitude2 &&
                wifi.ApproximatedLongitude >= filter.OuterBounds.Longitude1 &&
                wifi.ApproximatedLongitude <= filter.OuterBounds.Longitude2);

            if (filter.InnerBounds != null)
            {
                query = query.Where(wifi =>
                    wifi.ApproximatedLatitude < filter.InnerBounds.Latitude1 ||
                    wifi.ApproximatedLatitude > filter.InnerBounds.Latitude2 ||
                    wifi.ApproximatedLongitude < filter.InnerBounds.Longitude1 ||
                    wifi.ApproximatedLongitude > filter.InnerBounds.Longitude2
                );
            }

            if (!string.IsNullOrWhiteSpace(filter.Ssid))
            {
                query = query.Where(wifi => EF.Functions.Like(wifi.Ssid, $"%{filter.Ssid}%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.Bssid))
            {
                query = query.Where(wifi => wifi.Bssid.Equals(filter.Bssid, StringComparison.OrdinalIgnoreCase));
            }

            query = query.Include(nameof(WifiEntity.Address));

            List<WifiEntity> entities = await query.ToListAsync();
            List<WifiDisplayModel> filteredModels =  entities.Select(_wifiMapper.MapToDisplayModel).ToList();
            return filteredModels
                    .Where(m => (!filter.DateStart.HasValue || m.FirstSeen >= filter.DateStart.Value) &&
                                (!filter.DateEnd.HasValue || m.FirstSeen <= filter.DateEnd.Value))
                    .ToList();

        }

        public async Task<List<WifiModel>> GetByBssidsAsync(List<string> Bssids)
        {
            using IUnitOfWork unitOfWork = _unitOfWorkFactory.CreateUnitOfWork();
            IRepository<WifiEntity> repository = unitOfWork.GetRepository<WifiEntity>();
            IQueryable<WifiEntity> query = repository.Query();

            query = query.Where(wifi => Bssids.Contains(wifi.Bssid));

            foreach (string path in _navigationPaths)
            {
                query = query.Include(path);
            }

            List<WifiEntity> entities = await query.ToListAsync();

            return entities.Select(_wifiMapper.MapToWifiModel).ToList();
        }

        private async Task<Dictionary<string, (WifiModel Wifi, List<LocationModel> Locations)>> UpdateWifiModelsInGroup(
            IEnumerable<LocationModel> newLocations, IEnumerable<WifiModel> newWifiModels)
        {
            // If there are multiple instances of same wifi, group them to be processed all at once
            var groupByWifi = newLocations.Zip(newWifiModels, (location, wifi) => new { Wifi = wifi, Location = location })
                .GroupBy(item => item.Wifi.Bssid)
                .ToDictionary(
                    group => group.Key,
                    group => (group.Last().Wifi,
                        Locations: group.Select(e => e.Location).ToList()
                    ));

            List<string> Bssids = [.. groupByWifi.Keys];

            var existingWifiModels = (await GetByBssidsAsync(Bssids))
                .ToDictionary(w => w.Bssid, w => w);

            var fetchedGroup = new Dictionary<string, (WifiModel Wifi, List<LocationModel> Locations)>();

            foreach (var (Bssid, group) in groupByWifi)
            {
                if (existingWifiModels.TryGetValue(Bssid, out WifiModel? existingWifi))
                {
                    WifiModel existingModel = group.Wifi.Id == Guid.Empty ? existingWifi : group.Wifi;
                    fetchedGroup[Bssid] = (existingModel, group.Locations);
                }
                else
                {
                    // If no existing model, keep the current group data
                    fetchedGroup[Bssid] = group;
                }
            }

            return fetchedGroup;
        }

        public async Task<List<WifiDisplayModel>> GetByRangeAsync(RangeRequest request)
        {
            using IUnitOfWork unitOfWork = _unitOfWorkFactory.CreateUnitOfWork();
            IRepository<WifiEntity> repository = unitOfWork.GetRepository<WifiEntity>();
            IQueryable<WifiEntity> query = repository.Query();

            query = query.Where(wifi =>
                wifi.ApproximatedLatitude >= request.OuterBounds.Latitude1 &&
                wifi.ApproximatedLatitude <= request.OuterBounds.Latitude2 &&
                wifi.ApproximatedLongitude >= request.OuterBounds.Longitude1 &&
                wifi.ApproximatedLongitude <= request.OuterBounds.Longitude2);

            if (request.InnerBounds != null)
            {
                query = query.Where(wifi =>
                    wifi.ApproximatedLatitude < request.InnerBounds.Latitude1 ||
                    wifi.ApproximatedLatitude > request.InnerBounds.Latitude2 ||
                    wifi.ApproximatedLongitude < request.InnerBounds.Longitude1 ||
                    wifi.ApproximatedLongitude > request.InnerBounds.Longitude2
                );
            }

            foreach (string path in _navigationPaths)
            {
                query = query.Include(path);
            }

            List<WifiEntity> inRange = await query.ToListAsync();

            return inRange.Select(_wifiMapper.MapToDisplayModel).ToList();
        }

        public async Task<List<WifiModel>> GetByMissingLocationAsync()
        {
            using IUnitOfWork unitOfWork = _unitOfWorkFactory.CreateUnitOfWork();
            IRepository<WifiEntity> repository = unitOfWork.GetRepository<WifiEntity>();
            IQueryable<WifiEntity> query = repository.Query();

            query = query.Where(wifi =>
                 (wifi.AddressId == Guid.Empty ||
                 wifi.AddressId == null) &&
                 wifi.ApproximatedLatitude != null &&
                 wifi.ApproximatedLongitude != null)
                .Take(100);

            List<WifiEntity> missingRecords = await query.ToListAsync();
            return missingRecords.Select(_wifiMapper.MapToWifiModel).ToList();
        }

        public static WifiModel CheckEncryption(WifiModel model)
        {
            string? newEncryption = model.Locations?.OrderByDescending(location => location.Seen).FirstOrDefault()?.EncryptionValue;

            if (newEncryption != null && (newEncryption != model.Encryption)) {
                model.Encryption = newEncryption;
            }

            return model;
        }


        private async Task<List<WifiModel>> SaveAllAsync(IEnumerable<WifiModel> wifiModels)
        {
            List<WifiEntity> entities = wifiModels
                .Select(model => _wifiMapper.MapToEntity(model, model.Address?.Id == Guid.Empty ? null : model.Address?.Id))
                .ToList();
          
            using IUnitOfWork unitOfWork = _unitOfWorkFactory.CreateUnitOfWork();
            IRepository<WifiEntity> repository = unitOfWork.GetRepository<WifiEntity>();


            List<WifiEntity> savedEntities = await repository.BulkSaveAsync(entities);
            await unitOfWork.SaveChangesAsync();

            return savedEntities.Select(_wifiMapper.MapToWifiModel).ToList();
        }

        private static WifiModel CheckAddressId(WifiModel wifiModel)
        {
            if (wifiModel.Address == null)
            {

                wifiModel.Address = AddressModel.Empty;
            }

            return wifiModel;
        }

        private static List<LocationModel> FilterDuplicateLocations(WifiModel wifiModel, List<LocationModel> newLocations)
        {
            HashSet<string> locationKeys = wifiModel.Locations != null
                ? new HashSet<string>(wifiModel.Locations.Select(location => CreateLocationKey(location)))
                : [];

            List<LocationModel> filteredLocations = [];

            foreach (var newLoc in newLocations)
            {
                string key = CreateLocationKey(newLoc);
                if (!locationKeys.Contains(key))
                {
                    filteredLocations.Add(newLoc);
                    // add new location to prevent duplicates remaining locations
                    locationKeys.Add(key);
                }
            }

            return filteredLocations;
        }

        private static string CreateLocationKey(LocationModel location)
        {
            return $"{location.Latitude:F6}_{location.Longitude:F6}_{location.Altitude}_{location.Accuracy}_{location.SignaldBm}";
        }
    }
}
