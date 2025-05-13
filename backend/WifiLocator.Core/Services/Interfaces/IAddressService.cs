using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Core.Mappers;
using WifiLocator.Core.Models;
using WifiLocator.Infrastructure.Entities;
using WifiLocator.Infrastructure.Repositories;

namespace WifiLocator.Core.Services.Interfaces
{
    public interface IAddressService
    {
        Task<AddressModel> SaveAsync(AddressModel model);
        Task<AddressModel> GetByAddressAsync(AddressModel model);
    }
}
