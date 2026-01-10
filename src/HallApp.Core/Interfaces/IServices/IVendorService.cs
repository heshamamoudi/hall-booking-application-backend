using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IServices;

public interface IVendorService
{
    Task<Vendor> CreateVendorAsync(Vendor vendor);
    Task<Vendor> UpdateVendorAsync(int id, Vendor vendor);
    Task<Vendor> ToggleVendorActiveAsync(int id, bool isActive);
    Task<bool> DeleteVendorAsync(int id);
    Task<Vendor> GetVendorByIdAsync(int id);
    Task<IEnumerable<Vendor>> GetVendorsAsync(object vendorParams);
    Task<List<Vendor>> GetVendorsByTypeAsync(int typeId);
    Task<List<Vendor>> GetVendorsByManagerAsync(int managerId);
    Task<List<Vendor>> SearchVendorsAsync(string searchTerm);
    
    // Validation methods
    Task<bool> IsNameUniqueAsync(string name, int excludeId = 0);
    Task<bool> IsEmailUniqueAsync(string email, int excludeId = 0);
    Task<bool> IsPhoneUniqueAsync(string phone, int excludeId = 0);
    
    // Vendor Type methods
    Task<List<VendorType>> GetVendorTypesAsync();
    Task<VendorType> GetVendorTypeByIdAsync(int id);
    Task<VendorType> CreateVendorTypeAsync(VendorType vendorType);
    Task<VendorType> UpdateVendorTypeAsync(VendorType vendorType);
    Task<bool> DeleteVendorTypeAsync(int id);
}
