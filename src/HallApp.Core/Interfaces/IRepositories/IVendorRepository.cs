using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IVendorRepository : IGenericRepository<Vendor>
{
    // Create
    Task<Vendor> CreateVendor(Vendor vendor);

    // Read
    Task<IEnumerable<Vendor>> GetAllVendorsAsync();
    Task<Vendor> GetVendorByIdAsync(int id);
    Task<List<Vendor>> GetVendorsByTypeAsync(int typeId);
    Task<List<Vendor>> GetVendorsByManagerAsync(int managerId);
    Task<List<Vendor>> GetVendorsByManagerIdAsync(string userId);
    Task<List<Vendor>> SearchVendorsAsync(string searchTerm);

    // Update
    Task<Vendor> UpdateVendorAsync(int id, Vendor updateVendor);
    Task<VendorLocation> UpdateVendorLocationAsync(int vendorId, VendorLocation updateLocation);

    // Delete
    Task<bool> DeleteVendorAsync(int id);

    // Validation
    Task<bool> IsNameUniqueAsync(string name, int excludeId = 0);
    Task<bool> IsEmailUniqueAsync(string email, int excludeId = 0);
    Task<bool> IsPhoneUniqueAsync(string phone, int excludeId = 0);

    // Vendor Type Operations
    Task<List<VendorType>> GetVendorTypesAsync();
    Task<VendorType> GetVendorTypeByIdAsync(int id);
    Task<VendorType> CreateVendorTypeAsync(VendorType vendorType);
    Task<VendorType> UpdateVendorTypeAsync(VendorType vendorType);
    Task<bool> DeleteVendorTypeAsync(int id);
}
