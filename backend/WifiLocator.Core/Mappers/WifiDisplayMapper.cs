using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Core.Models;
using WifiLocator.Infrastructure.Entities;

namespace WifiLocator.Core.Mappers
{
    public class WifiDisplayMapper : ModelMapper<WifiEntity, WifiDisplayModel>
    {
        public WifiDisplayModel MapToModel(WifiEntity? entity)
        {
            if (entity is null)
            {
                return WifiDisplayModel.Empty;
            }
            else
            {
                return new WifiDisplayModel
                {
                    Ssid = entity.Ssid,
                    Bssid = entity.Bssid,
                    ApproximatedLatitude = entity.ApproximatedLatitude,
                    ApproximatedLongitude = entity.ApproximatedLongitude,
                    Encryption = entity.Encryption,
                    Channel = entity.Channel,
                    FirstSeen = entity.Locations.Count != 0 ? entity.Locations.Min(loc => loc.Seen) : DateTime.MinValue,
                    LastSeen = entity.Locations.Count != 0 ? entity.Locations.Max(loc => loc.Seen) : DateTime.MinValue,
                    Address = entity.Address != null ? $"{entity.Address.Country}, {entity.Address.City}, {entity.Address.Road}" : String.Empty,
                    UncertaintyRadius = entity.UncertaintyRadius,
                };
            }
        }

        public WifiEntity MapToEntity(WifiDisplayModel model, Guid joinId) 
        { 
            throw new NotImplementedException("Method not supported, cannot map from DisplayModel to Entity");
        }

        public WifiEntity MapToEntity(WifiDisplayModel model)
        {
            throw new NotImplementedException("Method not supported, cannot map from DisplayModel to Entity");
        }

    }
}
