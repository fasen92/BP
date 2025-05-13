using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Core.Models;


namespace WifiLocator.Core.Services.Interfaces
{
    public interface IGeoService
    {
        Task<AddressModel> GetAddressFromLocation(WifiModel wifiModel);
    }
}
