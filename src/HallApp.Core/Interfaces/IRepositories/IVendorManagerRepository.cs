using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IVendorManagerRepository : IGenericRepository<VendorManager>
{
    Task<VendorManager?> GetByUserIdAsync(string userId);
    Task<bool> VendorManagerExistsAsync(string userId);
}
