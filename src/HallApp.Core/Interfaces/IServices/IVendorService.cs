using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IServices;

public interface IVendorService
{
    Task<Vendor> CreateVendorAsync(Vendor vendor);
    Task<Vendor?> UpdateVendorAsync(int id, Vendor vendor);
    Task<Vendor?> ToggleVendorActiveAsync(int id, bool isActive);
    Task<bool> DeleteVendorAsync(int id);
    Task<Vendor?> GetVendorByIdAsync(int id);
    Task<IEnumerable<Vendor>> GetVendorsAsync(object? vendorParams);
    Task<List<Vendor>> GetVendorsByTypeAsync(int typeId);
    Task<List<Vendor>> GetVendorsByManagerAsync(int managerId);
    Task<List<Vendor>> GetVendorsByManagerIdAsync(string userId);
    Task<List<Vendor>> SearchVendorsAsync(string searchTerm);
    Task<bool> UpdateVendorManagersAsync(int vendorId, List<int> managerIds);
    
    // Validation methods
    Task<bool> IsNameUniqueAsync(string name, int excludeId = 0);
    Task<bool> IsEmailUniqueAsync(string email, int excludeId = 0);
    Task<bool> IsPhoneUniqueAsync(string phone, int excludeId = 0);
    
    // Vendor Type methods
    Task<List<VendorType>> GetVendorTypesAsync();
    Task<VendorType?> GetVendorTypeByIdAsync(int id);
    Task<VendorType> CreateVendorTypeAsync(VendorType vendorType);
    Task<VendorType> UpdateVendorTypeAsync(VendorType vendorType);
    Task<bool> DeleteVendorTypeAsync(int id);

    // Organization-based methods
    Task<bool> AssignVendorToManagerAsync(int vendorId, int vendorManagerId);
    Task<List<Vendor>> GetOrganizationVendorsAsync(int orgId);
    Task<List<Vendor>> GetManagerAssignedVendorsAsync(int managerId);

    /// <summary>
    /// Builds a mapping from AppUserId to VendorManager.Id for a batch of user IDs.
    /// HIGH-6 FIX: Enables efficient in-memory filtering without per-member database calls.
    /// </summary>
    Task<Dictionary<int, int>> GetAppUserToVendorManagerIdMapAsync(IEnumerable<int> appUserIds);
}
