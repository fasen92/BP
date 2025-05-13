using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Infrastructure.Entities;

namespace WifiLocator.Infrastructure.Mappers
{
    public class WifiEnMapper : IMapper<WifiEntity>
    {
        public void MapToEntity(WifiEntity entity, WifiEntity existingEntity)
        {
            existingEntity.Id = entity.Id;
            existingEntity.Bssid = entity.Bssid;
            existingEntity.Ssid = entity.Ssid;
            existingEntity.ApproximatedLatitude = entity.ApproximatedLatitude;
            existingEntity.ApproximatedLongitude = entity.ApproximatedLongitude;
            existingEntity.AddressId = entity.AddressId;
            existingEntity.UncertaintyRadius = entity.UncertaintyRadius;
            existingEntity.Channel = entity.Channel;
        }
    }
}
