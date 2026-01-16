using HallApp.Core.Entities.BookingEntities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// Interface for Invoice Service - handles invoice generation, management and ZATCA compliance
/// </summary>
public interface IInvoiceService
{
    // Core CRUD
    Task<Invoice> GetInvoiceByIdAsync(int invoiceId);
    Task<Invoice?> GetInvoiceByBookingIdAsync(int bookingId);
    Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber);
    Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
    Task<IEnumerable<Invoice>> GetInvoicesByCustomerIdAsync(int customerId);
    Task<IEnumerable<Invoice>> GetInvoicesByHallIdAsync(int hallId);
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

    // Updates
    Task<Invoice> UpdateInvoiceAsync(Invoice invoice);
    Task<bool> UpdatePaymentStatusAsync(int invoiceId, string paymentStatus, string paymentReference = "");
    Task<bool> CancelInvoiceAsync(int invoiceId, string reason);

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

    // Statistics
    Task<InvoiceStatistics> GetInvoiceStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
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
