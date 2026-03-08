using System.Text.Json;
using System.Text.Json.Serialization;
using HallApp.Core.Entities.PaymentEntities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

/// <summary>
/// HIGH-006: Service implementation for recording financial audit trail entries.
/// Serializes entity state snapshots to JSON for full traceability of financial operations.
/// </summary>
public class FinancialAuditService : IFinancialAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FinancialAuditService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        MaxDepth = 3
    };

    public FinancialAuditService(
        IUnitOfWork unitOfWork,
        ILogger<FinancialAuditService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogAsync(
        int? userId,
        string operationType,
        string entityType,
        string entityId,
        object? beforeState = null,
        object? afterState = null,
        string ipAddress = "",
        string userAgent = "",
        string correlationId = "",
        string description = "")
    {
        try
        {
            var auditLog = new FinancialAuditLog
            {
                UserId = userId,
                OperationType = operationType,
                EntityType = entityType,
                EntityId = entityId,
                BeforeState = SafeSerialize(beforeState),
                AfterState = SafeSerialize(afterState),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow,
                CorrelationId = string.IsNullOrEmpty(correlationId)
                    ? Guid.NewGuid().ToString("N")[..12]
                    : correlationId,
                Description = description
            };

            await _unitOfWork.FinancialAuditLogRepository.AddAsync(auditLog);
            await _unitOfWork.Complete();

            _logger.LogInformation(
                "HIGH-006: Financial audit logged - Operation: {OperationType}, " +
                "Entity: {EntityType}/{EntityId}, User: {UserId}, CorrelationId: {CorrelationId}",
                operationType, entityType, entityId, userId?.ToString() ?? "system", auditLog.CorrelationId);
        }
        catch (Exception ex)
        {
            // Audit logging must never fail the parent operation.
            // Log the failure and continue.
            _logger.LogError(ex,
                "HIGH-006: Failed to write financial audit log - Operation: {OperationType}, " +
                "Entity: {EntityType}/{EntityId}",
                operationType, entityType, entityId);
        }
    }

    /// <summary>
    /// Safely serializes an object to JSON, handling null and circular references.
    /// </summary>
    private static string SafeSerialize(object? value)
    {
        if (value == null) return string.Empty;

        try
        {
            return JsonSerializer.Serialize(value, JsonOptions);
        }
        catch
        {
            return $"{{\"error\": \"serialization_failed\", \"type\": \"{value.GetType().Name}\"}}";
        }
    }
}
