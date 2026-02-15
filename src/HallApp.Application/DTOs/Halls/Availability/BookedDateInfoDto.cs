namespace HallApp.Application.DTOs.Halls.Availability;

/// <summary>
/// DTO representing a booked date in the calendar view.
/// Used to show which dates have existing bookings.
/// </summary>
public class BookedDateInfoDto
{
    /// <summary>
    /// Date of the booking event
    /// </summary>
    public DateTime EventDate { get; set; }

    /// <summary>
    /// Booking status (e.g., Pending, Confirmed, Completed, Cancelled)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Name of the customer who made the booking
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Type of event (e.g., Wedding, Birthday, Corporate)
    /// </summary>
    public string EventType { get; set; } = string.Empty;
}
