using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Interfaces.IRepositories;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// Interface for Invoice Service - handles invoice generation, management and ZATCA compliance
/// </summary>
public interface IInvoiceService
{
    // Core CRUD
    Task<Invoice?> GetInvoiceByIdAsync(int invoiceId);
    Task<Invoice?> GetInvoiceByBookingIdAsync(int bookingId);
    Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber);
    Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
    Task<IEnumerable<Invoice>> GetInvoicesByCustomerIdAsync(int customerId);
    Task<IEnumerable<Invoice>> GetInvoicesByHallIdAsync(int hallId);
    Task<IEnumerable<Invoice>> GetInvoicesByHallIdsAsync(IEnumerable<int> hallIds);
    Task<IEnumerable<Invoice>> GetInvoicesByVendorIdAsync(int vendorId);
    Task<IEnumerable<Invoice>> GetInvoicesByStatusAsync(string paymentStatus);
    Task<IEnumerable<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);

    // Invoice Generation
    /// <summary>
    /// Generates an invoice for a confirmed booking
    /// </summary>
    Task<Invoice> GenerateInvoiceForBookingAsync(int bookingId, string createdBy);

    /// <summary>
    /// Generate next invoice number in format: INV-YYYY-XXXXXX
    /// </summary>
    Task<string> GenerateInvoiceNumberAsync();

    // Bulk Generation
    /// <summary>
    /// Generates invoices for multiple bookings in a single operation.
    /// Returns a tuple of successfully generated invoices and failures.
    /// </summary>
    Task<(List<Invoice> Generated, List<(int BookingId, string Error)> Failed)> GenerateBulkInvoicesAsync(
        IEnumerable<int> bookingIds, string createdBy);

    // Regeneration
    /// <summary>
    /// Regenerates an existing invoice from current booking data.
    /// Preserves invoice number and creation date. Cannot regenerate paid invoices.
    /// </summary>
    Task<Invoice> RegenerateInvoiceAsync(int invoiceId, string regeneratedBy);

    // Updates
    Task<Invoice> UpdateInvoiceAsync(Invoice invoice);
    Task<bool> UpdatePaymentStatusAsync(int invoiceId, string paymentStatus, string paymentReference = "");

    // Cancellation with enhanced validation
    /// <summary>
    /// Cancels an invoice. Cannot cancel already-paid invoices without a prior refund.
    /// </summary>
    Task<Invoice> CancelInvoiceWithValidationAsync(int invoiceId, string reason, string cancelledBy);

    // Legacy cancel method kept for backward compatibility
    Task<bool> CancelInvoiceAsync(int invoiceId, string reason);

    // Refund
    /// <summary>
    /// Processes a refund for a paid invoice. Creates a refund record and updates invoice status.
    /// </summary>
    Task<Invoice> RefundInvoiceAsync(int invoiceId, decimal refundAmount, string reason, string refundMethod, int refundedByUserId);

    // ZATCA Compliance
    /// <summary>
    /// Generate QR code content for simplified invoices (B2C) per ZATCA specification
    /// </summary>
    Task<string> GenerateZATCAQRCodeAsync(Invoice invoice);

    /// <summary>
    /// Generate invoice hash for security verification
    /// </summary>
    Task<string> GenerateInvoiceHashAsync(Invoice invoice);

    // PDF Generation
    Task<string> GenerateInvoicePdfAsync(int invoiceId);
    Task<byte[]> GetInvoicePdfBytesAsync(int invoiceId);

    // Statistics (legacy)
    Task<InvoiceStatistics> GetInvoiceStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    // --- Enhanced methods for invoice page redesign ---

    /// <summary>
    /// Gets role-scoped invoice statistics. Admin: all invoices. Org Manager: org halls. Hall Manager: assigned halls.
    /// </summary>
    Task<InvoiceStatisticsRaw> GetInvoiceStatisticsForRoleAsync(IEnumerable<int>? hallIds);

    /// <summary>
    /// Gets filtered, paginated invoices for Admin (no hall restriction).
    /// </summary>
    Task<(List<Invoice> Items, int TotalCount)> GetInvoicesForAdminAsync(
        string? search, string? status, string? paymentMethod, int? hallId,
        int? organizationId, decimal? minAmount, decimal? maxAmount,
        DateTime? startDate, DateTime? endDate, bool? isCancelled,
        string sortBy, bool sortDescending, int pageNumber, int pageSize);

    /// <summary>
    /// Gets filtered, paginated invoices for HallOrganizationManager (scoped to org halls).
    /// </summary>
    Task<(List<Invoice> Items, int TotalCount)> GetInvoicesForOrgManagerAsync(
        IEnumerable<int> orgHallIds,
        string? search, string? status, string? paymentMethod, int? hallId,
        decimal? minAmount, decimal? maxAmount,
        DateTime? startDate, DateTime? endDate, bool? isCancelled,
        string sortBy, bool sortDescending, int pageNumber, int pageSize);

    /// <summary>
    /// Gets filtered, paginated invoices for HallManager (scoped to assigned halls).
    /// </summary>
    Task<(List<Invoice> Items, int TotalCount)> GetInvoicesForHallManagerAsync(
        IEnumerable<int> assignedHallIds,
        string? search, string? status, string? paymentMethod, int? hallId,
        decimal? minAmount, decimal? maxAmount,
        DateTime? startDate, DateTime? endDate, bool? isCancelled,
        string sortBy, bool sortDescending, int pageNumber, int pageSize);

    /// <summary>
    /// Gets hall IDs belonging to a specific organization. Used for Admin org-level filtering.
    /// </summary>
    Task<IEnumerable<int>> GetHallIdsByOrganizationIdAsync(int organizationId);

    /// <summary>
    /// Gets invoices by a list of IDs. Used for bulk export authorization validation.
    /// </summary>
    Task<List<Invoice>> GetInvoicesByIdsAsync(IEnumerable<int> invoiceIds);

    /// <summary>
    /// Exports invoices as a CSV byte array.
    /// </summary>
    Task<byte[]> ExportInvoicesToCsvAsync(IEnumerable<int> invoiceIds);

    /// <summary>
    /// Exports invoices as an Excel-compatible CSV byte array.
    /// </summary>
    Task<byte[]> ExportInvoicesToExcelAsync(IEnumerable<int> invoiceIds);
}

/// <summary>
/// Invoice statistics summary
/// </summary>
public class InvoiceStatistics
{
    public int TotalInvoices { get; set; }
    public int PendingInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public int CancelledInvoices { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalTaxCollected { get; set; }
    public decimal AverageInvoiceAmount { get; set; }
}
