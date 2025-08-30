using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// Service interface for VendorManager business domain operations only
/// Focuses purely on VendorManager entity business logic
/// </summary>
public interface IVendorManagerService
{
    // Core CRUD operations
    Task<VendorManager> CreateVendorManagerAsync(VendorManager vendorManager);
    Task<VendorManager?> UpdateVendorManagerAsync(VendorManager vendorManager);
    Task<bool> DeleteVendorManagerAsync(int vendorManagerId);
    Task<VendorManager?> GetVendorManagerByIdAsync(int vendorManagerId);
    Task<VendorManager?> GetVendorManagerByAppUserIdAsync(int appUserId);
    Task<List<VendorManager>> GetAllVendorManagersAsync();
    
    // Business domain operations
    Task<bool> ApproveVendorManagerAsync(int vendorManagerId, bool isApproved);
    Task<List<VendorManager>> GetPendingApprovalVendorManagersAsync();
    Task<List<VendorManager>> GetApprovedVendorManagersAsync();
    Task<bool> UpdateCommercialInfoAsync(int vendorManagerId, string? registrationNumber, string? vatNumber);
    
    // Validation methods
    Task<bool> IsCommercialRegistrationUniqueAsync(string registrationNumber, int? excludeId = null);
    Task<bool> IsVatNumberUniqueAsync(string vatNumber, int? excludeId = null);
    
    // Business relationships
    Task<int> GetVendorManagerVendorCountAsync(int vendorManagerId);
    Task<List<VendorManager>> GetVendorManagersByStatusAsync(bool isApproved);
}
