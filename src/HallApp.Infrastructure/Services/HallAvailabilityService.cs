using HallApp.Core.Interfaces.IServices;
using HallApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HallApp.Infrastructure.Services;

public class HallAvailabilityService : IHallAvailabilityService
{
    private readonly DataContext _context;
    private readonly ILogger<HallAvailabilityService> _logger;

    public HallAvailabilityService(DataContext context, ILogger<HallAvailabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Check if a hall is available for the specified date and time range
    /// </summary>
    public async Task<bool> IsAvailableAsync(int hallId, DateTime eventDate, TimeSpan startTime, TimeSpan endTime)
    {
        try
        {
            // Check if hall exists and is active
            var hall = await _context.Halls.FindAsync(hallId);
            if (hall == null || !hall.IsActive)
            {
                _logger.LogWarning("Hall {HallId} not found or inactive", hallId);
                return false;
            }

            // Check for overlapping bookings
            var hasConflict = await _context.Bookings
                .Where(b => b.HallId == hallId &&
                           b.EventDate.Date == eventDate.Date &&
                           b.Status != "Cancelled" &&
                           b.Status != "Rejected" &&
                           // Check for time overlap
                           ((b.StartTime < endTime && b.EndTime > startTime)))
                .AnyAsync();

            if (hasConflict)
            {
                _logger.LogInformation(
                    "Hall {HallId} has conflicting booking on {EventDate} from {StartTime} to {EndTime}",
                    hallId, eventDate.Date, startTime, endTime);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability for hall {HallId}", hallId);
            return false;
        }
    }

    /// <summary>
    /// Get available time slots for a hall on a specific date
    /// </summary>
    public async Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(int hallId, DateTime date)
    {
        try
        {
            var availableSlots = new List<TimeSlot>();

            // Hall operating hours (typically 8 AM to 11 PM in Saudi Arabia)
            var openingTime = new TimeSpan(8, 0, 0);
            var closingTime = new TimeSpan(23, 0, 0);

            // Get all bookings for this hall on the specified date
            var existingBookings = await _context.Bookings
                .Where(b => b.HallId == hallId &&
                           b.EventDate.Date == date.Date &&
                           b.Status != "Cancelled" &&
                           b.Status != "Rejected")
                .OrderBy(b => b.StartTime)
                .Select(b => new { b.StartTime, b.EndTime })
                .ToListAsync();

            // If no bookings, entire day is available
            if (!existingBookings.Any())
            {
                availableSlots.Add(new TimeSlot
                {
                    StartTime = openingTime,
                    EndTime = closingTime,
                    IsAvailable = true
                });
                return availableSlots;
            }

            // Find gaps between bookings
            var currentTime = openingTime;

            foreach (var booking in existingBookings)
            {
                // If there's a gap before this booking
                if (currentTime < booking.StartTime)
                {
                    availableSlots.Add(new TimeSlot
                    {
                        StartTime = currentTime,
                        EndTime = booking.StartTime,
                        IsAvailable = true
                    });
                }

                // Mark booked slot
                availableSlots.Add(new TimeSlot
                {
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    IsAvailable = false
                });

                currentTime = booking.EndTime;
            }

            // Add remaining time after last booking
            if (currentTime < closingTime)
            {
                availableSlots.Add(new TimeSlot
                {
                    StartTime = currentTime,
                    EndTime = closingTime,
                    IsAvailable = true
                });
            }

            return availableSlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available time slots for hall {HallId} on {Date}", hallId, date);
            return new List<TimeSlot>();
        }
    }

    /// <summary>
    /// Get all available dates for a hall within a date range
    /// </summary>
    public async Task<List<DateTime>> GetAvailableDatesAsync(int hallId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var availableDates = new List<DateTime>();

            // Get all bookings in the date range
            var bookings = await _context.Bookings
                .Where(b => b.HallId == hallId &&
                           b.EventDate.Date >= startDate.Date &&
                           b.EventDate.Date <= endDate.Date &&
                           b.Status != "Cancelled" &&
                           b.Status != "Rejected")
                .Select(b => b.EventDate.Date)
                .Distinct()
                .ToListAsync();

            // Generate all dates in range
            var allDates = Enumerable.Range(0, (endDate.Date - startDate.Date).Days + 1)
                .Select(offset => startDate.Date.AddDays(offset))
                .ToList();

            // Filter out fully booked dates
            foreach (var date in allDates)
            {
                if (!bookings.Contains(date.Date))
                {
                    availableDates.Add(date);
                }
                else
                {
                    // Check if there are any available slots on this date
                    var slots = await GetAvailableTimeSlotsAsync(hallId, date);
                    if (slots.Any(s => s.IsAvailable))
                    {
                        availableDates.Add(date);
                    }
                }
            }

            return availableDates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available dates for hall {HallId}", hallId);
            return new List<DateTime>();
        }
    }

    /// <summary>
    /// Check if multiple halls are available for the same time slot
    /// </summary>
    public async Task<List<int>> GetAvailableHallsAsync(
        List<int> hallIds,
        DateTime eventDate,
        TimeSpan startTime,
        TimeSpan endTime)
    {
        var availableHalls = new List<int>();

        foreach (var hallId in hallIds)
        {
            if (await IsAvailableAsync(hallId, eventDate, startTime, endTime))
            {
                availableHalls.Add(hallId);
            }
        }

        return availableHalls;
    }

    /// <summary>
    /// Validate booking time (must be in future, within operating hours, etc.)
    /// </summary>
    public async Task<(bool IsValid, string ErrorMessage)> ValidateBookingTimeAsync(
        int hallId,
        DateTime eventDate,
        TimeSpan startTime,
        TimeSpan endTime)
    {
        // Check if event date is in the past
        if (eventDate.Date < DateTime.UtcNow.Date)
        {
            return (false, "Event date cannot be in the past");
        }

        // Check if event date is too far in the future (e.g., 1 year)
        if (eventDate.Date > DateTime.UtcNow.Date.AddYears(1))
        {
            return (false, "Event date cannot be more than 1 year in advance");
        }

        // Check if start time is before end time
        if (startTime >= endTime)
        {
            return (false, "Start time must be before end time");
        }

        // Minimum booking duration (e.g., 2 hours)
        var duration = endTime - startTime;
        if (duration.TotalHours < 2)
        {
            return (false, "Minimum booking duration is 2 hours");
        }

        // Maximum booking duration (e.g., 16 hours)
        if (duration.TotalHours > 16)
        {
            return (false, "Maximum booking duration is 16 hours");
        }

        // Check operating hours
        var openingTime = new TimeSpan(8, 0, 0);
        var closingTime = new TimeSpan(23, 0, 0);

        if (startTime < openingTime || endTime > closingTime)
        {
            return (false, $"Hall operating hours are from {openingTime:hh\\:mm} to {closingTime:hh\\:mm}");
        }

        // Check if hall is available
        if (!await IsAvailableAsync(hallId, eventDate, startTime, endTime))
        {
            return (false, "Hall is not available for the selected date and time");
        }

        return (true, string.Empty);
    }
}
