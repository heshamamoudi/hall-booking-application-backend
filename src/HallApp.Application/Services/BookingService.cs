using AutoMapper;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Application.DTOs.Booking;

namespace HallApp.Application.Services;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BookingService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Booking> CreateBookingAsync(Booking booking)
    {
        await _unitOfWork.BookingRepository.AddAsync(booking);
        await _unitOfWork.Complete();
        return booking;
    }

    public async Task<Booking> GetBookingByIdAsync(int bookingId)
    {
        return await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(bookingId) ?? new Booking();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByCustomerIdAsync(string customerId)
    {
        if (int.TryParse(customerId, out int customerIdInt))
        {
            return await _unitOfWork.BookingRepository.GetBookingsByCustomerIdAsync(customerIdInt);
        }
        return new List<Booking>();
    }

    public async Task<IEnumerable<Booking>> GetAllBookingsAsync()
    {
        return await _unitOfWork.BookingRepository.GetAllAsync();
    }

    public async Task<Booking> UpdateBookingAsync(Booking booking)
    {
        _unitOfWork.BookingRepository.Update(booking);
        await _unitOfWork.Complete();
        return booking;
    }

    public async Task<Booking> UpdateCustomerBookingAsync(string customerId, Booking booking)
    {
        var existingBooking = await _unitOfWork.BookingRepository.GetByIdAsync(booking.Id);
        if (existingBooking?.CustomerId.ToString() != customerId) return new Booking();
        
        _unitOfWork.BookingRepository.Update(booking);
        await _unitOfWork.Complete();
        return booking;
    }

    public async Task<bool> CancelBookingAsync(int bookingId)
    {
        var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
        if (booking == null) return false;

        booking.Status = "Cancelled";
        _unitOfWork.BookingRepository.Update(booking);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<IEnumerable<Booking>> GetBookingsByHallIdAsync(int hallId)
    {
        return await _unitOfWork.BookingRepository.GetBookingsByHallIdAsync(hallId);
    }

    public async Task<IEnumerable<Booking>> GetBookingsByStatusAsync(string status)
    {
        var allBookings = await _unitOfWork.BookingRepository.GetAllAsync();
        return allBookings.Where(b => b.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _unitOfWork.BookingRepository.GetBookingsByDateRangeAsync(startDate, endDate);
    }

    public async Task<bool> ValidateBookingAsync(Booking booking)
    {
        return await IsHallAvailableAsync(booking.HallId, booking.VisitDate, booking.VisitDate.AddHours(4));
    }

    public async Task<decimal> CalculateBookingCostAsync(int hallId, DateTime startDate, DateTime endDate)
    {
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        if (hall == null) return 0;
        
        // Use flat daily rate instead of hourly calculation
        // Most halls charge per day/event, not per hour
        var isWeekend = startDate.DayOfWeek == DayOfWeek.Friday || startDate.DayOfWeek == DayOfWeek.Saturday;
        var baseRate = isWeekend ? (decimal)(hall.BothWeekEnds) : (decimal)(hall.BothWeekDays);
        
        Console.WriteLine($"Hall {hallId} pricing: Weekend={isWeekend}, Rate={baseRate}");
        return baseRate;
    }

    public async Task<bool> IsHallAvailableAsync(int hallId, DateTime startDate, DateTime endDate)
    {
        var conflictingBookings = await GetConflictingBookingsAsync(hallId, startDate, endDate);
        return !conflictingBookings.Any();
    }

    public async Task<IEnumerable<Booking>> GetUpcomingBookingsAsync(string customerId)
    {
        var bookings = await GetBookingsByCustomerIdAsync(customerId);
        return bookings.Where(b => b.VisitDate > DateTime.UtcNow);
    }

    public async Task<IEnumerable<Booking>> GetPastBookingsAsync(string customerId)
    {
        var bookings = await GetBookingsByCustomerIdAsync(customerId);
        return bookings.Where(b => b.VisitDate <= DateTime.UtcNow);
    }

    public async Task<IEnumerable<Booking>> SearchBookingsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<Booking>();
            
        var allBookings = await _unitOfWork.BookingRepository.GetAllAsync();
        return allBookings.Where(b => 
            b.Comments.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            b.EventType.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            b.Status.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    public async Task<Booking> RescheduleBookingAsync(int bookingId, DateTime newStartDate, DateTime newEndDate)
    {
        var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
        if (booking == null) return new Booking();

        booking.VisitDate = newStartDate;
        _unitOfWork.BookingRepository.Update(booking);
        await _unitOfWork.Complete();
        return booking;
    }

    public async Task<IEnumerable<Booking>> GetConflictingBookingsAsync(int hallId, DateTime startDate, DateTime endDate)
    {
        var hallBookings = await _unitOfWork.BookingRepository.GetBookingsByHallIdAsync(hallId);
        var startTime = startDate.TimeOfDay;
        var endTime = endDate.TimeOfDay;
        
        return hallBookings.Where(b => 
            b.Status != "Cancelled" &&
            b.VisitDate.Date == startDate.Date && // Same day bookings
            (
                (b.StartTime <= startTime && b.EndTime > startTime) ||
                (b.StartTime < endTime && b.EndTime >= endTime) ||
                (b.StartTime >= startTime && b.EndTime <= endTime)
            )
        ).ToList();
    }

    public async Task<bool> ApproveBookingAsync(int bookingId)
    {
        return await UpdateBookingStatusAsync(bookingId, "Approved");
    }

    public async Task<bool> RejectBookingAsync(int bookingId, string reason)
    {
        return await UpdateBookingStatusAsync(bookingId, "Rejected");
    }

    public async Task<IEnumerable<Booking>> GetPendingBookingsAsync()
    {
        return await GetBookingsByStatusAsync("Pending");
    }

    public async Task<bool> UpdateBookingStatusAsync(int bookingId, string status)
    {
        var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
        if (booking == null) return false;

        booking.Status = status;
        _unitOfWork.BookingRepository.Update(booking);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<bool> CheckHallAvailabilityAsync(int hallId, DateTime startDate, DateTime endDate)
    {
        return await IsHallAvailableAsync(hallId, startDate, endDate);
    }

    public async Task<IEnumerable<Booking>> GetBookingHistoryAsync(string customerId)
    {
        return await GetBookingsByCustomerIdAsync(customerId);
    }

    public async Task<bool> ValidateBookingPermissionsAsync(int bookingId, string userId)
    {
        var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
        if (booking == null) return false;
        
        // Check if user owns the booking
        return booking.CustomerId.ToString() == userId;
    }

    public async Task<BookingStatisticsDto> GetBookingStatisticsAsync()
    {
        var allBookings = await _unitOfWork.BookingRepository.GetAllAsync();
        var bookingsList = allBookings.ToList();
        var thisMonth = DateTime.UtcNow.Month;
        var thisYear = DateTime.UtcNow.Year;
        
        return new BookingStatisticsDto 
        { 
            TotalBookings = bookingsList.Count,
            PendingBookings = bookingsList.Count(b => b.Status == "Pending"),
            ConfirmedBookings = bookingsList.Count(b => b.Status == "Confirmed"),
            CancelledBookings = bookingsList.Count(b => b.Status == "Cancelled"),
            TotalRevenue = (decimal)bookingsList.Sum(b => b.TotalAmount),
            ThisMonthRevenue = (decimal)bookingsList
                .Where(b => b.Created.Month == thisMonth && b.Created.Year == thisYear)
                .Sum(b => b.TotalAmount)
        };
    }

    public async Task<HallAvailabilityDto> GetHallAvailabilityAsync(int hallId, DateTime date)
    {
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        if (hall == null) return new HallAvailabilityDto();
        
        var dayBookings = await _unitOfWork.BookingRepository.GetBookingsByHallIdAsync(hallId);
        var dateBookings = dayBookings.Where(b => 
            b.VisitDate.Date == date.Date && 
            b.Status != "Cancelled").ToList();
        
        return new HallAvailabilityDto
        {
            HallId = hallId,
            HallName = hall.Name,
            Date = date,
            IsAvailable = !dateBookings.Any()
        };
    }
}
