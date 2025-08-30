using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IVendorManagerRepository
{
    Task<IEnumerable<VendorManager>> GetAllAsync();
    Task<VendorManager?> GetByIdAsync(int id);
    Task<VendorManager?> GetByUserIdAsync(string userId);
    Task<bool> VendorManagerExistsAsync(string userId);
    Task<bool> CommercialRegistrationExistsAsync(string commercialRegistrationNumber);
    Task<bool> VatNumberExistsAsync(string vatNumber);
    void Add(VendorManager vendorManager);
    void Update(VendorManager vendorManager);
    void Delete(VendorManager vendorManager);
}
