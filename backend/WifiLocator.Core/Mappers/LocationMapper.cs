using WifiLocator.Core.Models;
using WifiLocator.Infrastructure.Entities;

namespace WifiLocator.Core.Mappers
{
    public class LocationMapper : ModelMapper<LocationEntity, LocationModel>
    {
        public LocationModel MapToModel(LocationEntity? entity)
        {
            if (entity is null)
            {
                return LocationModel.Empty;
            }
            else
            {
                return new LocationModel
                {
                    Id = entity.Id,
                    Altitude = entity.Altitude,
                    Latitude = entity.Latitude,
                    Longitude = entity.Longitude,
                    Accuracy = entity.Accuracy,
                    SignaldBm = entity.SignaldBm,
                    FrequencyMHz = entity.FrequencyMHz,
                    Seen = entity.Seen,
                    EncryptionValue = entity.EncryptionValue,
                    UsedForApproximation = entity.UsedForApproximation,
                };
            }  
        }

        public LocationEntity MapToEntity(LocationModel model, Guid joinId)
        {
            return new LocationEntity
            {
                Id = model.Id,
                Altitude = model.Altitude,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Accuracy = model.Accuracy,
                SignaldBm = model.SignaldBm,
                FrequencyMHz = model.FrequencyMHz,
                Seen = DateTime.SpecifyKind(model.Seen, DateTimeKind.Utc),
                EncryptionValue = model.EncryptionValue,
                WifiId = joinId,
                UsedForApproximation = model.UsedForApproximation,
            };
        }

        public LocationEntity MapToEntity(LocationModel model)
        {
            throw new NotImplementedException("Unsupported, use the overload");
        }
    }
}
