using System.ComponentModel.DataAnnotations;

namespace HallApp.Core.Entities;

/// <summary>
/// Stores idempotency keys for payment operations to prevent duplicate processing
/// Keys expire after 24 hours
/// </summary>
public class IdempotencyKey
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The unique idempotency key provided by the client
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The operation type (e.g., "payment", "refund", "invoice_generation")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// The user ID who initiated the operation
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The entity ID affected by the operation (e.g., invoice ID, payment ID)
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// The HTTP status code of the response
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// The JSON serialized response body
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// When the operation was first processed
    /// </summary>
    [Required]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record expires (typically 24 hours after ProcessedAt)
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Additional metadata about the request (optional)
    /// </summary>
    public string? RequestMetadata { get; set; }
}
