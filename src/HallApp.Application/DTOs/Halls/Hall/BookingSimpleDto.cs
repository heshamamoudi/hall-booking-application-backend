namespace HallApp.Application.DTOs.Halls.Hall;

/// <summary>
/// Simplified booking representation for the recent bookings list.
/// Used by the GET /api/halls/{id}/bookings/recent endpoint.
/// Avoids exposing sensitive customer PII.
/// </summary>
public class BookingSimpleDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
