using HallApp.Core.Entities.PaymentEntities;

namespace HallApp.Core.Interfaces.IRepositories;

/// <summary>
/// Repository interface for PaymentRefund entity operations
/// </summary>
public interface IPaymentRefundRepository : IGenericRepository<PaymentRefund>
{
    /// <summary>
    /// Get all refunds for a payment
    /// </summary>
    Task<IEnumerable<PaymentRefund>> GetRefundsByPaymentIdAsync(int paymentId);

    /// <summary>
    /// Get refunds by status
    /// </summary>
    Task<IEnumerable<PaymentRefund>> GetRefundsByStatusAsync(string status);

    /// <summary>
    /// Get total refunded amount for a payment
    /// </summary>
    Task<decimal> GetTotalRefundedAmountAsync(int paymentId);

    /// <summary>
    /// Get refunds requested by a specific user
    /// </summary>
    Task<IEnumerable<PaymentRefund>> GetRefundsByRequestedByAsync(int userId);
}
