namespace HallApp.Application.DTOs.Halls.Availability;

/// <summary>
/// DTO representing a blocked date range for a hall.
/// Used in GET endpoints to return blocked date information.
/// </summary>
public class BlockedDateDto
{
    /// <summary>
    /// Unique identifier for the blocked date record
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The hall this block applies to
    /// </summary>
    public int HallId { get; set; }

    /// <summary>
    /// Start date of the blocked period (inclusive)
    /// Format: yyyy-MM-dd
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// End date of the blocked period (inclusive)
    /// Format: yyyy-MM-dd
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Reason for blocking these dates
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Name of the user who created this block
    /// </summary>
    public string BlockedByUserName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when this block was created
    /// </summary>
    public DateTime BlockedAt { get; set; }
}
