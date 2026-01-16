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
    Task<IEnumerable<Invoice>> GetInvoicesByStatusAsync(string paymentStatus);
    Task<IEnumerable<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<int> GetInvoiceCountForYearAsync(int year);
    Task<Invoice?> GetInvoiceWithDetailsAsync(int invoiceId);
}
