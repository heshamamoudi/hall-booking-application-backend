using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IServiceItemRepository
{
    Task<IEnumerable<ServiceItem>> GetServiceItemsByVendorAsync(int vendorId);
    Task<IEnumerable<ServiceItem>> GetServiceItemsByTypeAsync(string serviceType);
    Task<ServiceItem> GetServiceItemByIdAsync(int id);
    Task<ServiceItem> GetByIdAsync(int id);
    Task<IEnumerable<ServiceItem>> GetAllAsync();
    Task<bool> ServiceItemExistsAsync(int vendorId, string name);
    Task<IEnumerable<ServiceItem>> GetAvailableServiceItemsAsync(int vendorId, DateTime date);
    void Add(ServiceItem serviceItem);
    void Update(ServiceItem serviceItem);
    void Delete(ServiceItem serviceItem);
}
