using HallApp.Core.Entities.PaymentEntities;

namespace HallApp.Core.Interfaces.IRepositories;

/// <summary>
/// HIGH-006: Repository interface for financial audit log operations.
/// </summary>
public interface IFinancialAuditLogRepository : IGenericRepository<FinancialAuditLog>
{
    /// <summary>
    /// Get audit logs for a specific entity.
    /// </summary>
    Task<IEnumerable<FinancialAuditLog>> GetByEntityAsync(string entityType, string entityId);

    /// <summary>
    /// Get audit logs by correlation ID to trace related operations.
    /// </summary>
    Task<IEnumerable<FinancialAuditLog>> GetByCorrelationIdAsync(string correlationId);

    /// <summary>
    /// Get audit logs for a specific user.
    /// </summary>
    Task<IEnumerable<FinancialAuditLog>> GetByUserIdAsync(int userId);
}
