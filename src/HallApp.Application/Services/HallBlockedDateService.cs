using HallApp.Application.DTOs.Halls.Availability;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

/// <summary>
/// Service interface for hall blocked dates management.
/// Manages calendar blocked dates, date blocking, and booking conflict checking.
/// Renamed from IHallAvailabilityService to avoid conflict with Core.Interfaces.IServices.IHallAvailabilityService
/// </summary>
public interface IHallBlockedDateService
{
    /// <summary>
    /// Gets all active blocked dates for a specific hall.
    /// </summary>
    /// <param name="hallId">The hall ID</param>
    /// <returns>Collection of blocked dates</returns>
    Task<IEnumerable<BlockedDateDto>> GetBlockedDatesAsync(int hallId);

    /// <summary>
    /// Blocks a date range for a hall after validating no booking conflicts exist.
    /// </summary>
    /// <param name="hallId">The hall ID</param>
    /// <param name="request">Block date request with start/end dates and reason</param>
    /// <param name="userId">The user ID creating the block</param>
    /// <returns>The created blocked date</returns>
    /// <exception cref="InvalidOperationException">Thrown when booking conflicts exist</exception>
    Task<BlockedDateDto> BlockDatesAsync(int hallId, BlockDateRequestDto request, int userId);

    /// <summary>
    /// Unblocks (soft deletes) a blocked date range.
    /// </summary>
    /// <param name="hallId">The hall ID</param>
    /// <param name="blockId">The blocked date ID to remove</param>
    /// <returns>True if successful, false if not found</returns>
    Task<bool> UnblockDatesAsync(int hallId, int blockId);

    /// <summary>
    /// Gets calendar availability data for a specific month, including blocked and booked dates.
    /// </summary>
    /// <param name="hallId">The hall ID</param>
    /// <param name="year">Year to query</param>
    /// <param name="month">Month to query (1-12)</param>
    /// <returns>Calendar availability data</returns>
    Task<CalendarAvailabilityDto> GetCalendarAvailabilityAsync(int hallId, int year, int month);
}

/// <summary>
/// Service implementation for managing hall blocked dates.
/// Handles business logic for date blocking with booking conflict validation.
/// </summary>
public class HallBlockedDateService : IHallBlockedDateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HallBlockedDateService> _logger;

    public HallBlockedDateService(
        IUnitOfWork unitOfWork,
        ILogger<HallBlockedDateService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<BlockedDateDto>> GetBlockedDatesAsync(int hallId)
    {
        _logger.LogInformation("Getting blocked dates for Hall {HallId}", hallId);

        var blockedDates = await _unitOfWork.HallBlockedDateRepository
            .GetActiveBlockedDatesByHallIdAsync(hallId);

        return blockedDates.Select(MapToDto).ToList();
    }

    public async Task<BlockedDateDto> BlockDatesAsync(int hallId, BlockDateRequestDto request, int userId)
    {
        _logger.LogInformation(
            "Blocking dates for Hall {HallId} from {StartDate} to {EndDate} by User {UserId}",
            hallId, request.StartDate, request.EndDate, userId);

        // Check for booking conflicts
        var conflictingBookings = await GetBookingsInDateRange(hallId, request.StartDate, request.EndDate);
        if (conflictingBookings.Any())
        {
            var conflictDates = string.Join(", ", conflictingBookings.Select(b => b.EventDate.ToString("yyyy-MM-dd")));
            _logger.LogWarning(
                "Cannot block dates for Hall {HallId}: Conflicting bookings exist on {ConflictDates}",
                hallId, conflictDates);

            throw new InvalidOperationException(
                $"Cannot block these dates. There are {conflictingBookings.Count()} confirmed or pending booking(s) in this date range.");
        }

        // Create blocked date entity
        var blockedDate = new HallBlockedDate
        {
            HallId = hallId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            BlockedByUserId = userId,
            BlockedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.HallBlockedDateRepository.AddAsync(blockedDate);
        await _unitOfWork.Complete();

        _logger.LogInformation("Successfully blocked dates for Hall {HallId}, Block ID {BlockId}", hallId, blockedDate.Id);

        // Reload with user information
        var createdBlock = await _unitOfWork.HallBlockedDateRepository.GetBlockedDateByIdAsync(blockedDate.Id);
        return MapToDto(createdBlock!);
    }

    public async Task<bool> UnblockDatesAsync(int hallId, int blockId)
    {
        _logger.LogInformation("Unblocking dates for Hall {HallId}, Block ID {BlockId}", hallId, blockId);

        var blockedDate = await _unitOfWork.HallBlockedDateRepository.GetBlockedDateByIdAsync(blockId);

        if (blockedDate == null || blockedDate.HallId != hallId || !blockedDate.IsActive)
        {
            _logger.LogWarning("Blocked date not found or already inactive: Hall {HallId}, Block ID {BlockId}", hallId, blockId);
            return false;
        }

        // Soft delete
        blockedDate.IsActive = false;
        blockedDate.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.HallBlockedDateRepository.Update(blockedDate);
        await _unitOfWork.Complete();

        _logger.LogInformation("Successfully unblocked dates for Hall {HallId}, Block ID {BlockId}", hallId, blockId);
        return true;
    }

    public async Task<CalendarAvailabilityDto> GetCalendarAvailabilityAsync(int hallId, int year, int month)
    {
        _logger.LogInformation("Getting calendar availability for Hall {HallId}, Year {Year}, Month {Month}",
            hallId, year, month);

        // Get blocked dates for the month
        var blockedDates = await _unitOfWork.HallBlockedDateRepository
            .GetBlockedDatesByMonthAsync(hallId, year, month);

        // Get bookings for the month
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var bookings = await _unitOfWork.BookingRepository
            .GetBookingsByHallIdAndDateRangeAsync(hallId, monthStart, monthEnd);

        // Map to DTOs
        var blockedDateDtos = blockedDates.Select(MapToDto).ToList();
        var bookedDateDtos = bookings
            .Where(b => !IsCancelledStatus(b.Status)) // Exclude cancelled bookings
            .Select(b => new BookedDateInfoDto
            {
                EventDate = b.EventDate,
                Status = b.Status,
                CustomerName = b.Customer?.AppUser != null
                    ? $"{b.Customer.AppUser.FirstName} {b.Customer.AppUser.LastName}".Trim()
                    : "Unknown",
                EventType = b.EventType
            })
            .ToList();

        return new CalendarAvailabilityDto
        {
            Year = year,
            Month = month,
            BlockedDates = blockedDateDtos,
            BookedDates = bookedDateDtos
        };
    }

    /// <summary>
    /// Gets bookings in the specified date range that would conflict with blocking.
    /// Returns bookings with Confirmed or Pending status (excludes Cancelled/Rejected).
    /// </summary>
    private async Task<IEnumerable<Core.Entities.BookingEntities.Booking>> GetBookingsInDateRange(
        int hallId, DateOnly startDate, DateOnly endDate)
    {
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var allBookings = await _unitOfWork.BookingRepository
            .GetBookingsByHallIdAndDateRangeAsync(hallId, startDateTime, endDateTime);

        // Filter to only active bookings (not cancelled or rejected)
        return allBookings.Where(b => !IsCancelledStatus(b.Status));
    }

    /// <summary>
    /// Checks if a booking status represents a cancelled/rejected booking.
    /// </summary>
    private static bool IsCancelledStatus(string status)
    {
        var cancelledStatuses = new[] { "Cancelled", "Rejected", "CancelledByCustomer", "CancelledByManager" };
        return cancelledStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Maps HallBlockedDate entity to BlockedDateDto.
    /// </summary>
    private static BlockedDateDto MapToDto(HallBlockedDate entity)
    {
        return new BlockedDateDto
        {
            Id = entity.Id,
            HallId = entity.HallId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Reason = entity.Reason,
            BlockedByUserName = entity.BlockedByUser != null
                ? $"{entity.BlockedByUser.FirstName} {entity.BlockedByUser.LastName}".Trim()
                : "Unknown",
            BlockedAt = entity.BlockedAt
        };
    }
}
