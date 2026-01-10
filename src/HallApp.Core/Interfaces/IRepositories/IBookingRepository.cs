using HallApp.Core.Entities.BookingEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IBookingRepository : IGenericRepository<Booking>
{
    Task<IEnumerable<Booking>> GetBookingsByCustomerIdAsync(int customerId);
    Task<IEnumerable<Booking>> GetBookingsByHallIdAsync(int hallId);
    Task<IEnumerable<Booking>> GetBookingsByVendorIdAsync(int vendorId);
    Task<IEnumerable<Booking>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<Booking> GetBookingWithDetailsAsync(int bookingId);
    Task<IEnumerable<Booking>> GetPendingBookingsAsync();
    Task<IEnumerable<Booking>> GetConfirmedBookingsAsync();
}
