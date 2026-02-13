namespace HallApp.Application.DTOs.Halls.Hall;

/// <summary>
/// Aggregated statistics for a single hall.
/// Used by the GET /api/halls/{id}/stats endpoint.
/// </summary>
public class HallStatsDto
{
    // Booking counts
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int PendingBookings { get; set; }
    public int ActiveBookings { get; set; }

    // Financial
    public decimal TotalRevenue { get; set; }
    public int PaidInvoices { get; set; }
    public int PendingInvoices { get; set; }

    // Reviews
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
}
