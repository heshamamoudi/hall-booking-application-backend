using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Application.Services;

public class VendorAvailabilityService : IVendorAvailabilityService
{
    private readonly IUnitOfWork _unitOfWork;

    public VendorAvailabilityService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Business Hours
    public async Task<VendorBusinessHour> AddBusinessHourAsync(int vendorId, VendorBusinessHour businessHour)
    {
        businessHour.VendorId = vendorId;
        await _unitOfWork.VendorAvailabilityRepository.AddBusinessHourAsync(businessHour);
        await _unitOfWork.Complete();
        return businessHour;
    }

    public async Task<VendorBusinessHour> UpdateBusinessHourAsync(int vendorId, DayOfWeek dayOfWeek, VendorBusinessHour businessHour)
    {
        businessHour.VendorId = vendorId;
        await _unitOfWork.VendorAvailabilityRepository.UpdateBusinessHourAsync(businessHour);
        await _unitOfWork.Complete();
        return businessHour;
    }

    public async Task DeleteBusinessHourAsync(int vendorId, DayOfWeek dayOfWeek)
    {
        await _unitOfWork.VendorAvailabilityRepository.DeleteBusinessHourAsync(vendorId, (int)dayOfWeek);
        await _unitOfWork.Complete();
    }

    public async Task<List<VendorBusinessHour>> GetBusinessHoursAsync(int vendorId)
    {
        var hours = await _unitOfWork.VendorAvailabilityRepository.GetBusinessHoursAsync(vendorId);
        return hours.ToList();
    }

    // Blocked Dates
    public async Task<VendorBlockedDate> AddBlockedDateAsync(int vendorId, VendorBlockedDate blockedDate)
    {
        blockedDate.VendorId = vendorId;
        await _unitOfWork.VendorAvailabilityRepository.AddBlockedDateAsync(blockedDate);
        await _unitOfWork.Complete();
        return blockedDate;
    }

    public async Task<VendorBlockedDate> UpdateBlockedDateAsync(int vendorId, int blockedDateId, VendorBlockedDate blockedDate)
    {
        var existingDate = await _unitOfWork.VendorAvailabilityRepository.GetBlockedDateByIdAsync(blockedDateId);
        if (existingDate == null || existingDate.VendorId != vendorId) 
            throw new Exception($"BlockedDate with id {blockedDateId} not found for vendor {vendorId}");
        
        existingDate.StartDate = blockedDate.StartDate;
        existingDate.EndDate = blockedDate.EndDate;
        existingDate.Reason = blockedDate.Reason;
        existingDate.IsRecurring = blockedDate.IsRecurring;
        
        _unitOfWork.VendorAvailabilityRepository.UpdateBlockedDate(existingDate);
        await _unitOfWork.Complete();
        return existingDate;
    }

    public async Task DeleteBlockedDateAsync(int vendorId, int blockedDateId)
    {
        var date = await _unitOfWork.VendorAvailabilityRepository.GetBlockedDateByIdAsync(blockedDateId);
        if (date != null && date.VendorId == vendorId)
        {
            await _unitOfWork.VendorAvailabilityRepository.DeleteBlockedDateAsync(vendorId, blockedDateId);
            await _unitOfWork.Complete();
        }
    }

    public async Task<List<VendorBlockedDate>> GetBlockedDatesAsync(int vendorId)
    {
        var dates = await _unitOfWork.VendorAvailabilityRepository.GetBlockedDatesByVendorAsync(vendorId);
        return dates.ToList();
    }

    public async Task<List<VendorBlockedDate>> GetBlockedDatesAsync(int vendorId, DateTime startDate, DateTime endDate)
    {
        var dates = await _unitOfWork.VendorAvailabilityRepository.GetBlockedDatesInRangeAsync(vendorId, startDate, endDate);
        return dates.ToList();
    }

    // Availability Checks
    public async Task<bool> IsVendorAvailableAsync(int vendorId, DateTime date)
    {
        // "Available on this date" means the vendor is open that day and has not
        // blocked it out - NOT that it is free for all 24 hours. This used to ask
        // for a 00:00-24:00 window, which cannot fit inside any real set of
        // business hours, so every whole-day question answered "unavailable".
        var blocked = await _unitOfWork.VendorAvailabilityRepository.GetBlockedDateAsync(vendorId, date);
        if (blocked != null)
            return false;

        var businessHour = await _unitOfWork.VendorAvailabilityRepository
            .GetBusinessHourAsync(vendorId, (int)date.DayOfWeek);

        return businessHour is { IsClosed: false };
    }

    public async Task<bool> IsVendorAvailableAsync(int vendorId, DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        return await _unitOfWork.VendorAvailabilityRepository.IsVendorAvailableAsync(vendorId, date, startTime, endTime);
    }

    public async Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(int vendorId, DateTime date, int durationMinutes)
    {
        var duration = TimeSpan.FromMinutes(durationMinutes);
        var timeSlots = await _unitOfWork.VendorAvailabilityRepository.GetAvailableTimeSlotsAsync(vendorId, date, duration);
        return timeSlots.ToList();
    }
}
