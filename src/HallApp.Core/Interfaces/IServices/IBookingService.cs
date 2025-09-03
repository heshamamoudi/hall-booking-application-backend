using HallApp.Core.Entities.BookingEntities;

// Forward declarations for DTOs
public class BookingStatisticsDto
{
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal ThisMonthRevenue { get; set; }
}

public class HallAvailabilityDto
{
    public int HallId { get; set; }
    public string HallName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsAvailable { get; set; }
}

namespace HallApp.Core.Interfaces.IServices
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(Booking booking);
        Task<Booking> GetBookingByIdAsync(int bookingId);
        Task<IEnumerable<Booking>> GetBookingsByCustomerIdAsync(string customerId);
        Task<IEnumerable<Booking>> GetAllBookingsAsync();
        Task<Booking> UpdateBookingAsync(Booking booking);
        Task<Booking> UpdateCustomerBookingAsync(string customerId, Booking booking);
        Task<bool> CancelBookingAsync(int bookingId);
        Task<IEnumerable<Booking>> GetBookingsByHallIdAsync(int hallId);
        Task<IEnumerable<Booking>> GetBookingsByStatusAsync(string status);
        Task<IEnumerable<Booking>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> ValidateBookingAsync(Booking booking);
        Task<decimal> CalculateBookingCostAsync(int hallId, DateTime startDate, DateTime endDate);
        Task<bool> IsHallAvailableAsync(int hallId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<Booking>> GetUpcomingBookingsAsync(string customerId);
        Task<IEnumerable<Booking>> GetPastBookingsAsync(string customerId);
        Task<IEnumerable<Booking>> SearchBookingsAsync(string searchTerm);
        Task<Booking> RescheduleBookingAsync(int bookingId, DateTime newStartDate, DateTime newEndDate);
        Task<IEnumerable<Booking>> GetConflictingBookingsAsync(int hallId, DateTime startDate, DateTime endDate);
        Task<bool> ApproveBookingAsync(int bookingId);
        Task<bool> RejectBookingAsync(int bookingId, string reason);
        Task<IEnumerable<Booking>> GetPendingBookingsAsync();
        Task<bool> UpdateBookingStatusAsync(int bookingId, string status);
        Task<bool> CheckHallAvailabilityAsync(int hallId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<Booking>> GetBookingHistoryAsync(string customerId);
        Task<bool> ValidateBookingPermissionsAsync(int bookingId, string userId);
        Task<BookingStatisticsDto> GetBookingStatisticsAsync();
        Task<HallAvailabilityDto> GetHallAvailabilityAsync(int hallId, DateTime date);
    }
}
