using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// Service interface for VendorManager-specific booking operations.
///
/// Mirrors IHallManagerBookingService. The two roles are structurally the same:
/// a manager sees the bookings for the resources assigned to them, an organization
/// owner sees every booking across the organization. Keeping the shapes identical
/// means the frontend, the DTOs and the authorization all behave the same way on
/// both sides of the business.
/// </summary>
public interface IVendorManagerBookingService
{
    /// <summary>
    /// Gets paginated vendor bookings for the vendors assigned to the specified manager.
    /// Data isolation is enforced here: a manager only ever sees their own vendors.
    /// </summary>
    /// <param name="appUserId">The AppUser ID of the authenticated vendor manager</param>
    /// <param name="pageNumber">1-based page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (bookings for the current page, total count of all bookings)</returns>
    Task<(IEnumerable<VendorBooking> Bookings, int TotalCount)> GetBookingsForManagedVendorsPagedAsync(
        int appUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated vendor bookings for every vendor in the organization owned by
    /// the specified user. Used exclusively by the VendorOrganizationManager role.
    /// </summary>
    /// <param name="appUserId">The AppUser ID of the organization owner</param>
    /// <param name="vendorId">Optional vendor ID filter</param>
    /// <param name="status">Optional status filter (case-insensitive)</param>
    /// <param name="pageNumber">1-based page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (bookings for the current page, total count of all matching bookings)</returns>
    Task<(IEnumerable<VendorBooking> Bookings, int TotalCount)> GetBookingsForOrganizationPagedAsync(
        int appUserId,
        int? vendorId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dashboard statistics for the vendors assigned to the specified manager.
    /// Counts filter to future bookings (ServiceDate >= today). Revenue is all-time.
    /// </summary>
    Task<BookingDashboardStatsDto> GetVendorManagerDashboardStatsAsync(
        int appUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dashboard statistics for every vendor in the organization owned by the
    /// specified user. Counts filter to future bookings. Revenue is all-time.
    /// </summary>
    Task<BookingDashboardStatsDto> GetVendorOrgManagerDashboardStatsAsync(
        int appUserId,
        CancellationToken cancellationToken = default);
}
