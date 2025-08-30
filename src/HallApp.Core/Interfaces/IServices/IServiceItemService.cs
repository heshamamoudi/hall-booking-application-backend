using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IServices;

public interface IServiceItemService
{
    Task<ServiceItem> CreateServiceItemAsync(int vendorId, ServiceItem serviceItem);
    Task<ServiceItem> UpdateServiceItemAsync(int id, ServiceItem serviceItem);
    Task<bool> DeleteServiceItemAsync(int id);
    Task<ServiceItem> GetServiceItemByIdAsync(int id);
    Task<List<ServiceItem>> GetServiceItemsByVendorAsync(int vendorId);
    Task<List<ServiceItem>> GetServiceItemsByTypeAsync(int vendorId, string serviceType);
    Task<List<ServiceItem>> GetAvailableServiceItemsAsync(int vendorId);
    Task<IEnumerable<ServiceItem>> GetAllServiceItemsAsync();
}
