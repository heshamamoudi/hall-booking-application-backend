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
    Task<IEnumerable<Booking>> GetBookingsByHallIdsAsync(IEnumerable<int> hallIds);

    // --- Hall Statistics queries (optimized, read-only) ---

    /// <summary>
    /// Gets the count of bookings for a specific hall, optionally filtered by status groups.
    /// Uses AsNoTracking and server-side COUNT for performance.
    /// </summary>
    Task<int> GetBookingCountByHallIdAsync(int hallId, IEnumerable<string>? statusFilter = null);

    /// <summary>
    /// Gets the most recent bookings for a specific hall with minimal navigation properties.
    /// Uses AsNoTracking for read-only performance.
    /// </summary>
    Task<IEnumerable<Booking>> GetRecentBookingsByHallIdAsync(int hallId, int limit = 5);

    /// <summary>
    /// Gets bookings for a specific hall within a date range for revenue aggregation.
    /// Uses AsNoTracking for read-only performance.
    /// </summary>
    Task<IEnumerable<Booking>> GetBookingsByHallIdAndDateRangeAsync(int hallId, DateTime startDate, DateTime endDate);
}
