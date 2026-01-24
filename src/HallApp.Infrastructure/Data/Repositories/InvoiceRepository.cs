using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Invoice operations
/// </summary>
public class InvoiceRepository : GenericRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(DataContext context) : base(context)
    {
    }

    public async Task<Invoice?> GetInvoiceByBookingIdAsync(int bookingId)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
                .ThenInclude(b => b.Customer)
                    .ThenInclude(c => c.AppUser)
            .Include(i => i.Booking)
                .ThenInclude(b => b.Hall)
            .Include(i => i.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.BookingId == bookingId);
    }

    public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
            .Include(i => i.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByCustomerIdAsync(int customerId)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
            .Include(i => i.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByHallIdAsync(int hallId)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
            .Include(i => i.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .Where(i => i.HallId == hallId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByVendorIdAsync(int vendorId)
    {
        // Get invoices that contain line items referencing this vendor
        // or invoices where the booking includes vendor services from this vendor
        return await _context.Invoices
            .Include(i => i.Booking)
                .ThenInclude(b => b.VendorBookings)
            .Include(i => i.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .Where(i => i.Booking != null &&
                        i.Booking.VendorBookings != null &&
                        i.Booking.VendorBookings.Any(vb => vb.VendorId == vendorId))
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByStatusAsync(string paymentStatus)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
            .Include(i => i.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .Where(i => i.PaymentStatus == paymentStatus)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
            .Include(i => i.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<int> GetInvoiceCountForYearAsync(int year)
    {
        return await _context.Invoices
            .Where(i => i.InvoiceDate.Year == year)
            .CountAsync();
    }

    public async Task<Invoice?> GetInvoiceWithDetailsAsync(int invoiceId)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
                .ThenInclude(b => b.Customer)
                    .ThenInclude(c => c.AppUser)
            .Include(i => i.Booking)
                .ThenInclude(b => b.Hall)
            .Include(i => i.Booking)
                .ThenInclude(b => b.VendorBookings)
                    .ThenInclude(vb => vb.Vendor)
            .Include(i => i.Booking)
                .ThenInclude(b => b.VendorBookings)
                    .ThenInclude(vb => vb.Services)
                        .ThenInclude(s => s.ServiceItem)
            .Include(i => i.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    public new async Task<IEnumerable<Invoice>> GetAllAsync()
    {
        return await _context.Invoices
            .Include(i => i.Customer)
                .ThenInclude(c => c.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public new async Task<Invoice?> GetByIdAsync(int id)
    {
        return await GetInvoiceWithDetailsAsync(id);
    }
}
