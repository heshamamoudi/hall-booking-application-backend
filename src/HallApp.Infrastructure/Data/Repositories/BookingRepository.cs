using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class BookingRepository : GenericRepository<Booking>, IBookingRepository
{
    public BookingRepository(DataContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<Booking>> GetAllAsync()
    {
        return await _context.Bookings
            .Include(b => b.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(b => b.Hall)
                .ThenInclude(h => h.Managers)
            .Include(b => b.PackageDetails)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Services)
                    .ThenInclude(s => s.ServiceItem)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByCustomerIdAsync(int customerId)
    {
        return await _context.Bookings
            .Where(b => b.CustomerId == customerId)
            .Include(b => b.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(b => b.Hall)
                .ThenInclude(h => h.Managers)
            .Include(b => b.PackageDetails)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Services)
                    .ThenInclude(s => s.ServiceItem)
            .AsSplitQuery()
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByHallIdAsync(int hallId)
    {
        return await _context.Bookings
            .Where(b => b.HallId == hallId)
            .Include(b => b.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(b => b.Hall)
                .ThenInclude(h => h.Managers)
            .Include(b => b.PackageDetails)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Services)
                    .ThenInclude(s => s.ServiceItem)
            .AsSplitQuery()
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByVendorIdAsync(int vendorId)
    {
        return await _context.Bookings
            .Where(b => b.VendorBookings.Any(vb => vb.VendorId == vendorId))
            .Include(b => b.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(b => b.Hall)
                .ThenInclude(h => h.Managers)
            .Include(b => b.PackageDetails)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Services)
                    .ThenInclude(s => s.ServiceItem)
            .AsSplitQuery()
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Bookings
            .Where(b => b.BookingDate >= startDate && b.BookingDate <= endDate)
            .Include(b => b.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(b => b.Hall)
                .ThenInclude(h => h.Managers)
            .Include(b => b.PackageDetails)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Services)
                    .ThenInclude(s => s.ServiceItem)
            .AsSplitQuery()
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<Booking> GetBookingWithDetailsAsync(int bookingId)
    {
        return await _context.Bookings
            .Include(b => b.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(b => b.Hall)
                .ThenInclude(h => h.Managers)
            .Include(b => b.PackageDetails)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Services)
                    .ThenInclude(s => s.ServiceItem)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
    }

    public async Task<IEnumerable<Booking>> GetPendingBookingsAsync()
    {
        return await _context.Bookings
            .Where(b => b.Status == "Pending" || b.Status == "Pending Visitation")
            .Include(b => b.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(b => b.Hall)
                .ThenInclude(h => h.Managers)
            .Include(b => b.PackageDetails)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Services)
                    .ThenInclude(s => s.ServiceItem)
            .AsSplitQuery()
            .OrderBy(b => b.VisitDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetConfirmedBookingsAsync()
    {
        return await _context.Bookings
            .Where(b => b.Status == "Confirmed" || b.IsBookingConfirmed)
            .Include(b => b.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(b => b.Hall)
                .ThenInclude(h => h.Managers)
            .Include(b => b.PackageDetails)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Services)
                    .ThenInclude(s => s.ServiceItem)
            .AsSplitQuery()
            .OrderBy(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByHallIdsAsync(IEnumerable<int> hallIds)
    {
        var hallIdList = hallIds.ToList();
        return await _context.Bookings
            .Where(b => hallIdList.Contains(b.HallId))
            .Include(b => b.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(b => b.Hall)
                .ThenInclude(h => h.Managers)
            .Include(b => b.PackageDetails)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Vendor)
            .Include(b => b.VendorBookings)
                .ThenInclude(vb => vb.Services)
                    .ThenInclude(s => s.ServiceItem)
            .AsSplitQuery()
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    // --- Hall Statistics queries (optimized, read-only) ---

    /// <inheritdoc />
    public async Task<int> GetBookingCountByHallIdAsync(int hallId, IEnumerable<string>? statusFilter = null)
    {
        var query = _context.Bookings
            .AsNoTracking()
            .Where(b => b.HallId == hallId);

        if (statusFilter != null)
        {
            var statuses = statusFilter.ToList();
            query = query.Where(b => statuses.Contains(b.Status));
        }

        return await query.CountAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Booking>> GetRecentBookingsByHallIdAsync(int hallId, int limit = 5)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.HallId == hallId)
            .Include(b => b.Customer)
                .ThenInclude(c => c.AppUser)
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit)
            .AsSplitQuery()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Booking>> GetBookingsByHallIdAndDateRangeAsync(
        int hallId, DateTime startDate, DateTime endDate)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.HallId == hallId && b.CreatedAt >= startDate && b.CreatedAt <= endDate)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync();
    }
}
