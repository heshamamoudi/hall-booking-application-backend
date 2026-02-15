using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for HallBlockedDate entity.
/// Provides data access methods for managing hall availability blocking.
/// </summary>
public class HallBlockedDateRepository : GenericRepository<HallBlockedDate>, IHallBlockedDateRepository
{
    public HallBlockedDateRepository(DataContext context) : base(context)
    {
    }

    public async Task<IEnumerable<HallBlockedDate>> GetActiveBlockedDatesByHallIdAsync(int hallId)
    {
        return await _context.HallBlockedDates
            .Where(b => b.HallId == hallId && b.IsActive)
            .Include(b => b.BlockedByUser)
            .OrderBy(b => b.StartDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<HallBlockedDate?> GetBlockedDateByIdAsync(int id)
    {
        return await _context.HallBlockedDates
            .Include(b => b.BlockedByUser)
            .Include(b => b.Hall)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<HallBlockedDate>> GetBlockedDatesInRangeAsync(
        int hallId, DateOnly startDate, DateOnly endDate)
    {
        return await _context.HallBlockedDates
            .Where(b => b.HallId == hallId
                && b.IsActive
                && b.StartDate <= endDate
                && b.EndDate >= startDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<HallBlockedDate>> GetBlockedDatesByMonthAsync(
        int hallId, int year, int month)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        return await _context.HallBlockedDates
            .Where(b => b.HallId == hallId
                && b.IsActive
                && b.StartDate <= monthEnd
                && b.EndDate >= monthStart)
            .Include(b => b.BlockedByUser)
            .OrderBy(b => b.StartDate)
            .AsNoTracking()
            .ToListAsync();
    }
}
