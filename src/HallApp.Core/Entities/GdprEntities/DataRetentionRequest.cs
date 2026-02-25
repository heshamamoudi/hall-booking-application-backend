using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HallApp.Core.Entities.GdprEntities;

/// <summary>
/// HIGH-012: Tracks GDPR data deletion/anonymization requests.
/// Records the lifecycle of a right-to-erasure request from submission through completion.
/// </summary>
public class DataRetentionRequest
{
    public int Id { get; set; }

    /// <summary>
    /// The user whose data is subject to this request.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Type of request: "Deletion", "Anonymization", "Export", "LegalHold".
    /// </summary>
    [Required]
    [StringLength(50)]
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// Current status: "Pending", "Processing", "Completed", "Rejected", "Blocked".
    /// </summary>
    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Reason provided by the requester (user or admin).
    /// </summary>
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// If rejected or blocked, the reason for denial.
    /// </summary>
    [StringLength(1000)]
    public string RejectionReason { get; set; } = string.Empty;

    /// <summary>
    /// User who initiated the request (may be the user themselves or an admin).
    /// </summary>
    public int RequestedByUserId { get; set; }

    /// <summary>
    /// User who processed/completed the request (admin or system).
    /// </summary>
    public int? ProcessedByUserId { get; set; }

    /// <summary>
    /// When the request was submitted.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the request was processed (approved, rejected, or completed).
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// When the anonymization/deletion was actually completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Summary of what data was affected by the request.
    /// </summary>
    [Column(TypeName = "text")]
    public string AffectedDataSummary { get; set; } = string.Empty;

    /// <summary>
    /// Number of entities that were anonymized or deleted.
    /// </summary>
    public int AffectedEntityCount { get; set; }

    // Navigation properties
    public AppUser? User { get; set; }
    public AppUser? RequestedByUser { get; set; }
    public AppUser? ProcessedByUser { get; set; }
}

/// <summary>
/// Constants for GDPR request types.
/// </summary>
public static class GdprRequestType
{
    public const string Deletion = "Deletion";
    public const string Anonymization = "Anonymization";
    public const string Export = "Export";
    public const string LegalHold = "LegalHold";
}

/// <summary>
/// Constants for GDPR request statuses.
/// </summary>
public static class GdprRequestStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Rejected = "Rejected";
    public const string Blocked = "Blocked";
}
