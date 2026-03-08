using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IVendorAvailabilityRepository
{
    // Business Hours
    Task<IEnumerable<VendorBusinessHour>> GetBusinessHoursByVendorAsync(int vendorId);
    Task<IEnumerable<VendorBusinessHour>> GetBusinessHoursAsync(int vendorId);
    Task<VendorBusinessHour?> GetBusinessHourAsync(int vendorId, int dayOfWeek);
    Task AddBusinessHourAsync(VendorBusinessHour businessHour);
    Task UpdateBusinessHourAsync(VendorBusinessHour businessHour);
    Task DeleteBusinessHourAsync(int vendorId, int businessHourId);
    void AddBusinessHour(VendorBusinessHour businessHour);
    void UpdateBusinessHour(VendorBusinessHour businessHour);
    void DeleteBusinessHour(VendorBusinessHour businessHour);

    // Blocked Dates
    Task<IEnumerable<VendorBlockedDate>> GetBlockedDatesByVendorAsync(int vendorId);
    Task<IEnumerable<VendorBlockedDate>> GetBlockedDatesInRangeAsync(int vendorId, DateTime startDate, DateTime endDate);
    Task<VendorBlockedDate?> GetBlockedDateAsync(int vendorId, DateTime date);
    Task<VendorBlockedDate?> GetBlockedDateByIdAsync(int blockedDateId);
    Task AddBlockedDateAsync(VendorBlockedDate blockedDate);
    Task DeleteBlockedDateAsync(int vendorId, int blockedDateId);
    void AddBlockedDate(VendorBlockedDate blockedDate);
    void UpdateBlockedDate(VendorBlockedDate blockedDate);
    void DeleteBlockedDate(VendorBlockedDate blockedDate);

    // Availability Checks
    Task<bool> IsVendorAvailableAsync(int vendorId, DateTime date, TimeSpan startTime, TimeSpan endTime);
    Task<IEnumerable<TimeSpan>> GetAvailableTimeSlotsAsync(int vendorId, DateTime date, TimeSpan duration);
}
