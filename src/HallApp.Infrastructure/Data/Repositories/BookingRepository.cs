using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class BookingRepository : GenericRepository<Booking>, IBookingRepository
{
    public BookingRepository(DataContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Booking>> GetBookingsByCustomerIdAsync(int customerId)
    {
        return await _context.Bookings
            .Where(b => b.CustomerId == customerId)
            .Include(b => b.Hall) // Include hall information
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .OrderByDescending(b => b.Created)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByHallIdAsync(int hallId)
    {
        return await _context.Bookings
            .Where(b => b.HallId == hallId)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .OrderByDescending(b => b.Created)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Bookings
            .Where(b => b.BookingDate >= startDate && b.BookingDate <= endDate)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .OrderByDescending(b => b.Created)
            .ToListAsync();
    }

    public async Task<Booking> GetBookingWithDetailsAsync(int bookingId)
    {
        return await _context.Bookings
            .Include(b => b.Hall) // Include hall information
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Services)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
    }

    public async Task<IEnumerable<Booking>> GetPendingBookingsAsync()
    {
        return await _context.Bookings
            .Where(b => b.Status == "Pending" || b.Status == "Pending Visitation")
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .OrderBy(b => b.VisitDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetConfirmedBookingsAsync()
    {
        return await _context.Bookings
            .Where(b => b.Status == "Confirmed" || b.IsBookingConfirmed)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .OrderBy(b => b.BookingDate)
            .ToListAsync();
    }
}
