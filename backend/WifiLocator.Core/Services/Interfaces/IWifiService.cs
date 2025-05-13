using WifiLocator.Core.Models;

namespace WifiLocator.Core.Services.Interfaces
{
    public interface IWifiService
    {
        Task ProcessApproximationAsync(IEnumerable<LocationModel> newLocations, IEnumerable<WifiModel> newWifiModels);
        Task<WifiModel> SaveAsync(WifiModel model, Guid? addressId);
        Task<List<WifiDisplayModel>> GetByFiltersAsync(FilterRequest filter);
        Task<List<WifiDisplayModel>> GetByRangeAsync(RangeRequest request);
        Task<List<WifiModel>> GetByMissingLocationAsync();
    }
}
