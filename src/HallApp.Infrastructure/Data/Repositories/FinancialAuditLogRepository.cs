using HallApp.Core.Entities.PaymentEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

/// <summary>
/// HIGH-006: Repository implementation for financial audit log operations.
/// </summary>
public class FinancialAuditLogRepository : GenericRepository<FinancialAuditLog>, IFinancialAuditLogRepository
{
    public FinancialAuditLogRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FinancialAuditLog>> GetByEntityAsync(string entityType, string entityId)
    {
        return await _context.FinancialAuditLogs
            .AsNoTracking()
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FinancialAuditLog>> GetByCorrelationIdAsync(string correlationId)
    {
        return await _context.FinancialAuditLogs
            .AsNoTracking()
            .Where(l => l.CorrelationId == correlationId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FinancialAuditLog>> GetByUserIdAsync(int userId)
    {
        return await _context.FinancialAuditLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .Take(100)
            .ToListAsync();
    }

    /// <inheritdoc />
    public IQueryable<FinancialAuditLog> GetQueryable()
    {
        return _context.FinancialAuditLogs.AsQueryable();
    }
}
