using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IVendorBookingRepository : IGenericRepository<VendorBooking>
{
    Task<IEnumerable<VendorBooking>> GetBookingsByVendorIdAsync(int vendorId);
    Task<IEnumerable<VendorBooking>> GetBookingsByDateRangeAsync(int vendorId, DateTime startDate, DateTime endDate);
    Task<VendorBooking> GetBookingWithDetailsAsync(int bookingId);
    Task<bool> IsVendorAvailableAsync(int vendorId, DateTime startTime, DateTime endTime);
    Task<IEnumerable<VendorBooking>> GetPendingBookingsAsync(int vendorId);
}
