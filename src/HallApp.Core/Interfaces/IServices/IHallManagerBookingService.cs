using HallApp.Core.Entities.BookingEntities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// Dashboard statistics for hall manager / organization manager booking views.
/// Counts are future-only (EventDate >= today). Revenue is all-time.
/// </summary>
public class BookingDashboardStatsDto
{
    /// <summary>Number of bookings with EventDate == today.</summary>
    public int TodayCount { get; set; }

    /// <summary>Number of bookings with EventDate strictly after today (future only, excluding today).</summary>
    public int UpcomingCount { get; set; }

    /// <summary>Number of bookings with Status == "Pending" AND EventDate >= today.</summary>
    public int PendingApprovalCount { get; set; }

    /// <summary>Number of bookings with EventDate in the current calendar month and EventDate >= today.</summary>
    public int ThisMonthCount { get; set; }

    /// <summary>Sum of TotalAmount for bookings where PaymentStatus == "Paid". All time, no date filter.</summary>
    public decimal TotalPaidRevenue { get; set; }
}

/// <summary>
/// Service interface for HallManager-specific booking operations.
/// Follows Interface Segregation Principle - focused exclusively on
/// booking retrieval for hall managers, separate from general booking operations.
/// </summary>
public interface IHallManagerBookingService
{
    /// <summary>
    /// Gets all bookings for halls managed by the specified user.
    /// Data isolation is enforced at the service layer.
    /// </summary>
    /// <param name="appUserId">The AppUser ID of the authenticated hall manager</param>
    /// <param name="cancellationToken">Cancellation token for request cancellation</param>
    /// <returns>Collection of bookings for the manager's halls, ordered by creation date descending</returns>
    Task<IEnumerable<Booking>> GetBookingsForManagedHallsAsync(int appUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated bookings for halls managed by the specified user.
    /// </summary>
    /// <param name="appUserId">The AppUser ID of the authenticated hall manager</param>
    /// <param name="pageNumber">1-based page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token for request cancellation</param>
    /// <returns>Tuple of (bookings for the current page, total count of all bookings)</returns>
    Task<(IEnumerable<Booking> Bookings, int TotalCount)> GetBookingsForManagedHallsPagedAsync(
        int appUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated bookings for all halls in the organization owned by the specified user.
    /// Used exclusively by HallOrganizationManager role to see all org bookings.
    /// </summary>
    /// <param name="appUserId">The AppUser ID of the organization owner</param>
    /// <param name="hallId">Optional hall ID filter to narrow results to a specific hall</param>
    /// <param name="city">Optional city filter to narrow results to halls in a specific city</param>
    /// <param name="pageNumber">1-based page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token for request cancellation</param>
    /// <returns>Tuple of (bookings for the current page, total count of all matching bookings)</returns>
    Task<(IEnumerable<Booking> Bookings, int TotalCount)> GetBookingsForOrganizationPagedAsync(
        int appUserId,
        int? hallId,
        string? city,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dashboard statistics for halls managed by the specified hall manager.
    /// Counts filter to future bookings (EventDate >= today). Revenue is all-time.
    /// </summary>
    /// <param name="appUserId">The AppUser ID of the authenticated hall manager</param>
    /// <param name="cancellationToken">Cancellation token for request cancellation</param>
    /// <returns>Dashboard statistics DTO</returns>
    Task<BookingDashboardStatsDto> GetHallManagerDashboardStatsAsync(
        int appUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dashboard statistics for all halls in the organization owned by the specified user.
    /// Counts filter to future bookings (EventDate >= today). Revenue is all-time.
    /// </summary>
    /// <param name="appUserId">The AppUser ID of the organization owner</param>
    /// <param name="cancellationToken">Cancellation token for request cancellation</param>
    /// <returns>Dashboard statistics DTO</returns>
    Task<BookingDashboardStatsDto> GetOrgManagerDashboardStatsAsync(
        int appUserId,
        CancellationToken cancellationToken = default);
}
