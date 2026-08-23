using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

/// <summary>
/// Service for VendorManager-specific booking operations.
///
/// The vendor mirror of HallManagerBookingService. Scoping rules:
///   - a VendorManager sees bookings for the vendors assigned to them
///   - a VendorOrganizationManager sees bookings for every vendor in their organization
/// Both are resolved here rather than in the controller, so the isolation cannot be
/// bypassed by calling the service from somewhere else.
/// </summary>
public class VendorManagerBookingService : IVendorManagerBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<VendorManagerBookingService> _logger;

    public VendorManagerBookingService(
        IUnitOfWork unitOfWork,
        IOrganizationService organizationService,
        ILogger<VendorManagerBookingService> logger)
    {
        _unitOfWork = unitOfWork;
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<VendorBooking> Bookings, int TotalCount)> GetBookingsForManagedVendorsPagedAsync(
        int appUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var vendorIds = await GetAssignedVendorIdsAsync(appUserId);
        if (vendorIds.Count == 0)
        {
            _logger.LogInformation(
                "VendorManager {AppUserId} has no assigned vendors - returning empty page", appUserId);
            return (Array.Empty<VendorBooking>(), 0);
        }

        return await PageBookingsAsync(vendorIds, vendorId: null, status: null, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<VendorBooking> Bookings, int TotalCount)> GetBookingsForOrganizationPagedAsync(
        int appUserId,
        int? vendorId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var vendorIds = await GetOrganizationVendorIdsAsync(appUserId);
        if (vendorIds.Count == 0)
        {
            _logger.LogInformation(
                "VendorOrganizationManager {AppUserId} has no vendors - returning empty page", appUserId);
            return (Array.Empty<VendorBooking>(), 0);
        }

        return await PageBookingsAsync(vendorIds, vendorId, status, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<BookingDashboardStatsDto> GetVendorManagerDashboardStatsAsync(
        int appUserId,
        CancellationToken cancellationToken = default)
    {
        var vendorIds = await GetAssignedVendorIdsAsync(appUserId);
        return ComputeDashboardStats(await LoadBookingsAsync(vendorIds));
    }

    /// <inheritdoc />
    public async Task<BookingDashboardStatsDto> GetVendorOrgManagerDashboardStatsAsync(
        int appUserId,
        CancellationToken cancellationToken = default)
    {
        var vendorIds = await GetOrganizationVendorIdsAsync(appUserId);
        return ComputeDashboardStats(await LoadBookingsAsync(vendorIds));
    }

    // ===================================================================
    // Scoping
    // ===================================================================

    /// <summary>
    /// Vendors assigned to this manager. An organization owner has no VendorManager
    /// record of their own, so they fall back to the whole organization - the same
    /// arrangement HallController uses for hall organization managers.
    /// </summary>
    private async Task<List<int>> GetAssignedVendorIdsAsync(int appUserId)
    {
        var vendorManager = await _unitOfWork.VendorManagerRepository
            .GetByAppUserIdWithVendorsAsync(appUserId);

        var assigned = vendorManager?.Vendors?.Select(v => v.Id).ToList() ?? new List<int>();
        if (assigned.Count > 0)
        {
            return assigned;
        }

        return await GetOrganizationVendorIdsAsync(appUserId);
    }

    /// <summary>
    /// Every vendor belonging to the organization this user owns or is a member of.
    /// </summary>
    private async Task<List<int>> GetOrganizationVendorIdsAsync(int appUserId)
    {
        var organization = await _organizationService.GetOrganizationByOwnerId(appUserId);
        if (organization == null)
        {
            return new List<int>();
        }

        var vendors = await _unitOfWork.VendorRepository
            .GetVendorsByOrganizationIdAsync(organization.Id);

        return vendors.Select(v => v.Id).ToList();
    }

    // ===================================================================
    // Querying
    // ===================================================================

    private async Task<List<VendorBooking>> LoadBookingsAsync(List<int> vendorIds)
    {
        if (vendorIds.Count == 0)
        {
            return new List<VendorBooking>();
        }

        var bookings = await _unitOfWork.VendorBookingRepository
            .GetVendorBookingsByVendorIdsAsync(vendorIds);

        return bookings.ToList();
    }

    private async Task<(IEnumerable<VendorBooking> Bookings, int TotalCount)> PageBookingsAsync(
        List<int> vendorIds,
        int? vendorId,
        string? status,
        int pageNumber,
        int pageSize)
    {
        var bookings = await LoadBookingsAsync(vendorIds);

        if (vendorId.HasValue)
        {
            bookings = bookings.Where(b => b.VendorId == vendorId.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            bookings = bookings
                .Where(b => string.Equals(b.Status, status, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var totalCount = bookings.Count;

        var page = bookings
            .OrderByDescending(b => b.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (page, totalCount);
    }

    /// <summary>
    /// Same shape and same semantics as the hall side, so a dashboard reads
    /// identically for both businesses. Counts are future-only, revenue is all-time.
    /// ServiceDate is the vendor equivalent of a booking's EventDate.
    /// </summary>
    private static BookingDashboardStatsDto ComputeDashboardStats(List<VendorBooking> bookings)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new BookingDashboardStatsDto
        {
            TodayCount = bookings.Count(b => b.ServiceDate.Date == today),
            UpcomingCount = bookings.Count(b => b.ServiceDate.Date >= tomorrow),
            PendingApprovalCount = bookings.Count(b => b.Status == "Pending" && b.ServiceDate.Date >= today),
            ThisMonthCount = bookings.Count(b => b.ServiceDate >= startOfMonth && b.ServiceDate.Date >= today),
            TotalPaidRevenue = bookings.Where(b => b.IsPaid).Sum(b => b.TotalAmount),
        };
    }
}
