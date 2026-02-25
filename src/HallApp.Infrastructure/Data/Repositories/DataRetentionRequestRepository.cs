using HallApp.Core.Entities.GdprEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

/// <summary>
/// HIGH-012: Repository implementation for GDPR data retention requests.
/// </summary>
public class DataRetentionRequestRepository : GenericRepository<DataRetentionRequest>, IDataRetentionRequestRepository
{
    public DataRetentionRequestRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DataRetentionRequest>> GetPendingRequestsAsync()
    {
        return await _context.DataRetentionRequests
            .AsNoTracking()
            .Where(r => r.Status == GdprRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DataRetentionRequest>> GetByUserIdAsync(int userId)
    {
        return await _context.DataRetentionRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<DataRetentionRequest> GetActiveRequestForUserAsync(int userId)
    {
        return await _context.DataRetentionRequests
            .Where(r => r.UserId == userId
                        && (r.Status == GdprRequestStatus.Pending || r.Status == GdprRequestStatus.Processing))
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync();
    }
}
