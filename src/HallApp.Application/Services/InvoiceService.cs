using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HallApp.Core.Entities;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

/// <summary>
/// Invoice Service implementation - handles invoice generation, management and ZATCA compliance.
/// Seller information is resolved from PlatformSettings (Zawaji company info) as the deemed supplier,
/// ensuring ZATCA marketplace compliance where the platform is the seller on all customer-facing invoices.
/// Purchase orders are auto-generated for hall/vendor payouts with commission deductions.
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationService _organizationService;
    private readonly IPlatformSettingsService _platformSettingsService;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ILogger<InvoiceService> _logger;

    // CRIT-FIN-002: Standardized to decimal fraction format (0.15 = 15% VAT)
    // Matches Booking.TaxRate, BusinessRulesSettings.TaxRate, and BookingFinancialService format
    private const decimal TAX_RATE = 0.15m; // 15% VAT in Saudi Arabia

    public InvoiceService(
        IUnitOfWork unitOfWork,
        IOrganizationService organizationService,
        IPlatformSettingsService platformSettingsService,
        IPurchaseOrderService purchaseOrderService,
        ILogger<InvoiceService> logger)
    {
        _unitOfWork = unitOfWork;
        _organizationService = organizationService;
        _platformSettingsService = platformSettingsService;
        _purchaseOrderService = purchaseOrderService;
        _logger = logger;
    }

    #region Core CRUD

    public async Task<Invoice?> GetInvoiceByIdAsync(int invoiceId)
    {
        return await _unitOfWork.InvoiceRepository.GetByIdAsync(invoiceId);
    }

    public async Task<Invoice?> GetInvoiceByBookingIdAsync(int bookingId)
    {
        return await _unitOfWork.InvoiceRepository.GetInvoiceByBookingIdAsync(bookingId);
    }

    public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber)
    {
        return await _unitOfWork.InvoiceRepository.GetInvoiceByNumberAsync(invoiceNumber);
    }

    public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
    {
        return await _unitOfWork.InvoiceRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByCustomerIdAsync(int customerId)
    {
        return await _unitOfWork.InvoiceRepository.GetInvoicesByCustomerIdAsync(customerId);
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByHallIdAsync(int hallId)
    {
        return await _unitOfWork.InvoiceRepository.GetInvoicesByHallIdAsync(hallId);
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByHallIdsAsync(IEnumerable<int> hallIds)
    {
        return await _unitOfWork.InvoiceRepository.GetInvoicesByHallIdsAsync(hallIds);
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByVendorIdAsync(int vendorId)
    {
        return await _unitOfWork.InvoiceRepository.GetInvoicesByVendorIdAsync(vendorId);
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByStatusAsync(string paymentStatus)
    {
        return await _unitOfWork.InvoiceRepository.GetInvoicesByStatusAsync(paymentStatus);
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _unitOfWork.InvoiceRepository.GetInvoicesByDateRangeAsync(startDate, endDate);
    }

    #endregion

    #region Invoice Generation

    public async Task<Invoice> GenerateInvoiceForBookingAsync(int bookingId, string createdBy)
    {
        _logger.LogInformation("Generating invoice for booking {BookingId}", bookingId);

        // Check if invoice already exists for this booking
        var existingInvoice = await _unitOfWork.InvoiceRepository.GetInvoiceByBookingIdAsync(bookingId);
        if (existingInvoice != null)
        {
            _logger.LogWarning("Invoice already exists for booking {BookingId}: {InvoiceNumber}", bookingId, existingInvoice.InvoiceNumber);
            return existingInvoice;
        }

        // Get booking with all details
        var booking = await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(bookingId);
        if (booking == null)
        {
            throw new ArgumentException($"Booking {bookingId} not found");
        }

        // Get customer details
        var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(booking.CustomerId);
        if (customer == null)
        {
            throw new ArgumentException($"Customer {booking.CustomerId} not found");
        }

        // Get hall details
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(booking.HallId);

        // Resolve PlatformSettings (Zawaji) as the seller — deemed supplier model
        var platformSettings = await _platformSettingsService.GetSettingsAsync();
        ValidatePlatformSettingsForZatca(platformSettings);

        // Build the composite seller address from PlatformSettings
        var sellerAddress = BuildPlatformSellerAddress(platformSettings);

        // Generate invoice number
        var invoiceNumber = await GenerateInvoiceNumberAsync();

        // Create invoice with Zawaji as seller
        var invoice = BuildInvoiceFromBooking(booking, customer, hall, platformSettings, sellerAddress, invoiceNumber, createdBy);

        // Create line items (Enhancement 1: includes BookingPackage)
        invoice.LineItems = await BuildLineItemsForBookingAsync(booking, hall);

        // CRIT-002 FIX: Calculate header totals FROM line items to prevent mismatches
        RecalculateHeaderTotalsFromLineItems(invoice);

        // Generate ZATCA QR Code (now uses Zawaji's VAT number)
        invoice.QRCode = await GenerateZATCAQRCodeAsync(invoice);

        // Generate Invoice Hash
        invoice.InvoiceHash = await GenerateInvoiceHashAsync(invoice);

        // Generate UUID for ZATCA
        invoice.ZATCA_UUID = Guid.NewGuid().ToString();

        // Save invoice
        await _unitOfWork.InvoiceRepository.AddAsync(invoice);
        await _unitOfWork.Complete();

        _logger.LogInformation("Invoice {InvoiceNumber} generated successfully for booking {BookingId}", invoice.InvoiceNumber, bookingId);

        // Auto-generate purchase orders for hall/vendor payouts (commission deducted)
        try
        {
            var purchaseOrders = await _purchaseOrderService.GeneratePurchaseOrdersForInvoiceAsync(invoice.Id, createdBy);
            _logger.LogInformation(
                "Auto-generated {PoCount} purchase order(s) for invoice {InvoiceNumber}",
                purchaseOrders.Count, invoice.InvoiceNumber);
        }
        catch (Exception ex)
        {
            // PO generation failure should not block invoice creation
            _logger.LogError(ex,
                "Failed to auto-generate purchase orders for invoice {InvoiceNumber} (booking {BookingId}). " +
                "POs can be generated manually via the admin panel.",
                invoice.InvoiceNumber, bookingId);
        }

        return invoice;
    }

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.UtcNow.Year;

        // CRIT-001 FIX: Use database sequence for atomic, race-condition-free invoice number generation.
        // The sequence guarantees unique values even under concurrent requests.
        var sequenceNumber = await _unitOfWork.InvoiceRepository.GetNextInvoiceSequenceValueAsync();

        // Format: INV-YYYY-XXXXXX (e.g., INV-2026-000001)
        return $"INV-{year}-{sequenceNumber:D6}";
    }

    #endregion

    #region Enhancement 2: Bulk Invoice Generation

    public async Task<(List<Invoice> Generated, List<(int BookingId, string Error)> Failed)> GenerateBulkInvoicesAsync(
        IEnumerable<int> bookingIds, string createdBy)
    {
        var generated = new List<Invoice>();
        var failed = new List<(int BookingId, string Error)>();

        _logger.LogInformation(
            "Starting bulk invoice generation for {Count} bookings by {CreatedBy}",
            bookingIds.Count(), createdBy);

        foreach (var bookingId in bookingIds)
        {
            try
            {
                // Check if invoice already exists
                var existingInvoice = await _unitOfWork.InvoiceRepository.GetInvoiceByBookingIdAsync(bookingId);
                if (existingInvoice != null)
                {
                    failed.Add((bookingId, $"Invoice already exists: {existingInvoice.InvoiceNumber}"));
                    _logger.LogWarning(
                        "Bulk generation skipped booking {BookingId} - invoice already exists: {InvoiceNumber}",
                        bookingId, existingInvoice.InvoiceNumber);
                    continue;
                }

                var invoice = await GenerateInvoiceForBookingAsync(bookingId, createdBy);
                generated.Add(invoice);

                _logger.LogInformation(
                    "Bulk generation: Invoice {InvoiceNumber} generated for booking {BookingId}",
                    invoice.InvoiceNumber, bookingId);
            }
            catch (Exception ex)
            {
                failed.Add((bookingId, ex.Message));
                _logger.LogError(ex,
                    "Bulk generation: Failed to generate invoice for booking {BookingId}",
                    bookingId);
            }
        }

        _logger.LogInformation(
            "Bulk invoice generation completed: {GeneratedCount} generated, {FailedCount} failed",
            generated.Count, failed.Count);

        return (generated, failed);
    }

    #endregion

    #region Enhancement 4: Invoice Regeneration

    public async Task<Invoice> RegenerateInvoiceAsync(int invoiceId, string regeneratedBy)
    {
        _logger.LogInformation(
            "Regenerating invoice {InvoiceId} requested by {RegeneratedBy}",
            invoiceId, regeneratedBy);

        var invoice = await _unitOfWork.InvoiceRepository.GetInvoiceWithDetailsAsync(invoiceId);
        if (invoice == null)
        {
            throw new ArgumentException($"Invoice {invoiceId} not found");
        }

        // Cannot regenerate paid invoices - protect financial integrity
        if (invoice.PaymentStatus == "Paid")
        {
            throw new InvalidOperationException(
                $"Cannot regenerate paid invoice {invoice.InvoiceNumber}. " +
                "Process a refund first if the invoice needs to be corrected.");
        }

        // Cannot regenerate cancelled invoices
        if (invoice.IsCancelled)
        {
            throw new InvalidOperationException(
                $"Cannot regenerate cancelled invoice {invoice.InvoiceNumber}.");
        }

        // Cannot regenerate refunded invoices
        if (invoice.PaymentStatus == "Refunded")
        {
            throw new InvalidOperationException(
                $"Cannot regenerate refunded invoice {invoice.InvoiceNumber}.");
        }

        // Store original data for audit
        var originalSubtotal = invoice.SubtotalBeforeTax;
        var originalTotal = invoice.TotalAmountWithTax;
        var originalTax = invoice.TaxAmount;

        _logger.LogInformation(
            "Invoice {InvoiceNumber} original values - Subtotal: {Subtotal}, Tax: {Tax}, Total: {Total}",
            invoice.InvoiceNumber, originalSubtotal, originalTax, originalTotal);

        // Get current booking data
        var booking = await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(invoice.BookingId);
        if (booking == null)
        {
            throw new ArgumentException($"Booking {invoice.BookingId} not found for invoice regeneration");
        }

        var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(booking.CustomerId);
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(booking.HallId);

        // Resolve PlatformSettings (Zawaji) as seller — deemed supplier model
        var platformSettings = await _platformSettingsService.GetSettingsAsync();

        // Delete existing line items
        await _unitOfWork.InvoiceRepository.RemoveLineItemsByInvoiceIdAsync(invoiceId);

        // Update non-financial invoice header fields from current booking data
        invoice.SupplyDate = booking.EventDate;
        invoice.TaxRate = TAX_RATE;
        invoice.Currency = booking.Currency ?? "SAR";
        invoice.PaymentMethod = booking.PaymentMethod ?? "Online";

        // Update buyer information in case it changed
        if (customer?.AppUser != null)
        {
            invoice.BuyerName = $"{customer.AppUser.FirstName} {customer.AppUser.LastName}".Trim();
            invoice.BuyerAddress = customer.Addresses?.FirstOrDefault()?.Street ?? "";
            invoice.BuyerCity = customer.Addresses?.FirstOrDefault()?.City ?? "";
            invoice.BuyerPostalCode = customer.Addresses?.FirstOrDefault()?.ZipCode ?? "";
        }

        // Update seller information from PlatformSettings (Zawaji)
        invoice.SellerName = platformSettings.LegalName is { Length: > 0 }
            ? platformSettings.LegalName
            : platformSettings.CompanyName is { Length: > 0 }
                ? platformSettings.CompanyName
                : "Zawaji";
        invoice.SellerVatNumber = platformSettings.VatNumber;
        invoice.SellerCommercialRegistrationNumber = platformSettings.CommercialRegistrationNumber;
        invoice.SellerAddress = BuildPlatformSellerAddress(platformSettings);
        invoice.SellerBuildingNumber = platformSettings.BuildingNumber;
        invoice.SellerStreetName = platformSettings.StreetName;
        invoice.SellerDistrict = platformSettings.District;
        invoice.SellerCity = platformSettings.City;
        invoice.SellerPostalCode = platformSettings.PostalCode;
        invoice.SellerCountryCode = platformSettings.CountryCode is { Length: > 0 }
            ? platformSettings.CountryCode : "SA";

        // Regenerate line items from current booking data (includes BookingPackage)
        invoice.LineItems = await BuildLineItemsForBookingAsync(booking, hall);

        // CRIT-002 FIX: Calculate header totals FROM line items to prevent mismatches
        RecalculateHeaderTotalsFromLineItems(invoice);

        // Add regeneration note
        var regenerationNote = $"Regenerated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC} due to booking modifications. " +
                               $"Previous totals - Subtotal: {originalSubtotal:F2}, Tax: {originalTax:F2}, Total: {originalTotal:F2}.";
        invoice.Notes = string.IsNullOrWhiteSpace(invoice.Notes)
            ? regenerationNote
            : $"{invoice.Notes} | {regenerationNote}";

        // Update regeneration tracking
        invoice.IsRegenerated = true;
        invoice.RegeneratedAt = DateTime.UtcNow;
        invoice.RegenerationCount++;

        // Preserve invoice number and creation date (already on the entity)
        invoice.UpdatedAt = DateTime.UtcNow;

        // Regenerate ZATCA fields
        invoice.QRCode = await GenerateZATCAQRCodeAsync(invoice);
        invoice.InvoiceHash = await GenerateInvoiceHashAsync(invoice);

        // Reset PDF flag since data changed
        invoice.IsPdfGenerated = false;
        invoice.PdfPath = string.Empty;

        _unitOfWork.InvoiceRepository.Update(invoice);
        await _unitOfWork.Complete();

        _logger.LogInformation(
            "Invoice {InvoiceNumber} regenerated successfully. New totals - Subtotal: {Subtotal}, Tax: {Tax}, Total: {Total}",
            invoice.InvoiceNumber, invoice.SubtotalBeforeTax, invoice.TaxAmount, invoice.TotalAmountWithTax);

        // Reload with full details for response
        return await _unitOfWork.InvoiceRepository.GetInvoiceWithDetailsAsync(invoiceId) ?? invoice;
    }

    #endregion

    #region Enhancement 5A: Invoice Cancellation with Enhanced Validation

    public async Task<Invoice> CancelInvoiceWithValidationAsync(int invoiceId, string reason, string cancelledBy)
    {
        _logger.LogInformation(
            "Cancelling invoice {InvoiceId} requested by {CancelledBy}. Reason: {Reason}",
            invoiceId, cancelledBy, reason);

        var invoice = await _unitOfWork.InvoiceRepository.GetInvoiceWithDetailsAsync(invoiceId);
        if (invoice == null)
        {
            throw new ArgumentException($"Invoice {invoiceId} not found");
        }

        // Cannot cancel an already cancelled invoice
        if (invoice.IsCancelled)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.InvoiceNumber} is already cancelled.");
        }

        // Cannot cancel paid invoices without a prior refund
        if (invoice.PaymentStatus == "Paid")
        {
            throw new InvalidOperationException(
                $"Cannot cancel paid invoice {invoice.InvoiceNumber}. " +
                "Process a refund first before cancelling.");
        }

        // Validate reason is provided
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Cancellation reason is required.");
        }

        invoice.IsCancelled = true;
        invoice.CancelledAt = DateTime.UtcNow;
        invoice.CancellationReason = reason;
        invoice.PaymentStatus = "Cancelled";
        invoice.UpdatedAt = DateTime.UtcNow;

        // Add cancellation note
        var cancellationNote = $"Cancelled on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC} by {cancelledBy}. Reason: {reason}";
        invoice.Notes = string.IsNullOrWhiteSpace(invoice.Notes)
            ? cancellationNote
            : $"{invoice.Notes} | {cancellationNote}";

        _unitOfWork.InvoiceRepository.Update(invoice);
        await _unitOfWork.Complete();

        _logger.LogInformation(
            "Invoice {InvoiceNumber} cancelled successfully by {CancelledBy}. Reason: {Reason}",
            invoice.InvoiceNumber, cancelledBy, reason);

        return invoice;
    }

    // CRIT-005 FIX: Legacy cancel method now delegates to validated method
    // to prevent bypassing paid-invoice checks and cancellation reason validation.
    public async Task<bool> CancelInvoiceAsync(int invoiceId, string reason)
    {
        // Delegate to the validated method which enforces:
        // - Cannot cancel already-cancelled invoices
        // - Cannot cancel paid invoices without prior refund
        // - Cancellation reason must be provided
        await CancelInvoiceWithValidationAsync(invoiceId, reason, "System");
        return true;
    }

    #endregion

    #region Enhancement 5B: Invoice Refund

    public async Task<Invoice> RefundInvoiceAsync(
        int invoiceId, decimal refundAmount, string reason, string refundMethod, int refundedByUserId)
    {
        _logger.LogInformation(
            "Processing refund for invoice {InvoiceId}. Amount: {Amount}, Method: {Method}, RequestedBy: {UserId}",
            invoiceId, refundAmount, refundMethod, refundedByUserId);

        var invoice = await _unitOfWork.InvoiceRepository.GetInvoiceWithDetailsAsync(invoiceId);
        if (invoice == null)
        {
            throw new ArgumentException($"Invoice {invoiceId} not found");
        }

        // Validate invoice is paid
        if (invoice.PaymentStatus != "Paid")
        {
            throw new InvalidOperationException(
                $"Cannot refund invoice {invoice.InvoiceNumber} - current status is '{invoice.PaymentStatus}'. " +
                "Only paid invoices can be refunded.");
        }

        // Cannot refund a cancelled invoice
        if (invoice.IsCancelled)
        {
            throw new InvalidOperationException(
                $"Cannot refund cancelled invoice {invoice.InvoiceNumber}.");
        }

        // Validate refund amount
        if (refundAmount <= 0)
        {
            throw new ArgumentException("Refund amount must be greater than zero.");
        }

        var maxRefundable = invoice.TotalAmountWithTax - invoice.RefundAmount;
        if (refundAmount > maxRefundable)
        {
            throw new ArgumentException(
                $"Refund amount {refundAmount:F2} exceeds maximum refundable amount {maxRefundable:F2} " +
                $"for invoice {invoice.InvoiceNumber}.");
        }

        // Validate reason is provided
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Refund reason is required.");
        }

        // Update invoice refund fields
        invoice.RefundAmount += refundAmount;
        invoice.RefundReason = reason;
        invoice.RefundMethod = refundMethod;
        invoice.RefundDate = DateTime.UtcNow;
        invoice.RefundedBy = refundedByUserId;

        // Determine if this is a full or partial refund
        var isFullRefund = invoice.RefundAmount >= invoice.TotalAmountWithTax;
        invoice.PaymentStatus = isFullRefund ? "Refunded" : "PartiallyRefunded";

        // Add refund note
        var refundNote = $"Refund of {refundAmount:F2} {invoice.Currency} processed on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}. " +
                         $"Method: {refundMethod}. Reason: {reason}. " +
                         $"Total refunded: {invoice.RefundAmount:F2}/{invoice.TotalAmountWithTax:F2}.";
        invoice.Notes = string.IsNullOrWhiteSpace(invoice.Notes)
            ? refundNote
            : $"{invoice.Notes} | {refundNote}";

        invoice.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.InvoiceRepository.Update(invoice);
        await _unitOfWork.Complete();

        _logger.LogInformation(
            "Refund processed for invoice {InvoiceNumber}. Amount: {RefundAmount}, Status: {Status}, TotalRefunded: {TotalRefunded}",
            invoice.InvoiceNumber, refundAmount, invoice.PaymentStatus, invoice.RefundAmount);

        return invoice;
    }

    #endregion

    #region Updates

    public async Task<Invoice> UpdateInvoiceAsync(Invoice invoice)
    {
        invoice.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.InvoiceRepository.Update(invoice);
        await _unitOfWork.Complete();
        return invoice;
    }

    public async Task<bool> UpdatePaymentStatusAsync(int invoiceId, string paymentStatus, string paymentReference = "")
    {
        var invoice = await _unitOfWork.InvoiceRepository.GetByIdAsync(invoiceId);
        if (invoice == null) return false;

        invoice.PaymentStatus = paymentStatus;
        invoice.PaymentReference = paymentReference;

        if (paymentStatus == "Paid")
        {
            invoice.PaymentDate = DateTime.UtcNow;
        }

        invoice.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.InvoiceRepository.Update(invoice);
        await _unitOfWork.Complete();

        _logger.LogInformation("Invoice {InvoiceNumber} payment status updated to {Status}", invoice.InvoiceNumber, paymentStatus);

        return true;
    }

    #endregion

    #region ZATCA Compliance

    /// <summary>
    /// Generate QR code content per ZATCA Phase 2 specification
    /// TLV (Tag-Length-Value) encoded data for simplified invoices
    /// </summary>
    public Task<string> GenerateZATCAQRCodeAsync(Invoice invoice)
    {
        // ZATCA QR Code contains TLV encoded values:
        // Tag 1: Seller Name
        // Tag 2: VAT Number
        // Tag 3: Invoice Date (ISO 8601)
        // Tag 4: Total with VAT
        // Tag 5: VAT Amount

        var tlvData = new List<byte>();

        // Tag 1: Seller Name
        AddTLV(tlvData, 1, invoice.SellerName);

        // Tag 2: VAT Registration Number
        AddTLV(tlvData, 2, invoice.SellerVatNumber);

        // Tag 3: Invoice Date/Time (ISO 8601)
        AddTLV(tlvData, 3, invoice.InvoiceDate.ToString("yyyy-MM-ddTHH:mm:ssZ"));

        // Tag 4: Total Amount with VAT
        AddTLV(tlvData, 4, invoice.TotalAmountWithTax.ToString("F2"));

        // Tag 5: VAT Amount
        AddTLV(tlvData, 5, invoice.TaxAmount.ToString("F2"));

        // Convert to Base64
        var qrCodeContent = Convert.ToBase64String(tlvData.ToArray());

        return Task.FromResult(qrCodeContent);
    }

    private void AddTLV(List<byte> data, byte tag, string value)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        data.Add(tag);
        data.Add((byte)valueBytes.Length);
        data.AddRange(valueBytes);
    }

    /// <summary>
    /// Generate SHA-256 hash of invoice for security verification
    /// </summary>
    public Task<string> GenerateInvoiceHashAsync(Invoice invoice)
    {
        var dataToHash = $"{invoice.InvoiceNumber}|{invoice.SellerVatNumber}|{invoice.InvoiceDate:O}|{invoice.TotalAmountWithTax:F2}|{invoice.TaxAmount:F2}";

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
        var hash = Convert.ToBase64String(hashBytes);

        return Task.FromResult(hash);
    }

    /// <summary>
    /// Bulk regenerate QR codes for all invoices using current PlatformSettings.
    /// This updates only the QR code and seller info — does NOT modify financial data.
    /// Safe to run on paid/cancelled invoices since it only fixes ZATCA compliance data.
    /// </summary>
    public async Task<(int Updated, int Failed, List<string> Errors)> BulkRegenerateQRCodesAsync(string requestedBy)
    {
        _logger.LogInformation("Bulk QR code regeneration requested by {RequestedBy}", requestedBy);

        // Validate PlatformSettings has required ZATCA fields before bulk operation
        var platformSettings = await _platformSettingsService.GetSettingsAsync();
        ValidatePlatformSettingsForZatca(platformSettings);

        var sellerAddress = BuildPlatformSellerAddress(platformSettings);
        var allInvoices = (await _unitOfWork.InvoiceRepository.GetAllAsync()).ToList();

        int updated = 0;
        int failed = 0;
        var errors = new List<string>();

        foreach (var invoice in allInvoices)
        {
            try
            {
                // Update seller info from current PlatformSettings
                invoice.SellerName = platformSettings.LegalName is { Length: > 0 }
                    ? platformSettings.LegalName
                    : platformSettings.CompanyName is { Length: > 0 }
                        ? platformSettings.CompanyName
                        : "Zawaji";
                invoice.SellerVatNumber = platformSettings.VatNumber;
                invoice.SellerCommercialRegistrationNumber = platformSettings.CommercialRegistrationNumber;
                invoice.SellerAddress = sellerAddress;
                invoice.SellerBuildingNumber = platformSettings.BuildingNumber;
                invoice.SellerStreetName = platformSettings.StreetName;
                invoice.SellerDistrict = platformSettings.District;
                invoice.SellerCity = platformSettings.City;
                invoice.SellerPostalCode = platformSettings.PostalCode;
                invoice.SellerCountryCode = platformSettings.CountryCode is { Length: > 0 }
                    ? platformSettings.CountryCode : "SA";

                // Regenerate QR code and hash with updated seller info
                invoice.QRCode = await GenerateZATCAQRCodeAsync(invoice);
                invoice.InvoiceHash = await GenerateInvoiceHashAsync(invoice);
                invoice.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.InvoiceRepository.Update(invoice);
                updated++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"Invoice {invoice.InvoiceNumber}: {ex.Message}");
                _logger.LogError(ex, "Failed to regenerate QR for invoice {InvoiceNumber}", invoice.InvoiceNumber);
            }
        }

        await _unitOfWork.Complete();

        _logger.LogInformation(
            "Bulk QR regeneration complete. Updated: {Updated}, Failed: {Failed}, Total: {Total}",
            updated, failed, allInvoices.Count);

        return (updated, failed, errors);
    }

    #endregion

    #region PDF Generation

    public async Task<string> GenerateInvoicePdfAsync(int invoiceId)
    {
        var invoice = await _unitOfWork.InvoiceRepository.GetInvoiceWithDetailsAsync(invoiceId);
        if (invoice == null)
        {
            throw new ArgumentException($"Invoice {invoiceId} not found");
        }

        // PDF generation would use a library like QuestPDF, iTextSharp, or similar
        // For now, just mark as generated and return path placeholder
        var pdfPath = $"/invoices/{invoice.InvoiceNumber}.pdf";

        invoice.PdfPath = pdfPath;
        invoice.IsPdfGenerated = true;
        invoice.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.InvoiceRepository.Update(invoice);
        await _unitOfWork.Complete();

        _logger.LogInformation("Invoice PDF generated: {PdfPath}", pdfPath);

        return pdfPath;
    }

    public async Task<byte[]> GetInvoicePdfBytesAsync(int invoiceId)
    {
        var invoice = await _unitOfWork.InvoiceRepository.GetByIdAsync(invoiceId);
        if (invoice == null || string.IsNullOrEmpty(invoice.PdfPath))
        {
            // Generate PDF if not exists
            await GenerateInvoicePdfAsync(invoiceId);
            invoice = await _unitOfWork.InvoiceRepository.GetByIdAsync(invoiceId);
        }

        // In a real implementation, this would read the PDF file from storage
        // For now, return empty array as placeholder
        return Array.Empty<byte>();
    }

    #endregion

    #region Private Helpers - Invoice Building

    /// <summary>
    /// CRIT-002 FIX: Recalculates invoice header financial totals from line items.
    /// This ensures header totals always match the sum of line items, preventing discrepancies
    /// when booking totals differ from the sum of individually calculated line items.
    /// </summary>
    private static void RecalculateHeaderTotalsFromLineItems(Invoice invoice)
    {
        if (invoice.LineItems == null || !invoice.LineItems.Any())
            return;

        invoice.SubtotalBeforeTax = invoice.LineItems.Sum(li => li.SubtotalBeforeTax);
        invoice.TaxAmount = invoice.LineItems.Sum(li => li.TaxAmount);
        invoice.TotalAmountWithTax = invoice.LineItems.Sum(li => li.TotalAmount);
        invoice.DiscountAmount = invoice.LineItems.Sum(li => li.DiscountAmount);
        invoice.TaxableAmount = invoice.SubtotalBeforeTax - invoice.DiscountAmount;
    }

    /// <summary>
    /// Validates that PlatformSettings has all ZATCA-required fields populated.
    /// Logs structured warnings for any missing fields to aid compliance monitoring.
    /// </summary>
    private void ValidatePlatformSettingsForZatca(PlatformSettings settings)
    {
        // ZATCA-critical: VAT number is required for valid QR codes
        if (string.IsNullOrWhiteSpace(settings.VatNumber))
        {
            _logger.LogError("PlatformSettings missing VAT number - cannot generate ZATCA-compliant invoice. " +
                             "Configure via Admin > Settings > Platform.");
            throw new InvalidOperationException(
                "Cannot generate invoice: Platform VAT number is not configured. " +
                "Please go to Admin > Settings > Platform and set the VAT Registration Number before generating invoices.");
        }

        if (string.IsNullOrWhiteSpace(settings.LegalName) && string.IsNullOrWhiteSpace(settings.CompanyName))
        {
            _logger.LogError("PlatformSettings missing both LegalName and CompanyName - cannot generate ZATCA-compliant invoice.");
            throw new InvalidOperationException(
                "Cannot generate invoice: Platform company name is not configured. " +
                "Please go to Admin > Settings > Platform and set the Legal Name or Company Name.");
        }

        // Non-blocking warnings for recommended fields
        if (string.IsNullOrWhiteSpace(settings.CommercialRegistrationNumber))
            _logger.LogWarning("PlatformSettings missing Commercial Registration number - invoices may fail ZATCA validation.");

        if (string.IsNullOrWhiteSpace(settings.City))
            _logger.LogWarning("PlatformSettings missing City - invoices may fail ZATCA address validation.");

        if (string.IsNullOrWhiteSpace(settings.PostalCode))
            _logger.LogWarning("PlatformSettings missing PostalCode - invoices may fail ZATCA address validation.");

        if (string.IsNullOrWhiteSpace(settings.StreetName))
            _logger.LogWarning("PlatformSettings missing StreetName - invoices may fail ZATCA address validation.");

        if (string.IsNullOrWhiteSpace(settings.BuildingNumber))
            _logger.LogWarning("PlatformSettings missing BuildingNumber - invoices may fail ZATCA address validation.");

        if (string.IsNullOrWhiteSpace(settings.District))
            _logger.LogWarning("PlatformSettings missing District - invoices may fail ZATCA address validation.");
    }

    /// <summary>
    /// Builds a composite seller address string from PlatformSettings fields.
    /// </summary>
    private static string BuildPlatformSellerAddress(PlatformSettings settings)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(settings.BuildingNumber))
            parts.Add(settings.BuildingNumber);
        if (!string.IsNullOrWhiteSpace(settings.StreetName))
            parts.Add(settings.StreetName);
        if (!string.IsNullOrWhiteSpace(settings.District))
            parts.Add(settings.District);
        if (!string.IsNullOrWhiteSpace(settings.City))
            parts.Add(settings.City);
        if (!string.IsNullOrWhiteSpace(settings.PostalCode))
            parts.Add(settings.PostalCode);
        if (!string.IsNullOrWhiteSpace(settings.CountryCode))
            parts.Add(settings.CountryCode);

        return parts.Count > 0 ? string.Join(", ", parts) : "Saudi Arabia";
    }

    /// <summary>
    /// Builds an Invoice entity from booking data. Does not include line items or ZATCA fields.
    /// Seller info comes from PlatformSettings (Zawaji) as the deemed supplier.
    /// </summary>
    private static Invoice BuildInvoiceFromBooking(
        Booking booking,
        Core.Entities.CustomerEntities.Customer customer,
        Hall? hall,
        PlatformSettings platformSettings,
        string sellerAddress,
        string invoiceNumber,
        string createdBy)
    {
        return new Invoice
        {
            InvoiceNumber = invoiceNumber,
            InvoiceType = "Standard",
            InvoiceDate = DateTime.UtcNow,
            SupplyDate = booking.EventDate,
            BookingId = booking.Id,
            CustomerId = booking.CustomerId,
            HallId = booking.HallId,

            // Seller Information — Zawaji (deemed supplier)
            SellerName = platformSettings.LegalName is { Length: > 0 }
                ? platformSettings.LegalName
                : platformSettings.CompanyName is { Length: > 0 }
                    ? platformSettings.CompanyName
                    : "Zawaji",
            SellerVatNumber = platformSettings.VatNumber,
            SellerCommercialRegistrationNumber = platformSettings.CommercialRegistrationNumber,
            SellerAddress = sellerAddress,
            SellerBuildingNumber = platformSettings.BuildingNumber,
            SellerStreetName = platformSettings.StreetName,
            SellerDistrict = platformSettings.District,
            SellerCity = platformSettings.City,
            SellerPostalCode = platformSettings.PostalCode,
            SellerCountryCode = platformSettings.CountryCode is { Length: > 0 }
                ? platformSettings.CountryCode
                : "SA",

            // Buyer Information
            BuyerName = customer.AppUser != null
                ? $"{customer.AppUser.FirstName} {customer.AppUser.LastName}".Trim()
                : "Customer",
            BuyerVatNumber = "",
            BuyerAddress = customer.Addresses?.FirstOrDefault()?.Street ?? "",
            BuyerCity = customer.Addresses?.FirstOrDefault()?.City ?? "",
            BuyerPostalCode = customer.Addresses?.FirstOrDefault()?.ZipCode ?? "",
            BuyerCountryCode = "SA",

            // Financial Details
            SubtotalBeforeTax = booking.Subtotal,
            TaxableAmount = booking.Subtotal - booking.DiscountAmount,
            TaxRate = TAX_RATE,
            TaxAmount = booking.TaxAmount,
            DiscountAmount = booking.DiscountAmount,
            TotalAmountWithTax = booking.TotalAmount,
            Currency = booking.Currency ?? "SAR",

            // Payment Information
            PaymentMethod = booking.PaymentMethod ?? "Online",
            PaymentStatus = booking.PaymentStatus ?? "Pending",
            PaymentDate = booking.PaidAt,

            // Notes
            Notes = $"Booking Reference: {booking.Id}. Event Date: {booking.EventDate:yyyy-MM-dd}. Guest Count: {booking.GuestCount}.",
            Terms = "Payment due upon confirmation. Cancellation policy applies as per terms and conditions.",

            // Audit
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>
    /// Builds invoice line items from a booking, including:
    /// 1. Hall rental (if HallCost > 0)
    /// 2. BookingPackage (Enhancement 1 - if package exists)
    /// 3. Vendor services
    /// 4. Fallback generic line item if nothing else was created
    /// </summary>
    private async Task<List<InvoiceLineItem>> BuildLineItemsForBookingAsync(Booking booking, Hall? hall)
    {
        var lineItems = new List<InvoiceLineItem>();
        var lineNumber = 1;

        // 1. Hall Rental as first line item
        if (booking.HallCost > 0)
        {
            var hallSubtotal = booking.HallCost;
            var hallTax = Math.Round(hallSubtotal * TAX_RATE, 2);

            lineItems.Add(new InvoiceLineItem
            {
                LineNumber = lineNumber++,
                Description = $"Hall Rental - {hall?.Name ?? "Event Hall"}",
                ItemCode = $"HALL-{booking.HallId}",
                Quantity = 1,
                Unit = "Service",
                UnitPrice = hallSubtotal,
                DiscountAmount = 0,
                SubtotalBeforeTax = hallSubtotal,
                TaxRate = TAX_RATE,
                TaxAmount = hallTax,
                TotalAmount = hallSubtotal + hallTax,
                TaxCategory = "Standard"
            });
        }

        // 2. Enhancement 1: BookingPackage line item (after hall rental, before vendor services)
        if (booking.PackageDetails != null && booking.PackageDetails.IsActive)
        {
            var packagePrice = booking.PackageDetails.Price;
            var packageTax = Math.Round(packagePrice * TAX_RATE, 2);

            var packageDescription = booking.PackageDetails.Name;
            if (!string.IsNullOrWhiteSpace(booking.PackageDetails.Features))
            {
                packageDescription += $" ({booking.PackageDetails.Features})";
            }

            lineItems.Add(new InvoiceLineItem
            {
                LineNumber = lineNumber++,
                Description = $"Package - {packageDescription}",
                ItemCode = $"PKG-{booking.PackageDetails.Id}",
                Quantity = 1,
                Unit = "Package",
                UnitPrice = packagePrice,
                DiscountAmount = 0,
                SubtotalBeforeTax = packagePrice,
                TaxRate = TAX_RATE,
                TaxAmount = packageTax,
                TotalAmount = packagePrice + packageTax,
                TaxCategory = "Standard"
            });
        }

        // 3. Vendor Services as line items (only approved/confirmed vendors)
        if (booking.VendorBookings != null && booking.VendorBookings.Any())
        {
            // Filter to only include approved or confirmed vendor bookings.
            // Rejected vendor services must not appear on invoices (HIGH-002 partial approval fix).
            var approvedVendorBookings = booking.VendorBookings
                .Where(vb => vb.Status == "Approved" || vb.Status == "Confirmed")
                .ToList();

            if (approvedVendorBookings.Count < booking.VendorBookings.Count)
            {
                var skippedCount = booking.VendorBookings.Count - approvedVendorBookings.Count;
                _logger.LogInformation(
                    "Invoice for booking {BookingId}: Skipping {SkippedCount} non-approved vendor booking(s). " +
                    "Including {IncludedCount} approved vendor booking(s).",
                    booking.Id, skippedCount, approvedVendorBookings.Count);
            }

            foreach (var vendorBooking in approvedVendorBookings)
            {
                var vendor = await _unitOfWork.VendorRepository.GetByIdAsync(vendorBooking.VendorId);

                if (vendorBooking.Services != null && vendorBooking.Services.Any())
                {
                    foreach (var service in vendorBooking.Services)
                    {
                        var serviceSubtotal = service.TotalPrice;
                        var serviceTax = Math.Round(serviceSubtotal * TAX_RATE, 2);

                        lineItems.Add(new InvoiceLineItem
                        {
                            LineNumber = lineNumber++,
                            Description = $"{vendor?.Name ?? "Vendor"} - {service.ServiceItem?.Name ?? "Service"}",
                            ItemCode = $"SVC-{service.ServiceItemId}",
                            Quantity = service.Quantity,
                            Unit = "Service",
                            UnitPrice = service.UnitPrice,
                            DiscountAmount = 0,
                            SubtotalBeforeTax = serviceSubtotal,
                            TaxRate = TAX_RATE,
                            TaxAmount = serviceTax,
                            TotalAmount = serviceSubtotal + serviceTax,
                            TaxCategory = "Standard"
                        });
                    }
                }
                else
                {
                    // No detailed services, add vendor total as single line item
                    var vendorSubtotal = vendorBooking.TotalAmount;
                    var vendorTax = Math.Round(vendorSubtotal * TAX_RATE, 2);

                    lineItems.Add(new InvoiceLineItem
                    {
                        LineNumber = lineNumber++,
                        Description = $"{vendor?.Name ?? "Vendor"} Services",
                        ItemCode = $"VENDOR-{vendorBooking.VendorId}",
                        Quantity = 1,
                        Unit = "Service",
                        UnitPrice = vendorSubtotal,
                        DiscountAmount = 0,
                        SubtotalBeforeTax = vendorSubtotal,
                        TaxRate = TAX_RATE,
                        TaxAmount = vendorTax,
                        TotalAmount = vendorSubtotal + vendorTax,
                        TaxCategory = "Standard"
                    });
                }
            }
        }

        // 4. Fallback: if no line items were created, add a generic one
        if (!lineItems.Any())
        {
            var subtotal = booking.Subtotal;
            var tax = booking.TaxAmount;

            lineItems.Add(new InvoiceLineItem
            {
                LineNumber = 1,
                Description = "Event Booking Services",
                ItemCode = $"BOOKING-{booking.Id}",
                Quantity = 1,
                Unit = "Service",
                UnitPrice = subtotal,
                DiscountAmount = booking.DiscountAmount,
                SubtotalBeforeTax = subtotal - booking.DiscountAmount,
                TaxRate = TAX_RATE,
                TaxAmount = tax,
                TotalAmount = booking.TotalAmount,
                TaxCategory = "Standard"
            });
        }

        return lineItems;
    }

    #endregion

    #region Statistics

    public async Task<InvoiceStatistics> GetInvoiceStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var allInvoices = await _unitOfWork.InvoiceRepository.GetAllAsync();
        var invoices = allInvoices.ToList();

        if (startDate.HasValue)
        {
            invoices = invoices.Where(i => i.InvoiceDate >= startDate.Value).ToList();
        }

        if (endDate.HasValue)
        {
            invoices = invoices.Where(i => i.InvoiceDate <= endDate.Value).ToList();
        }

        return new InvoiceStatistics
        {
            TotalInvoices = invoices.Count,
            PendingInvoices = invoices.Count(i => i.PaymentStatus == "Pending"),
            PaidInvoices = invoices.Count(i => i.PaymentStatus == "Paid"),
            CancelledInvoices = invoices.Count(i => i.IsCancelled),
            TotalRevenue = invoices.Where(i => i.PaymentStatus == "Paid").Sum(i => i.TotalAmountWithTax),
            TotalTaxCollected = invoices.Where(i => i.PaymentStatus == "Paid").Sum(i => i.TaxAmount),
            AverageInvoiceAmount = invoices.Any() ? invoices.Average(i => i.TotalAmountWithTax) : 0
        };
    }

    #endregion

    #region Enhanced Methods for Invoice Page Redesign

    /// <inheritdoc />
    public async Task<InvoiceStatisticsRaw> GetInvoiceStatisticsForRoleAsync(IEnumerable<int>? hallIds)
    {
        _logger.LogInformation(
            "Getting invoice statistics for role scope. HallIds: {HallIdScope}",
            hallIds == null ? "All (Admin)" : string.Join(",", hallIds));

        return await _unitOfWork.InvoiceRepository.GetInvoiceStatisticsAsync(hallIds);
    }

    /// <inheritdoc />
    public async Task<(List<Invoice> Items, int TotalCount)> GetInvoicesForAdminAsync(
        string? search, string? status, string? paymentMethod, int? hallId,
        int? organizationId, decimal? minAmount, decimal? maxAmount,
        DateTime? startDate, DateTime? endDate, bool? isCancelled,
        string sortBy, bool sortDescending, int pageNumber, int pageSize)
    {
        _logger.LogInformation("Getting filtered invoices for Admin");

        return await _unitOfWork.InvoiceRepository.GetFilteredInvoicesAsync(
            hallIds: null, // Admin: no restriction
            search, status, paymentMethod, hallId,
            organizationId, minAmount, maxAmount,
            startDate, endDate, isCancelled,
            sortBy, sortDescending, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<(List<Invoice> Items, int TotalCount)> GetInvoicesForOrgManagerAsync(
        IEnumerable<int> orgHallIds,
        string? search, string? status, string? paymentMethod, int? hallId,
        decimal? minAmount, decimal? maxAmount,
        DateTime? startDate, DateTime? endDate, bool? isCancelled,
        string sortBy, bool sortDescending, int pageNumber, int pageSize)
    {
        var hallIdsList = orgHallIds.ToList();
        _logger.LogInformation(
            "Getting filtered invoices for OrgManager. Scoped to {HallCount} halls",
            hallIdsList.Count);

        // If a specific hallId filter is provided, validate it is within the org scope
        if (hallId.HasValue && !hallIdsList.Contains(hallId.Value))
        {
            _logger.LogWarning(
                "OrgManager requested hallId {HallId} not in their organization scope",
                hallId.Value);
            return (new List<Invoice>(), 0);
        }

        return await _unitOfWork.InvoiceRepository.GetFilteredInvoicesAsync(
            hallIds: hallIdsList,
            search, status, paymentMethod, hallId,
            organizationId: null, // Not applicable for org manager
            minAmount, maxAmount,
            startDate, endDate, isCancelled,
            sortBy, sortDescending, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<(List<Invoice> Items, int TotalCount)> GetInvoicesForHallManagerAsync(
        IEnumerable<int> assignedHallIds,
        string? search, string? status, string? paymentMethod, int? hallId,
        decimal? minAmount, decimal? maxAmount,
        DateTime? startDate, DateTime? endDate, bool? isCancelled,
        string sortBy, bool sortDescending, int pageNumber, int pageSize)
    {
        var hallIdsList = assignedHallIds.ToList();
        _logger.LogInformation(
            "Getting filtered invoices for HallManager. Scoped to {HallCount} halls",
            hallIdsList.Count);

        // If a specific hallId filter is provided, validate it is within the assigned scope
        if (hallId.HasValue && !hallIdsList.Contains(hallId.Value))
        {
            _logger.LogWarning(
                "HallManager requested hallId {HallId} not in their assigned halls",
                hallId.Value);
            return (new List<Invoice>(), 0);
        }

        return await _unitOfWork.InvoiceRepository.GetFilteredInvoicesAsync(
            hallIds: hallIdsList,
            search, status, paymentMethod, hallId,
            organizationId: null, // Not applicable for hall manager
            minAmount, maxAmount,
            startDate, endDate, isCancelled,
            sortBy, sortDescending, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<int>> GetHallIdsByOrganizationIdAsync(int organizationId)
    {
        return await _unitOfWork.InvoiceRepository.GetHallIdsByOrganizationIdAsync(organizationId);
    }

    /// <inheritdoc />
    public async Task<List<Invoice>> GetInvoicesByIdsAsync(IEnumerable<int> invoiceIds)
    {
        return await _unitOfWork.InvoiceRepository.GetInvoicesByIdsAsync(invoiceIds);
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportInvoicesToCsvAsync(IEnumerable<int> invoiceIds)
    {
        var invoices = await _unitOfWork.InvoiceRepository.GetInvoicesByIdsAsync(invoiceIds);

        _logger.LogInformation(
            "Exporting {Count} invoices to CSV",
            invoices.Count);

        var sb = new StringBuilder();

        // CSV Header
        sb.AppendLine("Invoice Number,Invoice Date,Customer Name,Hall Name,Subtotal Before Tax,Tax Amount,Total Amount,Currency,Payment Status,Payment Method,Payment Date,Created At,Is Cancelled,Cancellation Reason,Refund Amount");

        foreach (var invoice in invoices)
        {
            var customerName = invoice.Customer?.AppUser != null
                ? $"{invoice.Customer.AppUser.FirstName} {invoice.Customer.AppUser.LastName}".Trim()
                : invoice.BuyerName;
            var hallName = invoice.Hall?.Name ?? "";

            sb.AppendLine(string.Join(",",
                EscapeCsvField(invoice.InvoiceNumber),
                invoice.InvoiceDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                EscapeCsvField(customerName),
                EscapeCsvField(hallName),
                invoice.SubtotalBeforeTax.ToString("F2", CultureInfo.InvariantCulture),
                invoice.TaxAmount.ToString("F2", CultureInfo.InvariantCulture),
                invoice.TotalAmountWithTax.ToString("F2", CultureInfo.InvariantCulture),
                invoice.Currency,
                invoice.PaymentStatus,
                invoice.PaymentMethod,
                invoice.PaymentDate?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "",
                invoice.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                invoice.IsCancelled.ToString(),
                EscapeCsvField(invoice.CancellationReason),
                invoice.RefundAmount.ToString("F2", CultureInfo.InvariantCulture)));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportInvoicesToExcelAsync(IEnumerable<int> invoiceIds)
    {
        // Excel-compatible CSV with tab separator for better Excel import
        var invoices = await _unitOfWork.InvoiceRepository.GetInvoicesByIdsAsync(invoiceIds);

        _logger.LogInformation(
            "Exporting {Count} invoices to Excel format",
            invoices.Count);

        var sb = new StringBuilder();

        // Tab-separated header for Excel
        sb.AppendLine(string.Join("\t",
            "Invoice Number", "Invoice Date", "Customer Name", "Hall Name",
            "Subtotal Before Tax", "Tax Rate (%)", "Tax Amount", "Discount Amount",
            "Total Amount", "Currency", "Payment Status", "Payment Method",
            "Payment Date", "Payment Reference", "Invoice Type",
            "Seller Name", "Seller VAT", "Buyer Name",
            "Created At", "Created By", "Is Cancelled", "Cancellation Reason",
            "Refund Amount", "Refund Reason", "ZATCA Status"));

        foreach (var invoice in invoices)
        {
            var customerName = invoice.Customer?.AppUser != null
                ? $"{invoice.Customer.AppUser.FirstName} {invoice.Customer.AppUser.LastName}".Trim()
                : invoice.BuyerName;
            var hallName = invoice.Hall?.Name ?? "";

            sb.AppendLine(string.Join("\t",
                invoice.InvoiceNumber,
                invoice.InvoiceDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                customerName,
                hallName,
                invoice.SubtotalBeforeTax.ToString("F2", CultureInfo.InvariantCulture),
                invoice.TaxRate.ToString("F2", CultureInfo.InvariantCulture),
                invoice.TaxAmount.ToString("F2", CultureInfo.InvariantCulture),
                invoice.DiscountAmount.ToString("F2", CultureInfo.InvariantCulture),
                invoice.TotalAmountWithTax.ToString("F2", CultureInfo.InvariantCulture),
                invoice.Currency,
                invoice.PaymentStatus,
                invoice.PaymentMethod,
                invoice.PaymentDate?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "",
                invoice.PaymentReference,
                invoice.InvoiceType,
                invoice.SellerName,
                invoice.SellerVatNumber,
                invoice.BuyerName,
                invoice.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                invoice.CreatedBy,
                invoice.IsCancelled.ToString(),
                invoice.CancellationReason,
                invoice.RefundAmount.ToString("F2", CultureInfo.InvariantCulture),
                invoice.RefundReason,
                invoice.ZATCA_Status));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    /// <summary>
    /// Escapes a CSV field by wrapping in quotes if it contains commas, quotes, or newlines.
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    #endregion
}
