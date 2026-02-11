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
    private readonly ILogger<HallManagerBookingService> _logger;

    public HallManagerBookingService(
        IUnitOfWork unitOfWork,
        ILogger<HallManagerBookingService> logger)
    {
        _unitOfWork = unitOfWork;
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
}
