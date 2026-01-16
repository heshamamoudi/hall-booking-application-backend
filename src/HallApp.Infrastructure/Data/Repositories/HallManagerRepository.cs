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

    public async Task<HallManager> GetByUserIdAsync(string userId)
    {
        if (int.TryParse(userId, out int userIdInt))
        {
            return await _context.HallManagers
                .Include(hm => hm.AppUser)
                .FirstOrDefaultAsync(hm => hm.AppUserId == userIdInt);
        }
        return null;
    }

    // Override base methods to include AppUser and Halls relationships
    public new async Task<IEnumerable<HallManager>> GetAllAsync()
    {
        return await _context.HallManagers
            .Include(hm => hm.AppUser)
            .Include(hm => hm.Halls)  // CRITICAL: Load Halls for filtering conversations
            .OrderBy(hm => hm.CreatedAt)
            .ToListAsync();
    }

    public new async Task<HallManager> GetByIdAsync(int id)
    {
        return await _context.HallManagers
            .Include(hm => hm.AppUser)
            .Include(hm => hm.Halls)  // CRITICAL: Load Halls for filtering conversations
            .FirstOrDefaultAsync(hm => hm.Id == id);
    }
}
