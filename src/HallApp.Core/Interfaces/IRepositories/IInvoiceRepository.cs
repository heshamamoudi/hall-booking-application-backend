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

    /// <summary>
    /// Gets the next value from the invoice_number_seq database sequence.
    /// Provides atomic, race-condition-free invoice number generation.
    /// </summary>
    Task<int> GetNextInvoiceSequenceValueAsync();

    Task<Invoice?> GetInvoiceWithDetailsAsync(int invoiceId);

    /// <summary>
    /// Removes all line items associated with an invoice.
    /// Used during invoice regeneration.
    /// </summary>
    Task RemoveLineItemsByInvoiceIdAsync(int invoiceId);

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

    // --- Enhanced query methods for invoice page redesign ---

    /// <summary>
    /// Gets a filtered, paginated IQueryable of invoices scoped to specific hall IDs.
    /// All filtering is done server-side via IQueryable composition.
    /// Pass null for hallIds to query all invoices (Admin scope).
    /// </summary>
    Task<(List<Invoice> Items, int TotalCount)> GetFilteredInvoicesAsync(
        IEnumerable<int>? hallIds,
        string? search,
        string? status,
        string? paymentMethod,
        int? hallId,
        int? organizationId,
        decimal? minAmount,
        decimal? maxAmount,
        DateTime? startDate,
        DateTime? endDate,
        bool? isCancelled,
        string sortBy,
        bool sortDescending,
        int pageNumber,
        int pageSize);

    /// <summary>
    /// Calculates aggregated invoice statistics using a single efficient database query.
    /// Pass null for hallIds to calculate across all invoices (Admin scope).
    /// </summary>
    Task<InvoiceStatisticsRaw> GetInvoiceStatisticsAsync(IEnumerable<int>? hallIds);

    /// <summary>
    /// Gets invoices for all halls belonging to a specific organization.
    /// Used for Admin organization filtering.
    /// </summary>
    Task<IEnumerable<int>> GetHallIdsByOrganizationIdAsync(int organizationId);

    /// <summary>
    /// Gets invoices by a list of IDs for bulk export.
    /// Uses AsNoTracking for read-only performance.
    /// </summary>
    Task<List<Invoice>> GetInvoicesByIdsAsync(IEnumerable<int> invoiceIds);
}

/// <summary>
/// Raw statistics result from the database aggregation query.
/// Used internally by the repository; the service layer maps this to the DTO.
/// </summary>
public class InvoiceStatisticsRaw
{
    public decimal TotalRevenue { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal TaxCollected { get; set; }
    public int TotalInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public int PendingInvoices { get; set; }
    public int CancelledInvoices { get; set; }
}
