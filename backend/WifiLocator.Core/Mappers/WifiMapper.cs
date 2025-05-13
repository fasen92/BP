using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Core.Models;
using WifiLocator.Infrastructure.Entities;

namespace WifiLocator.Core.Mappers
{
    public class WifiMapper(
        ModelMapper<LocationEntity, LocationModel> locationMapper,
        ModelMapper<AddressEntity, AddressModel> addressMapper
        ) : ModelMapper<WifiEntity, WifiModel>
    {
        private readonly ModelMapper<LocationEntity, LocationModel> _locationMapper = locationMapper;
        private readonly ModelMapper<AddressEntity, AddressModel> _addressMapper = addressMapper;

        public WifiModel MapToModel(WifiEntity? entity)
        {
            if(entity is null)
            {
                return WifiModel.Empty;
            }
            else
            {
                return new WifiModel
                {
                    Id = entity.Id,
                    Ssid = entity.Ssid,
                    Bssid = entity.Bssid,
                    ApproximatedLatitude = entity.ApproximatedLatitude,
                    ApproximatedLongitude = entity.ApproximatedLongitude,
                    UncertaintyRadius = entity.UncertaintyRadius,
                    Encryption = entity.Encryption,
                    Channel = entity.Channel,
                    FirstSeen = entity.Locations.Count != 0 ? entity.Locations.Min(loc => loc.Seen) : DateTime.MinValue,
                    LastSeen = entity.Locations.Count != 0 ? entity.Locations.Max(loc => loc.Seen) : DateTime.MinValue,
                    Locations = new ObservableCollection<LocationModel>(
                        entity.Locations.Select(_locationMapper.MapToModel).ToList()),
                    Address = _addressMapper.MapToModel(entity.Address)
                };
            }
        }

        public WifiEntity MapToEntity(WifiModel model, Guid? joinId)
        {
            return new WifiEntity
            {
                Id = model.Id,
                Ssid = model.Ssid,
                Bssid = model.Bssid,
                ApproximatedLatitude = model.ApproximatedLatitude,
                ApproximatedLongitude = model.ApproximatedLongitude,
                Encryption = model.Encryption,
                Channel = model.Channel,
                AddressId = joinId,
                UncertaintyRadius = model.UncertaintyRadius
            };
            
        }

        public WifiEntity MapToEntity(WifiModel model, Guid joinId)
        {
            throw new NotImplementedException("Unsupported, use the overload");
        }

        public WifiEntity MapToEntity(WifiModel model)
        {
            throw new NotImplementedException("Unsupported, use the overload");
        }
    }
}
