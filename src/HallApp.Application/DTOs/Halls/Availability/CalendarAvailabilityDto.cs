namespace HallApp.Application.DTOs.Halls.Availability;

/// <summary>
/// DTO representing calendar availability data for a specific month.
/// Used in GET /api/halls/{hallId}/availability/calendar endpoint.
/// Shows both blocked dates and existing bookings for efficient calendar rendering.
/// </summary>
public class CalendarAvailabilityDto
{
    /// <summary>
    /// Year of the calendar view
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Month of the calendar view (1-12)
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// List of blocked date ranges in this month
    /// </summary>
    public IEnumerable<BlockedDateDto> BlockedDates { get; set; } = [];

    /// <summary>
    /// List of booked dates in this month
    /// </summary>
    public IEnumerable<BookedDateInfoDto> BookedDates { get; set; } = [];
}
