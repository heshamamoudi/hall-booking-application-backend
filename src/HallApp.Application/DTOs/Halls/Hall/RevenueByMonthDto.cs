namespace HallApp.Application.DTOs.Halls.Hall;

/// <summary>
/// Monthly revenue data point for a single hall.
/// Used by the GET /api/halls/{id}/revenue/monthly endpoint.
/// </summary>
public class RevenueByMonthDto
{
    /// <summary>
    /// Month in YYYY-MM format, e.g. "2026-01"
    /// </summary>
    public string Month { get; set; } = string.Empty;

    /// <summary>
    /// Total revenue from paid invoices in this month
    /// </summary>
    public decimal Revenue { get; set; }

    /// <summary>
    /// Number of bookings created in this month
    /// </summary>
    public int Bookings { get; set; }
}
