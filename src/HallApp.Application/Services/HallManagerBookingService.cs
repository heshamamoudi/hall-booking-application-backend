using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

/// <summary>
/// Service for HallManager-specific booking operations.
/// Follows Single Responsibility Principle - handles only booking retrieval
/// for halls managed by a specific hall manager.
/// </summary>
public class HallManagerBookingService : IHallManagerBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<HallManagerBookingService> _logger;

    public HallManagerBookingService(
        IUnitOfWork unitOfWork,
        IOrganizationService organizationService,
        ILogger<HallManagerBookingService> logger)
    {
        _unitOfWork = unitOfWork;
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Booking>> GetBookingsForManagedHallsAsync(
        int appUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching bookings for HallManager with AppUserId {AppUserId}", appUserId);

        // CRIT-001 FIX: Direct query instead of GetAllAsync().FirstOrDefault()
        var hallManager = await _unitOfWork.HallManagerRepository.GetByAppUserIdWithHallsAsync(appUserId);

        if (hallManager?.Halls == null || !hallManager.Halls.Any())
        {
            _logger.LogWarning(
                "HallManager with AppUserId {AppUserId} not found or has no assigned halls", appUserId);
            return Enumerable.Empty<Booking>();
        }

        var hallIds = hallManager.Halls.Select(h => h.ID).ToList();
        _logger.LogDebug(
            "HallManager manages {HallCount} halls: [{HallIds}]",
            hallIds.Count, string.Join(", ", hallIds));

        // Use the dedicated repository method for efficient database query
        var managerBookings = await _unitOfWork.BookingRepository.GetBookingsByHallIdsAsync(hallIds);

        _logger.LogInformation(
            "Retrieved {BookingCount} bookings for HallManager {AppUserId}",
            managerBookings.Count(), appUserId);

        return managerBookings;
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<Booking> Bookings, int TotalCount)> GetBookingsForManagedHallsPagedAsync(
        int appUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching paged bookings (page {Page}, size {Size}) for HallManager with AppUserId {AppUserId}",
            pageNumber, pageSize, appUserId);

        var hallManager = await _unitOfWork.HallManagerRepository.GetByAppUserIdWithHallsAsync(appUserId);

        if (hallManager?.Halls == null || !hallManager.Halls.Any())
        {
            _logger.LogWarning(
                "HallManager with AppUserId {AppUserId} not found or has no assigned halls", appUserId);
            return (Enumerable.Empty<Booking>(), 0);
        }

        var hallIds = hallManager.Halls.Select(h => h.ID).ToList();

        // Fetch all bookings for the managed halls, then paginate in memory
        // A future optimization could push pagination to the repository layer
        var allBookings = (await _unitOfWork.BookingRepository.GetBookingsByHallIdsAsync(hallIds)).ToList();
        var totalCount = allBookings.Count;

        var pagedBookings = allBookings
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        _logger.LogInformation(
            "Returned {PageCount}/{TotalCount} bookings (page {Page}) for HallManager {AppUserId}",
            pagedBookings.Count, totalCount, pageNumber, appUserId);

        return (pagedBookings, totalCount);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<Booking> Bookings, int TotalCount)> GetBookingsForOrganizationPagedAsync(
        int appUserId,
        int? hallId,
        string? city,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching org bookings (page {Page}, size {Size}, hallId {HallId}, city {City}) for user {AppUserId}",
            pageNumber, pageSize, hallId, city, appUserId);

        // Step 1: Find the organization where the user is the owner
        var organization = await _organizationService.GetOrganizationByOwnerId(appUserId);

        if (organization == null)
        {
            _logger.LogWarning(
                "No organization found for user {AppUserId}", appUserId);
            return (Enumerable.Empty<Booking>(), 0);
        }

        // Step 2: Get all halls belonging to this organization (includes Location via IncludeForDetails)
        var orgHalls = await _unitOfWork.HallRepository.GetHallsByOrganizationIdAsync(organization.Id);

        if (orgHalls == null || orgHalls.Count == 0)
        {
            _logger.LogWarning(
                "Organization {OrgId} for user {AppUserId} has no halls", organization.Id, appUserId);
            return (Enumerable.Empty<Booking>(), 0);
        }

        // Step 3: Apply optional hallId filter
        var filteredHalls = orgHalls.AsEnumerable();

        if (hallId.HasValue)
        {
            filteredHalls = filteredHalls.Where(h => h.ID == hallId.Value);
        }

        // Step 4: Apply optional city filter (case-insensitive)
        if (!string.IsNullOrWhiteSpace(city))
        {
            filteredHalls = filteredHalls.Where(h =>
                h.Location != null &&
                h.Location.City.Equals(city, StringComparison.OrdinalIgnoreCase));
        }

        var hallIds = filteredHalls.Select(h => h.ID).ToList();

        if (hallIds.Count == 0)
        {
            _logger.LogInformation(
                "No halls matched filters (hallId={HallId}, city={City}) in organization {OrgId}",
                hallId, city, organization.Id);
            return (Enumerable.Empty<Booking>(), 0);
        }

        _logger.LogDebug(
            "Organization {OrgId} has {HallCount} halls matching filters: [{HallIds}]",
            organization.Id, hallIds.Count, string.Join(", ", hallIds));

        // Step 5: Get bookings for the filtered hall IDs
        var allBookings = (await _unitOfWork.BookingRepository.GetBookingsByHallIdsAsync(hallIds)).ToList();
        var totalCount = allBookings.Count;

        // Step 6: Apply pagination in memory (same pattern as GetBookingsForManagedHallsPagedAsync)
        var pagedBookings = allBookings
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        _logger.LogInformation(
            "Returned {PageCount}/{TotalCount} org bookings (page {Page}) for user {AppUserId}",
            pagedBookings.Count, totalCount, pageNumber, appUserId);

        return (pagedBookings, totalCount);
    }

    /// <inheritdoc />
    public async Task<BookingDashboardStatsDto> GetHallManagerDashboardStatsAsync(
        int appUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching dashboard stats for HallManager with AppUserId {AppUserId}", appUserId);

        var hallManager = await _unitOfWork.HallManagerRepository.GetByAppUserIdWithHallsAsync(appUserId);

        if (hallManager?.Halls == null || !hallManager.Halls.Any())
        {
            _logger.LogWarning(
                "HallManager with AppUserId {AppUserId} not found or has no assigned halls", appUserId);
            return new BookingDashboardStatsDto();
        }

        var hallIds = hallManager.Halls.Select(h => h.ID).ToList();
        var bookings = (await _unitOfWork.BookingRepository.GetBookingsByHallIdsAsync(hallIds)).ToList();

        var stats = ComputeDashboardStats(bookings);

        _logger.LogInformation(
            "Computed dashboard stats for HallManager {AppUserId}: Today={TodayCount}, Upcoming={UpcomingCount}, Pending={PendingCount}, ThisMonth={ThisMonthCount}, Revenue={Revenue}",
            appUserId, stats.TodayCount, stats.UpcomingCount, stats.PendingApprovalCount, stats.ThisMonthCount, stats.TotalPaidRevenue);

        return stats;
    }

    /// <inheritdoc />
    public async Task<BookingDashboardStatsDto> GetOrgManagerDashboardStatsAsync(
        int appUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching dashboard stats for OrgManager with AppUserId {AppUserId}", appUserId);

        var organization = await _organizationService.GetOrganizationByOwnerId(appUserId);

        if (organization == null)
        {
            _logger.LogWarning(
                "No organization found for user {AppUserId}", appUserId);
            return new BookingDashboardStatsDto();
        }

        var orgHalls = await _unitOfWork.HallRepository.GetHallsByOrganizationIdAsync(organization.Id);

        if (orgHalls == null || orgHalls.Count == 0)
        {
            _logger.LogWarning(
                "Organization {OrgId} for user {AppUserId} has no halls", organization.Id, appUserId);
            return new BookingDashboardStatsDto();
        }

        var hallIds = orgHalls.Select(h => h.ID).ToList();
        var bookings = (await _unitOfWork.BookingRepository.GetBookingsByHallIdsAsync(hallIds)).ToList();

        var stats = ComputeDashboardStats(bookings);

        _logger.LogInformation(
            "Computed dashboard stats for OrgManager {AppUserId} (Org {OrgId}): Today={TodayCount}, Upcoming={UpcomingCount}, Pending={PendingCount}, ThisMonth={ThisMonthCount}, Revenue={Revenue}",
            appUserId, organization.Id, stats.TodayCount, stats.UpcomingCount, stats.PendingApprovalCount, stats.ThisMonthCount, stats.TotalPaidRevenue);

        return stats;
    }

    /// <summary>
    /// Computes dashboard statistics from a list of bookings.
    /// Counts filter to future bookings (EventDate >= today). Revenue is all-time paid.
    /// </summary>
    private static BookingDashboardStatsDto ComputeDashboardStats(List<Booking> bookings)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new BookingDashboardStatsDto
        {
            TodayCount = bookings.Count(b => b.EventDate.Date == today),
            UpcomingCount = bookings.Count(b => b.EventDate.Date >= tomorrow),
            PendingApprovalCount = bookings.Count(b => b.Status == "Pending" && b.EventDate.Date >= today),
            ThisMonthCount = bookings.Count(b => b.EventDate >= startOfMonth && b.EventDate.Date >= today),
            TotalPaidRevenue = bookings.Where(b => b.PaymentStatus == "Paid").Sum(b => b.TotalAmount),
        };
    }
}
