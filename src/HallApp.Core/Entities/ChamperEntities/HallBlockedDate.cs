namespace HallApp.Core.Entities.ChamperEntities;

/// <summary>
/// Represents a date range blocked by a hall manager to prevent bookings.
/// Used for maintenance, private events, holidays, or other unavailability periods.
/// </summary>
public class HallBlockedDate
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the Hall this block applies to
    /// </summary>
    public int HallId { get; set; }

    /// <summary>
    /// Start date of the blocked period (inclusive)
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// End date of the blocked period (inclusive)
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Reason for blocking these dates (required for audit trail)
    /// Max length: 500 characters
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the user (hall manager) who created this block
    /// </summary>
    public int BlockedByUserId { get; set; }

    /// <summary>
    /// Timestamp when this block was created
    /// </summary>
    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft delete flag. False means the block has been removed/unblocked.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Record last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// The hall this block applies to
    /// </summary>
    public Hall? Hall { get; set; }

    /// <summary>
    /// The user who created this block
    /// </summary>
    public AppUser? BlockedByUser { get; set; }
}
