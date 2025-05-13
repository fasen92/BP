using WifiLocator.Core.Mappers;
using WifiLocator.Core.Models;
using WifiLocator.Core.Services.Interfaces;
using WifiLocator.Infrastructure.UnitOfWork;
using WifiLocator.Infrastructure.Entities;
using WifiLocator.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace WifiLocator.Core.Services
{
    public class LocationService(
        IUnitOfWorkFactory unitOfWorkFactory,
        ModelMapper<LocationEntity, LocationModel> locationMapper) : ILocationService
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory = unitOfWorkFactory;
        private readonly ModelMapper<LocationEntity, LocationModel> _locationMapper = locationMapper;

        public async Task<LocationModel> SaveAsync(LocationModel model, Guid wifiId)
        {
            LocationEntity entity = _locationMapper.MapToEntity(model, wifiId);
            LocationModel returnModel;

            using IUnitOfWork unitOfWork = _unitOfWorkFactory.CreateUnitOfWork();
            IRepository<LocationEntity> repository = unitOfWork.GetRepository<LocationEntity>();

            if (await repository.ExistsAsync(entity)) {
                await repository.Update(entity);
                returnModel = _locationMapper.MapToModel(entity);
            }
            else
            {
                LocationEntity addedEntity = await repository.AddAsync(entity);
                returnModel = _locationMapper.MapToModel(addedEntity);
            }

            await unitOfWork.SaveChangesAsync();
            return returnModel;
        }

        public async Task<List<LocationModel>> SaveAllAsync(Dictionary<Guid, List<LocationModel>> locationMap)
        {
            using IUnitOfWork unitOfWork = _unitOfWorkFactory.CreateUnitOfWork();
            IRepository<LocationEntity> repository = unitOfWork.GetRepository<LocationEntity>();

            List<LocationEntity> newEntities = [];
            List<LocationEntity> returnEntities = [];

            foreach (var pair in locationMap)
            {
                Guid wifiId = pair.Key;
                foreach (var location in pair.Value)
                {
                    LocationEntity entity = _locationMapper.MapToEntity(location, wifiId);

                    if (entity.Id != Guid.Empty && await repository.ExistsAsync(entity))
                    {
                        LocationEntity updatedEntity = await repository.Update(entity);
                        returnEntities.Add(updatedEntity);
                    }
                    else
                    {
                        newEntities.Add(entity);
                    }
                }
            }

            if (newEntities.Count != 0)
            {
                var addedEntities = await repository.AddRangeAsync(newEntities);
                returnEntities.AddRange(addedEntities);
            }

            await unitOfWork.SaveChangesAsync();

            return returnEntities.Select(_locationMapper.MapToModel).ToList();
        }

        public async Task<List<LocationModel>> GetLocationsByBssidAsync(string bssid)
        {
            using IUnitOfWork unitOfWork = _unitOfWorkFactory.CreateUnitOfWork();
            IRepository<LocationEntity> locationRepo = unitOfWork.GetRepository<LocationEntity>();

            var locations = await locationRepo.Query()
                .Where(location => location.UsedForApproximation && location.Wifi != null && location.Wifi.Bssid == bssid)
                .Include(loc => loc.Wifi) 
                .ToListAsync();

            return locations.Select(_locationMapper.MapToModel).OrderByDescending(location => location.SignaldBm).Take(15).ToList();
        }
    }
}
