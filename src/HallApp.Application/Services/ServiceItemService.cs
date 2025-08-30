using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Application.Services;

public class ServiceItemService : IServiceItemService
{
    private readonly IUnitOfWork _unitOfWork;

    public ServiceItemService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceItem> CreateServiceItemAsync(int vendorId, ServiceItem serviceItem)
    {
        serviceItem.VendorId = vendorId;
        _unitOfWork.ServiceItemRepository.Add(serviceItem);
        await _unitOfWork.Complete();
        return serviceItem;
    }

    public async Task<ServiceItem> UpdateServiceItemAsync(int id, ServiceItem serviceItem)
    {
        var existingItem = await _unitOfWork.ServiceItemRepository.GetByIdAsync(id);
        if (existingItem == null) throw new Exception($"ServiceItem with id {id} not found");
        
        existingItem.Name = serviceItem.Name;
        existingItem.Description = serviceItem.Description;
        existingItem.Price = serviceItem.Price;
        existingItem.IsAvailable = serviceItem.IsAvailable;
        existingItem.ServiceType = serviceItem.ServiceType;
        
        await _unitOfWork.Complete();
        return existingItem;
    }

    public async Task<List<ServiceItem>> GetServiceItemsByVendorAsync(int vendorId)
    {
        var items = await _unitOfWork.ServiceItemRepository.GetServiceItemsByVendorAsync(vendorId);
        return items.ToList();
    }

    public async Task<List<ServiceItem>> GetServiceItemsByTypeAsync(int vendorId, string serviceType)
    {
        var allItems = await _unitOfWork.ServiceItemRepository.GetServiceItemsByTypeAsync(serviceType);
        return allItems.Where(item => item.VendorId == vendorId).ToList();
    }

    public async Task<List<ServiceItem>> GetAvailableServiceItemsAsync(int vendorId)
    {
        var items = await _unitOfWork.ServiceItemRepository.GetAvailableServiceItemsAsync(vendorId, DateTime.Now);
        return items.ToList();
    }

    public async Task<ServiceItem> GetServiceItemByIdAsync(int id)
    {
        return await _unitOfWork.ServiceItemRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<ServiceItem>> GetAllServiceItemsAsync()
    {
        return await _unitOfWork.ServiceItemRepository.GetAllAsync();
    }

    public async Task<bool> DeleteServiceItemAsync(int id)
    {
        var item = await _unitOfWork.ServiceItemRepository.GetByIdAsync(id);
        if (item == null) return false;

        _unitOfWork.ServiceItemRepository.Delete(item);
        await _unitOfWork.Complete();
        return true;
    }
}
