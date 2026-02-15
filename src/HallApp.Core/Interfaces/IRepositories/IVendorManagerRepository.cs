using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IVendorManagerRepository : IGenericRepository<VendorManager>
{
    Task<VendorManager?> GetByUserIdAsync(string userId);
    Task<bool> VendorManagerExistsAsync(string userId);

    /// <summary>
    /// Gets VendorManagers for a batch of AppUserIds using AsNoTracking.
    /// HIGH-6 FIX: Enables building AppUserId-to-VendorManagerId mapping in a single query
    /// instead of loading all managers per-member.
    /// </summary>
    Task<List<VendorManager>> GetByAppUserIdsAsync(IEnumerable<int> appUserIds);
}
