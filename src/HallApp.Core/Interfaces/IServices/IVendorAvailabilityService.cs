using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IServices;

public interface IVendorAvailabilityService
{
    // Business Hours
    Task<VendorBusinessHour> AddBusinessHourAsync(int vendorId, VendorBusinessHour businessHour);
    Task<VendorBusinessHour> UpdateBusinessHourAsync(int vendorId, DayOfWeek dayOfWeek, VendorBusinessHour businessHour);
    Task DeleteBusinessHourAsync(int vendorId, DayOfWeek dayOfWeek);
    Task<List<VendorBusinessHour>> GetBusinessHoursAsync(int vendorId);
    
    // Blocked Dates
    Task<VendorBlockedDate> AddBlockedDateAsync(int vendorId, VendorBlockedDate blockedDate);
    Task<VendorBlockedDate> UpdateBlockedDateAsync(int vendorId, int blockedDateId, VendorBlockedDate blockedDate);
    Task DeleteBlockedDateAsync(int vendorId, int blockedDateId);
    Task<List<VendorBlockedDate>> GetBlockedDatesAsync(int vendorId);
    Task<List<VendorBlockedDate>> GetBlockedDatesAsync(int vendorId, DateTime startDate, DateTime endDate);
    
    // Availability Checks
    Task<bool> IsVendorAvailableAsync(int vendorId, DateTime date);
    Task<bool> IsVendorAvailableAsync(int vendorId, DateTime date, TimeSpan? startTime, TimeSpan? endTime);
    Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(int vendorId, DateTime date, int durationMinutes);
}
