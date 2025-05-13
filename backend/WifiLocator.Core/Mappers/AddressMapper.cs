using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Infrastructure.Entities;
using WifiLocator.Core.Models;

namespace WifiLocator.Core.Mappers
{
    public class AddressMapper : ModelMapper<AddressEntity, AddressModel>
    {
        public AddressModel MapToModel(AddressEntity? entity)
        {
            if (entity is null)
            {
                return AddressModel.Empty;
            }
            else
            {
                return new AddressModel
                {
                    Id = entity.Id,
                    Country = entity.Country,
                    City = entity.City,
                    PostalCode = entity.PostalCode,
                    Region = entity.Region ?? string.Empty,
                    Road = entity.Road,
                };
            }
        }

        public AddressEntity MapToEntity(AddressModel model, Guid joinId)
        {
            throw new NotImplementedException("Unsupported, use the overload");
        }

        public AddressEntity MapToEntity(AddressModel model)
        {
            return new AddressEntity
            {
                Id = model.Id,
                City = model.City,
                Country = model.Country,
                PostalCode = model.PostalCode,
                Region = model.Region,
                Road= model.Road,
            };
        }
    }
}
