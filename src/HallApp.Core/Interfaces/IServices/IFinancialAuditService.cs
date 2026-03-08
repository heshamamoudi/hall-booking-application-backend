namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// HIGH-006: Service interface for recording financial audit trail entries.
/// </summary>
public interface IFinancialAuditService
{
    /// <summary>
    /// Logs a financial operation with before/after state snapshots.
    /// </summary>
    /// <param name="userId">User who performed the operation. Null for system operations.</param>
    /// <param name="operationType">Type of operation (use FinancialOperationType constants).</param>
    /// <param name="entityType">Entity type name (e.g., "Payment", "Invoice").</param>
    /// <param name="entityId">Primary key of the entity.</param>
    /// <param name="beforeState">Object representing state before the operation. Serialized to JSON.</param>
    /// <param name="afterState">Object representing state after the operation. Serialized to JSON.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="userAgent">Client User-Agent header.</param>
    /// <param name="correlationId">Correlation ID for tracing related operations.</param>
    /// <param name="description">Human-readable description of the operation.</param>
    Task LogAsync(
        int? userId,
        string operationType,
        string entityType,
        string entityId,
        object? beforeState = null,
        object? afterState = null,
        string ipAddress = "",
        string userAgent = "",
        string correlationId = "",
        string description = "");
}
