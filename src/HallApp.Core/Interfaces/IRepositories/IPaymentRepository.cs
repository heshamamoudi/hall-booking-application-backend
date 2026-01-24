using HallApp.Core.Entities.PaymentEntities;

namespace HallApp.Core.Interfaces.IRepositories;

/// <summary>
/// Repository interface for Payment entity operations
/// </summary>
public interface IPaymentRepository : IGenericRepository<Payment>
{
    /// <summary>
    /// Get payment by checkout ID with booking details
    /// </summary>
    Task<Payment?> GetPaymentWithBookingByCheckoutIdAsync(string checkoutId);

    /// <summary>
    /// Get payment by ID with booking details
    /// </summary>
    Task<Payment?> GetPaymentWithBookingAsync(int paymentId);

    /// <summary>
    /// Get successful payment for a booking
    /// </summary>
    Task<Payment?> GetSuccessfulPaymentByBookingIdAsync(int bookingId);

    /// <summary>
    /// Get all payments for a booking
    /// </summary>
    Task<IEnumerable<Payment>> GetPaymentsByBookingIdAsync(int bookingId);

    /// <summary>
    /// Get all payments for a customer
    /// </summary>
    Task<IEnumerable<Payment>> GetPaymentsByCustomerIdAsync(int customerId);

    /// <summary>
    /// Get payments by status
    /// </summary>
    Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(string status);

    /// <summary>
    /// Get payments by payment gateway
    /// </summary>
    Task<IEnumerable<Payment>> GetPaymentsByGatewayAsync(string gateway);

    /// <summary>
    /// Get payments within a date range
    /// </summary>
    Task<IEnumerable<Payment>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate);
}
