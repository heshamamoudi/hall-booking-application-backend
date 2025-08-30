#nullable enable
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class VendorAvailabilityRepository : IVendorAvailabilityRepository
{
    private readonly DataContext _context;

    public VendorAvailabilityRepository(DataContext context)
    {
        _context = context;
    }

    // Business Hours
    public async Task<IEnumerable<VendorBusinessHour>> GetBusinessHoursByVendorAsync(int vendorId)
    {
        return await _context.VendorBusinessHours
            .Where(h => h.VendorId == vendorId)
            .OrderBy(h => h.DayOfWeek)
            .ToListAsync();
    }

    public async Task<IEnumerable<VendorBusinessHour>> GetBusinessHoursAsync(int vendorId)
    {
        return await GetBusinessHoursByVendorAsync(vendorId);
    }

    public async Task<VendorBusinessHour?> GetBusinessHourAsync(int vendorId, int dayOfWeek)
    {
        return await _context.VendorBusinessHours
            .FirstOrDefaultAsync(h => h.VendorId == vendorId && (int)h.DayOfWeek == dayOfWeek);
    }

    public async Task AddBusinessHourAsync(VendorBusinessHour businessHour)
    {
        await _context.VendorBusinessHours.AddAsync(businessHour);
    }

    public async Task UpdateBusinessHourAsync(VendorBusinessHour businessHour)
    {
        _context.VendorBusinessHours.Update(businessHour);
        await Task.CompletedTask;
    }

    public async Task DeleteBusinessHourAsync(int vendorId, int businessHourId)
    {
        var businessHour = await _context.VendorBusinessHours
            .FirstOrDefaultAsync(h => h.VendorId == vendorId && h.Id == businessHourId);

        if (businessHour != null)
        {
            _context.VendorBusinessHours.Remove(businessHour);
        }
    }

    public void AddBusinessHour(VendorBusinessHour businessHour)
    {
        _context.VendorBusinessHours.Add(businessHour);
    }

    public void UpdateBusinessHour(VendorBusinessHour businessHour)
    {
        _context.VendorBusinessHours.Update(businessHour);
    }

    public void DeleteBusinessHour(VendorBusinessHour businessHour)
    {
        _context.VendorBusinessHours.Remove(businessHour);
    }

    // Blocked Dates
    public async Task<IEnumerable<VendorBlockedDate>> GetBlockedDatesByVendorAsync(int vendorId)
    {
        return await _context.VendorBlockedDates
            .Where(bd => bd.VendorId == vendorId)
            .OrderBy(bd => bd.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<VendorBlockedDate>> GetBlockedDatesInRangeAsync(int vendorId, DateTime startDate, DateTime endDate)
    {
        return await _context.VendorBlockedDates
            .Where(bd => bd.VendorId == vendorId && bd.StartDate <= endDate && bd.EndDate >= startDate)
            .OrderBy(bd => bd.StartDate)
            .ToListAsync();
    }

    public async Task<VendorBlockedDate?> GetBlockedDateAsync(int vendorId, DateTime date)
    {
        return await _context.VendorBlockedDates
            .FirstOrDefaultAsync(bd => bd.VendorId == vendorId && bd.StartDate <= date && bd.EndDate >= date);
    }

    public async Task<VendorBlockedDate?> GetBlockedDateByIdAsync(int blockedDateId)
    {
        return await _context.VendorBlockedDates
            .FirstOrDefaultAsync(bd => bd.Id == blockedDateId);
    }

    public async Task AddBlockedDateAsync(VendorBlockedDate blockedDate)
    {
        await _context.VendorBlockedDates.AddAsync(blockedDate);
    }

    public async Task DeleteBlockedDateAsync(int vendorId, int blockedDateId)
    {
        var blockedDate = await _context.VendorBlockedDates
            .FirstOrDefaultAsync(bd => bd.VendorId == vendorId && bd.Id == blockedDateId);

        if (blockedDate != null)
        {
            _context.VendorBlockedDates.Remove(blockedDate);
        }
    }

    public void AddBlockedDate(VendorBlockedDate blockedDate)
    {
        _context.VendorBlockedDates.Add(blockedDate);
    }

    public void UpdateBlockedDate(VendorBlockedDate blockedDate)
    {
        _context.VendorBlockedDates.Update(blockedDate);
    }

    public void DeleteBlockedDate(VendorBlockedDate blockedDate)
    {
        _context.VendorBlockedDates.Remove(blockedDate);
    }

    // Availability Checks
    public async Task<bool> IsVendorAvailableAsync(int vendorId, DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        // Check if vendor has blocked dates on this date
        var blockedDate = await GetBlockedDateAsync(vendorId, date);
        if (blockedDate != null)
            return false;

        // Check business hours for the day of week
        var dayOfWeek = (int)date.DayOfWeek;
        var businessHour = await GetBusinessHourAsync(vendorId, dayOfWeek);
        
        if (businessHour == null || businessHour.IsClosed || !businessHour.IsOpen)
            return false;

        // Check if requested time is within business hours
        if (startTime < businessHour.OpenTime || endTime > businessHour.CloseTime)
            return false;

        return true;
    }

    public async Task<IEnumerable<TimeSpan>> GetAvailableTimeSlotsAsync(int vendorId, DateTime date, TimeSpan duration)
    {
        var timeSlots = new List<TimeSpan>();
        
        var dayOfWeek = (int)date.DayOfWeek;
        var businessHour = await GetBusinessHourAsync(vendorId, dayOfWeek);
        
        if (businessHour == null || businessHour.IsClosed || !businessHour.IsOpen)
            return timeSlots;

        var currentTime = businessHour.OpenTime;
        var endTime = businessHour.CloseTime;

        while (currentTime.Add(duration) <= endTime)
        {
            var slotEndTime = currentTime.Add(duration);
            if (await IsVendorAvailableAsync(vendorId, date, currentTime, slotEndTime))
            {
                timeSlots.Add(currentTime);
            }
            currentTime = currentTime.Add(TimeSpan.FromMinutes(30)); // 30-minute intervals
        }

        return timeSlots;
    }
}
