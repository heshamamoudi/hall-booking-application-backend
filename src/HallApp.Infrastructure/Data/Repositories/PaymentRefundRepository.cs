using HallApp.Core.Entities.PaymentEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for PaymentRefund entity operations
/// </summary>
public class PaymentRefundRepository : GenericRepository<PaymentRefund>, IPaymentRefundRepository
{
    public PaymentRefundRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PaymentRefund>> GetRefundsByPaymentIdAsync(int paymentId)
    {
        return await _context.PaymentRefunds
            .Where(r => r.PaymentId == paymentId)
            .OrderByDescending(r => r.ProcessedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PaymentRefund>> GetRefundsByStatusAsync(string status)
    {
        return await _context.PaymentRefunds
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.ProcessedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalRefundedAmountAsync(int paymentId)
    {
        return await _context.PaymentRefunds
            .Where(r => r.PaymentId == paymentId && r.Status == "Completed")
            .SumAsync(r => r.RefundAmount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PaymentRefund>> GetRefundsByRequestedByAsync(int userId)
    {
        return await _context.PaymentRefunds
            .Where(r => r.RequestedBy == userId)
            .OrderByDescending(r => r.ProcessedAt)
            .ToListAsync();
    }
}
