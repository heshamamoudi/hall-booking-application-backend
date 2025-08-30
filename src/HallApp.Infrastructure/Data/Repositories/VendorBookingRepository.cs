using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class VendorBookingRepository : GenericRepository<VendorBooking>, IVendorBookingRepository
{
    public VendorBookingRepository(DataContext context) : base(context)
    {
    }

    public async Task<IEnumerable<VendorBooking>> GetBookingsByVendorIdAsync(int vendorId)
    {
        return await _context.VendorBookings
            .Where(vb => vb.VendorId == vendorId)
            .Include(vb => vb.Booking)
            .OrderByDescending(vb => vb.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<VendorBooking>> GetBookingsByDateRangeAsync(int vendorId, DateTime startDate, DateTime endDate)
    {
        return await _context.VendorBookings
            .Where(vb => vb.VendorId == vendorId && 
                        vb.StartTime.Date >= startDate.Date && 
                        vb.StartTime.Date <= endDate.Date)
            .Include(vb => vb.Booking)
            .OrderBy(vb => vb.StartTime)
            .ToListAsync();
    }

    public async Task<VendorBooking> GetBookingWithDetailsAsync(int bookingId)
    {
        return await _context.VendorBookings
            .Include(vb => vb.Booking)
            .Include(vb => vb.Vendor)
            .FirstOrDefaultAsync(vb => vb.Id == bookingId);
    }

    public async Task<bool> IsVendorAvailableAsync(int vendorId, DateTime startTime, DateTime endTime)
    {
        // Check if vendor has any overlapping bookings
        var overlappingBooking = await _context.VendorBookings
            .Where(vb => vb.VendorId == vendorId &&
                        vb.Status != "Cancelled" &&
                        ((startTime >= vb.StartTime && startTime < vb.EndTime) ||
                         (endTime > vb.StartTime && endTime <= vb.EndTime) ||
                         (startTime <= vb.StartTime && endTime >= vb.EndTime)))
            .FirstOrDefaultAsync();

        return overlappingBooking == null;
    }

    public async Task<IEnumerable<VendorBooking>> GetPendingBookingsAsync(int vendorId)
    {
        return await _context.VendorBookings
            .Where(vb => vb.VendorId == vendorId && 
                        (vb.Status == "Pending" || vb.Status == "Confirmed"))
            .Include(vb => vb.Booking)
            .OrderBy(vb => vb.StartTime)
            .ToListAsync();
    }
}
