using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class VendorManagerRepository : GenericRepository<VendorManager>, IVendorManagerRepository
{
    private readonly DataContext _context;

    public VendorManagerRepository(DataContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<VendorManager>> GetAllAsync()
    {
        return await _context.VendorManagers
            .Include(vm => vm.AppUser)
            .OrderBy(vm => vm.CreatedAt)
            .ToListAsync();
    }

    public async Task<VendorManager> GetByIdAsync(int id)
    {
        return await _context.VendorManagers
            .Include(vm => vm.AppUser)
            .FirstOrDefaultAsync(vm => vm.Id == id) ?? new VendorManager();
    }

    public async Task<VendorManager> GetByUserIdAsync(string userId)
    {
        if (int.TryParse(userId, out int userIdInt))
        {
            return await _context.VendorManagers
                .Include(vm => vm.AppUser)
                .FirstOrDefaultAsync(vm => vm.AppUserId == userIdInt) ?? new VendorManager();
        }
        return new VendorManager();
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
}
