using WifiLocator.Core.Models;

namespace WifiLocator.Core.Services.Interfaces
{
    public interface ILocationService
    {
        Task<LocationModel> SaveAsync(LocationModel model, Guid aggregateId);
        Task<List<LocationModel>> SaveAllAsync(Dictionary<Guid, List<LocationModel>> locationMap);
        Task<List<LocationModel>> GetLocationsByBssidAsync(string ssid);
    }
}
