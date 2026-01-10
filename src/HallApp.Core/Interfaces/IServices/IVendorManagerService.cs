using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// Service interface for VendorManager operations
/// VendorManager is just a link between AppUser and managed Vendors
/// Business properties (approval, commercial reg, VAT) are on Vendor entity
/// </summary>
public interface IVendorManagerService
{
    // Core CRUD operations
    Task<VendorManager> CreateVendorManagerAsync(VendorManager vendorManager);
    Task<VendorManager> UpdateVendorManagerAsync(VendorManager vendorManager);
    Task<bool> DeleteVendorManagerAsync(int vendorManagerId);
    Task<VendorManager> GetVendorManagerByIdAsync(int vendorManagerId);
    Task<VendorManager> GetVendorManagerByAppUserIdAsync(int appUserId);
    Task<List<VendorManager>> GetAllVendorManagersAsync();
    
    // Business relationships
    Task<int> GetVendorManagerVendorCountAsync(int vendorManagerId);
}
