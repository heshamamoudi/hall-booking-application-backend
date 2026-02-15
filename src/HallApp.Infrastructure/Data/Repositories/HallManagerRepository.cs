using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class HallManagerRepository : GenericRepository<HallManager>, IHallManagerRepository
{
    public HallManagerRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<HallManager?> GetByAppUserIdWithHallsAsync(int appUserId)
    {
        return await _context.HallManagers
            .Include(hm => hm.AppUser)
            .Include(hm => hm.Halls)
            .AsNoTracking()
            .FirstOrDefaultAsync(hm => hm.AppUserId == appUserId);
    }

    /// <inheritdoc />
    public async Task<HallManager?> GetByUserIdAsync(string userId)
    {
        if (int.TryParse(userId, out int userIdInt))
        {
            return await _context.HallManagers
                .Include(hm => hm.AppUser)
                .FirstOrDefaultAsync(hm => hm.AppUserId == userIdInt);
        }
        return null;
    }

    // Override base methods to include AppUser and Halls relationships (LSP fix: use override)
    public override async Task<IEnumerable<HallManager>> GetAllAsync()
    {
        return await _context.HallManagers
            .Include(hm => hm.AppUser)
            .Include(hm => hm.Halls)
            .OrderBy(hm => hm.CreatedAt)
            .ToListAsync();
    }

    public override async Task<HallManager> GetByIdAsync(int id)
    {
        return await _context.HallManagers
            .Include(hm => hm.AppUser)
            .Include(hm => hm.Halls)
            .FirstOrDefaultAsync(hm => hm.Id == id);
    }

    /// <inheritdoc />
    public async Task<List<HallManager>> GetByAppUserIdsAsync(IEnumerable<int> appUserIds)
    {
        var userIdSet = appUserIds.ToList();
        return await _context.HallManagers
            .Where(hm => userIdSet.Contains(hm.AppUserId))
            .AsNoTracking()
            .Select(hm => new HallManager
            {
                Id = hm.Id,
                AppUserId = hm.AppUserId
            })
            .ToListAsync();
    }
}
