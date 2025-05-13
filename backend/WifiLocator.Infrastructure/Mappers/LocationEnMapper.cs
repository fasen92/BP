using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Infrastructure.Entities;

namespace WifiLocator.Infrastructure.Mappers
{
    public class LocationEnMapper : IMapper<LocationEntity>
    {
        public void MapToEntity(LocationEntity entity, LocationEntity existingEntity)
        {
            existingEntity.Id = entity.Id;
            existingEntity.WifiId = entity.WifiId;
            existingEntity.Longitude = entity.Longitude;
            existingEntity.Latitude = entity.Latitude;
            existingEntity.Accuracy = entity.Accuracy;
            existingEntity.Altitude = entity.Altitude;
            existingEntity.EncryptionValue = entity.EncryptionValue;
            existingEntity.Seen = entity.Seen;
            existingEntity.SignaldBm = entity.SignaldBm;
            existingEntity.UsedForApproximation = entity.UsedForApproximation;
        }

    }
}
