using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class VendorManagerRepository : GenericRepository<VendorManager>, IVendorManagerRepository
{
    public VendorManagerRepository(DataContext context) : base(context)
    {
    }

    public new async Task<IEnumerable<VendorManager>> GetAllAsync()
    {
        return await _context.VendorManagers
            .Include(vm => vm.AppUser)
            .Include(vm => vm.Vendors)  // CRITICAL: Load Vendors for filtering conversations
            .OrderBy(vm => vm.CreatedAt)
            .ToListAsync();
    }

    public new async Task<VendorManager?> GetByIdAsync(int id)
    {
        return await _context.VendorManagers
            .Include(vm => vm.AppUser)
            .Include(vm => vm.Vendors)  // CRITICAL: Load Vendors for filtering conversations
            .FirstOrDefaultAsync(vm => vm.Id == id);
    }

    /// <inheritdoc />
    public async Task<VendorManager?> GetByAppUserIdWithVendorsAsync(int appUserId)
    {
        return await _context.VendorManagers
            .Include(vm => vm.AppUser)
            .Include(vm => vm.Vendors)
            .AsNoTracking()
            .FirstOrDefaultAsync(vm => vm.AppUserId == appUserId);
    }

    public async Task<VendorManager?> GetByUserIdAsync(string userId)
    {
        if (int.TryParse(userId, out int userIdInt))
        {
            return await _context.VendorManagers
                .Include(vm => vm.AppUser)
                .FirstOrDefaultAsync(vm => vm.AppUserId == userIdInt);
        }
        return null;
    }

    public async Task<bool> VendorManagerExistsAsync(string userId)
    {
        if (int.TryParse(userId, out int userIdInt))
        {
            return await _context.VendorManagers
                .AnyAsync(vm => vm.AppUserId == userIdInt);
        }
        return false;
    }

    /// <inheritdoc />
    public async Task<List<VendorManager>> GetByAppUserIdsAsync(IEnumerable<int> appUserIds)
    {
        var userIdSet = appUserIds.ToList();
        return await _context.VendorManagers
            .Where(vm => userIdSet.Contains(vm.AppUserId))
            .AsNoTracking()
            .Select(vm => new VendorManager
            {
                Id = vm.Id,
                AppUserId = vm.AppUserId
            })
            .ToListAsync();
    }
}
