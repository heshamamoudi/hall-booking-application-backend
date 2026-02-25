using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HallApp.Core.Entities.PaymentEntities;

/// <summary>
/// HIGH-006: Audit trail for all financial operations (payments, refunds, invoices).
/// Records before/after state snapshots as JSON for full traceability.
/// </summary>
public class FinancialAuditLog
{
    public int Id { get; set; }

    /// <summary>
    /// User who performed the operation. Null for system-initiated operations.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Type of financial operation performed.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// The entity type being operated on (e.g., "Payment", "Invoice", "Booking").
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The primary key of the entity being operated on.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// JSON snapshot of the entity state before the operation. Null for create operations.
    /// </summary>
    [Column(TypeName = "text")]
    public string BeforeState { get; set; } = string.Empty;

    /// <summary>
    /// JSON snapshot of the entity state after the operation. Null for delete operations.
    /// </summary>
    [Column(TypeName = "text")]
    public string AfterState { get; set; } = string.Empty;

    /// <summary>
    /// IP address of the client that initiated the operation.
    /// </summary>
    [StringLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User-Agent header of the client that initiated the operation.
    /// </summary>
    [StringLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the operation in UTC.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for tracing related operations across the request pipeline.
    /// </summary>
    [StringLength(100)]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the operation for human readability.
    /// </summary>
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    // Navigation
    public AppUser? User { get; set; }
}

/// <summary>
/// Enumeration of financial operation types for the audit log.
/// </summary>
public static class FinancialOperationType
{
    public const string PaymentCheckoutCreated = "payment_checkout_created";
    public const string PaymentStatusChanged = "payment_status_changed";
    public const string PaymentRefundProcessed = "payment_refund_processed";
    public const string PaymentCompensation = "payment_compensation";
    public const string InvoiceGenerated = "invoice_generated";
    public const string InvoiceCancelled = "invoice_cancelled";
    public const string InvoiceRefunded = "invoice_refunded";
    public const string InvoiceRegenerated = "invoice_regenerated";
    public const string InvoiceBulkGenerated = "invoice_bulk_generated";
    public const string BookingStatusReverted = "booking_status_reverted";
}
