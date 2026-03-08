using System.Globalization;
using System.Text;
using HallApp.Core.Entities.PaymentEntities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Web.Controllers.Admin;

/// <summary>
/// Provides audit trail endpoints for financial operations.
/// Admin-only access for reviewing payment, invoice, and booking audit logs.
/// </summary>
[Authorize(Roles = "Admin")]
[Route("api/audit")]
public class AuditController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IUnitOfWork unitOfWork, ILogger<AuditController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated financial audit logs with optional filters.
    /// </summary>
    /// <param name="page">Page number (1-based, default 1)</param>
    /// <param name="pageSize">Items per page (default 20, max 100)</param>
    /// <param name="search">Free-text search across description, entity type, entity ID</param>
    /// <param name="operationType">Filter by operation type (e.g., payment_checkout_created)</param>
    /// <param name="entityType">Filter by entity type (e.g., Payment, Invoice)</param>
    /// <param name="userId">Filter by user ID</param>
    /// <param name="startDate">Filter logs from this date (inclusive, UTC)</param>
    /// <param name="endDate">Filter logs up to this date (inclusive, UTC)</param>
    [HttpGet("financial-logs")]
    public async Task<ActionResult<ApiResponse<FinancialAuditLogPageDto>>> GetFinancialLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? operationType = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // Clamp pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = BuildFilteredQuery(search, operationType, entityType, userId, startDate, endDate);

            var totalCount = await query.CountAsync();

            var entities = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var logs = entities.Select(l => new FinancialAuditLogDto
            {
                Id = l.Id.ToString(),
                Timestamp = l.Timestamp.ToString("o"),
                UserId = l.UserId.HasValue ? l.UserId.Value.ToString() : "system",
                UserName = l.User != null
                    ? $"{l.User.FirstName} {l.User.LastName}".Trim()
                    : "System",
                OperationType = l.OperationType,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Description = l.Description,
                BeforeState = l.BeforeState,
                AfterState = l.AfterState,
                IpAddress = l.IpAddress
            }).ToList();

            var result = new FinancialAuditLogPageDto
            {
                Items = logs,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Success(result, "Financial audit logs retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve financial audit logs");
            return Error<FinancialAuditLogPageDto>("Failed to retrieve financial audit logs", 500);
        }
    }

    /// <summary>
    /// Export financial audit logs as a CSV file with the same filters as the list endpoint.
    /// </summary>
    [HttpGet("financial-logs/export")]
    public async Task<IActionResult> ExportFinancialLogs(
        [FromQuery] string? search = null,
        [FromQuery] string? operationType = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var query = BuildFilteredQuery(search, operationType, entityType, userId, startDate, endDate);

            // Cap export at 10,000 rows to prevent memory issues
            var entities = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(10_000)
                .ToListAsync();

            var logs = entities.Select(l => new
            {
                l.Id,
                l.Timestamp,
                UserId = l.UserId.HasValue ? l.UserId.Value.ToString() : "system",
                UserName = l.User != null
                    ? $"{l.User.FirstName} {l.User.LastName}".Trim()
                    : "System",
                l.OperationType,
                l.EntityType,
                l.EntityId,
                l.Description,
                l.IpAddress,
                l.CorrelationId
            }).ToList();

            var sb = new StringBuilder();

            // CSV header
            sb.AppendLine("Id,Timestamp,UserId,UserName,OperationType,EntityType,EntityId,Description,IpAddress,CorrelationId");

            // CSV rows
            foreach (var log in logs)
            {
                sb.Append(log.Id).Append(',');
                sb.Append(log.Timestamp.ToString("o", CultureInfo.InvariantCulture)).Append(',');
                sb.Append(CsvEscape(log.UserId)).Append(',');
                sb.Append(CsvEscape(log.UserName)).Append(',');
                sb.Append(CsvEscape(log.OperationType)).Append(',');
                sb.Append(CsvEscape(log.EntityType)).Append(',');
                sb.Append(CsvEscape(log.EntityId)).Append(',');
                sb.Append(CsvEscape(log.Description)).Append(',');
                sb.Append(CsvEscape(log.IpAddress)).Append(',');
                sb.Append(CsvEscape(log.CorrelationId));
                sb.AppendLine();
            }

            var bom = Encoding.UTF8.GetPreamble();
            var csvBytes = bom.Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var fileName = $"financial-audit-logs-{DateTime.UtcNow:yyyy-MM-dd}.csv";

            return File(csvBytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export financial audit logs");
            return StatusCode(500, new ApiResponse(500, "Failed to export financial audit logs"));
        }
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds a filtered IQueryable for FinancialAuditLog based on the provided parameters.
    /// Shared between the list and export endpoints to ensure consistent filtering.
    /// </summary>
    private IQueryable<FinancialAuditLog> BuildFilteredQuery(
        string? search,
        string? operationType,
        string? entityType,
        string? userId,
        DateTime? startDate,
        DateTime? endDate)
    {
        IQueryable<FinancialAuditLog> query = _unitOfWork.FinancialAuditLogRepository
            .GetQueryable()
            .AsNoTracking()
            .Include(l => l.User);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(l =>
                (l.Description != null && l.Description.ToLower().Contains(term)) ||
                l.EntityType.ToLower().Contains(term) ||
                l.EntityId.ToLower().Contains(term) ||
                l.OperationType.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(operationType))
        {
            query = query.Where(l => l.OperationType == operationType);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(l => l.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(userId) && int.TryParse(userId, out var parsedUserId))
        {
            query = query.Where(l => l.UserId == parsedUserId);
        }

        if (startDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            // Include the entire end date day
            query = query.Where(l => l.Timestamp < endDate.Value.Date.AddDays(1));
        }

        return query;
    }

    /// <summary>
    /// Escapes a string value for safe inclusion in a CSV cell.
    /// Wraps in quotes if the value contains commas, quotes, or newlines.
    /// </summary>
    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Protect against CSV formula injection
        if (value.Length > 0 && "=+-@\t\r".IndexOf(value[0]) >= 0)
            value = "'" + value;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

// ---------------------------------------------------------------------------
// DTOs — kept in the controller file for locality; can be moved to
// HallApp.Application/DTOs if the project prefers a separate file.
// ---------------------------------------------------------------------------

/// <summary>
/// Paginated result of financial audit log entries.
/// Matches the frontend AuditLogPage interface.
/// </summary>
public class FinancialAuditLogPageDto
{
    public List<FinancialAuditLogDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Single financial audit log entry.
/// Matches the frontend AuditLogEntry interface.
/// </summary>
public class FinancialAuditLogDto
{
    public string Id { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BeforeState { get; set; } = string.Empty;
    public string AfterState { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}
