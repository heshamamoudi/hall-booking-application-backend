using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class ServiceItemRepository : IServiceItemRepository
{
    private readonly DataContext _context;

    public ServiceItemRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ServiceItem>> GetServiceItemsByVendorAsync(int vendorId)
    {
        return await _context.ServiceItems
            .Where(si => si.VendorId == vendorId)
            .OrderBy(si => si.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<ServiceItem>> GetServiceItemsByTypeAsync(string serviceType)
    {
        return await _context.ServiceItems
            .Where(si => si.ServiceType.ToLower() == serviceType.ToLower())
            .OrderBy(si => si.Name)
            .ToListAsync();
    }

    public async Task<ServiceItem> GetServiceItemByIdAsync(int id)
    {
        return await _context.ServiceItems
            .Include(si => si.Vendor)
            .FirstOrDefaultAsync(si => si.Id == id) ?? new ServiceItem();
    }

    public async Task<ServiceItem> GetByIdAsync(int id)
    {
        return await _context.ServiceItems
            .Include(si => si.Vendor)
            .FirstOrDefaultAsync(si => si.Id == id) ?? new ServiceItem();
    }

    public async Task<IEnumerable<ServiceItem>> GetAllAsync()
    {
        return await _context.ServiceItems
            .Include(si => si.Vendor)
            .ToListAsync();
    }

    public async Task<bool> ServiceItemExistsAsync(int vendorId, string name)
    {
        return await _context.ServiceItems
            .AnyAsync(si => si.VendorId == vendorId && si.Name.ToLower() == name.ToLower());
    }

    public async Task<IEnumerable<ServiceItem>> GetAvailableServiceItemsAsync(int vendorId, DateTime date)
    {
        return await _context.ServiceItems
            .Where(si => si.VendorId == vendorId && si.IsAvailable)
            .OrderBy(si => si.Name)
            .ToListAsync();
    }

    public void Add(ServiceItem serviceItem)
    {
        _context.ServiceItems.Add(serviceItem);
    }

    public void Update(ServiceItem serviceItem)
    {
        _context.ServiceItems.Update(serviceItem);
    }

    public void Delete(ServiceItem serviceItem)
    {
        _context.ServiceItems.Remove(serviceItem);
    }
}
