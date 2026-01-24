using HallApp.Core.Entities.PaymentEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Payment entity operations
/// </summary>
public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Payment?> GetPaymentWithBookingByCheckoutIdAsync(string checkoutId)
    {
        return await _context.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.CheckoutId == checkoutId);
    }

    /// <inheritdoc />
    public async Task<Payment?> GetPaymentWithBookingAsync(int paymentId)
    {
        return await _context.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.Id == paymentId);
    }

    /// <inheritdoc />
    public async Task<Payment?> GetSuccessfulPaymentByBookingIdAsync(int bookingId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.BookingId == bookingId && p.Status == "Success");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Payment>> GetPaymentsByBookingIdAsync(int bookingId)
    {
        return await _context.Payments
            .Where(p => p.BookingId == bookingId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Payment>> GetPaymentsByCustomerIdAsync(int customerId)
    {
        return await _context.Payments
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(string status)
    {
        return await _context.Payments
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Payment>> GetPaymentsByGatewayAsync(string gateway)
    {
        return await _context.Payments
            .Where(p => p.PaymentGateway == gateway)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Payment>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Payments
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
