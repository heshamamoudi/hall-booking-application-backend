using HallApp.Core.Entities.BookingEntities;

namespace HallApp.Core.Interfaces.IRepositories;

/// <summary>
/// Repository interface for Invoice operations
/// </summary>
public interface IInvoiceRepository : IGenericRepository<Invoice>
{
    Task<Invoice?> GetInvoiceByBookingIdAsync(int bookingId);
    Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber);
    Task<IEnumerable<Invoice>> GetInvoicesByCustomerIdAsync(int customerId);
    Task<IEnumerable<Invoice>> GetInvoicesByHallIdAsync(int hallId);
    Task<IEnumerable<Invoice>> GetInvoicesByHallIdsAsync(IEnumerable<int> hallIds);
    Task<IEnumerable<Invoice>> GetInvoicesByVendorIdAsync(int vendorId);
    Task<IEnumerable<Invoice>> GetInvoicesByStatusAsync(string paymentStatus);
    Task<IEnumerable<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<int> GetInvoiceCountForYearAsync(int year);
    Task<Invoice?> GetInvoiceWithDetailsAsync(int invoiceId);

    // --- Hall Statistics queries (optimized, read-only) ---

    /// <summary>
    /// Gets the count of invoices for a specific hall, optionally filtered by payment status.
    /// Uses AsNoTracking and server-side COUNT for performance.
    /// </summary>
    Task<int> GetInvoiceCountByHallIdAsync(int hallId, string? paymentStatus = null);

    /// <summary>
    /// Gets the total revenue (sum of TotalAmountWithTax) for paid invoices for a specific hall.
    /// Uses AsNoTracking and server-side SUM for performance.
    /// </summary>
    Task<decimal> GetTotalRevenueByHallIdAsync(int hallId);

    /// <summary>
    /// Gets invoices for a specific hall within a date range for monthly revenue aggregation.
    /// Uses AsNoTracking for read-only performance.
    /// </summary>
    Task<IEnumerable<Invoice>> GetInvoicesByHallIdAndDateRangeAsync(int hallId, DateTime startDate, DateTime endDate);
}
