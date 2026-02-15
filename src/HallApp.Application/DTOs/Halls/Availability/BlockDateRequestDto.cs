namespace HallApp.Application.DTOs.Halls.Availability;

/// <summary>
/// Request DTO for blocking a date range for a hall.
/// Used in POST /api/halls/{hallId}/availability/block endpoint.
/// </summary>
public class BlockDateRequestDto
{
    /// <summary>
    /// Start date of the period to block (inclusive)
    /// Must be today or in the future
    /// Format: yyyy-MM-dd
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// End date of the period to block (inclusive)
    /// Must be >= StartDate
    /// Format: yyyy-MM-dd
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Reason for blocking these dates (required for audit trail)
    /// Min length: 1, Max length: 500
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
