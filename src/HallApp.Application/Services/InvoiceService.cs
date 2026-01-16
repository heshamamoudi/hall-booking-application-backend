using System.Security.Cryptography;
using System.Text;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

/// <summary>
/// Invoice Service implementation - handles invoice generation, management and ZATCA compliance
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InvoiceService> _logger;

    // Platform seller information (should be configured in appsettings)
    private const string PLATFORM_NAME = "HallApp Platform";
    private const string PLATFORM_VAT_NUMBER = "300000000000003"; // Example 15-digit VAT
    private const string PLATFORM_CR_NUMBER = "1010000000"; // Commercial Registration
    private const string PLATFORM_ADDRESS = "Riyadh, Saudi Arabia";
    private const string PLATFORM_CITY = "Riyadh";
    private const string PLATFORM_POSTAL_CODE = "12345";
    private const decimal TAX_RATE = 15.00m; // 15% VAT in Saudi Arabia

    public InvoiceService(IUnitOfWork unitOfWork, ILogger<InvoiceService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    #region Core CRUD

    public async Task<Invoice> GetInvoiceByIdAsync(int invoiceId)
    {
        return await _unitOfWork.InvoiceRepository.GetByIdAsync(invoiceId) ?? new Invoice();
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
        var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
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

        // Generate invoice number
        var invoiceNumber = await GenerateInvoiceNumberAsync();

        // Create invoice
        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            InvoiceType = "Standard", // B2C simplified invoice for individual customers
            InvoiceDate = DateTime.UtcNow,
            SupplyDate = booking.EventDate,
            BookingId = bookingId,
            CustomerId = booking.CustomerId,
            HallId = booking.HallId,

            // Seller Information (Platform/Hall)
            SellerName = hall?.Name ?? PLATFORM_NAME,
            SellerVatNumber = PLATFORM_VAT_NUMBER,
            SellerCommercialRegistrationNumber = PLATFORM_CR_NUMBER,
            SellerAddress = hall?.Location?.Address ?? PLATFORM_ADDRESS,
            SellerCity = hall?.Location?.City ?? PLATFORM_CITY,
            SellerPostalCode = PLATFORM_POSTAL_CODE,
            SellerCountryCode = "SA",

            // Buyer Information (Customer)
            BuyerName = customer.AppUser != null
                ? $"{customer.AppUser.FirstName} {customer.AppUser.LastName}".Trim()
                : "Customer",
            BuyerVatNumber = "", // Individual customers usually don't have VAT numbers
            BuyerAddress = customer.Addresses?.FirstOrDefault()?.Street ?? "",
            BuyerCity = customer.Addresses?.FirstOrDefault()?.City ?? "",
            BuyerPostalCode = customer.Addresses?.FirstOrDefault()?.ZipCode ?? "",
            BuyerCountryCode = "SA",

            // Financial Details from Booking
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
            Notes = $"Booking Reference: {bookingId}. Event Date: {booking.EventDate:yyyy-MM-dd}. Guest Count: {booking.GuestCount}.",
            Terms = "Payment due upon confirmation. Cancellation policy applies as per terms and conditions.",

            // Audit
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        // Create line items
        invoice.LineItems = new List<InvoiceLineItem>();
        int lineNumber = 1;

        // Add Hall Rental as first line item
        if (booking.HallCost > 0)
        {
            var hallSubtotal = booking.HallCost;
            var hallTax = Math.Round(hallSubtotal * (TAX_RATE / 100), 2);

            invoice.LineItems.Add(new InvoiceLineItem
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

        // Add Vendor Services as line items
        if (booking.VendorBookings != null && booking.VendorBookings.Any())
        {
            foreach (var vendorBooking in booking.VendorBookings)
            {
                var vendor = await _unitOfWork.VendorRepository.GetByIdAsync(vendorBooking.VendorId);

                if (vendorBooking.Services != null && vendorBooking.Services.Any())
                {
                    foreach (var service in vendorBooking.Services)
                    {
                        var serviceSubtotal = service.TotalPrice;
                        var serviceTax = Math.Round(serviceSubtotal * (TAX_RATE / 100), 2);

                        invoice.LineItems.Add(new InvoiceLineItem
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
                    var vendorTax = Math.Round(vendorSubtotal * (TAX_RATE / 100), 2);

                    invoice.LineItems.Add(new InvoiceLineItem
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

        // If no line items were created (shouldn't happen), add a generic one
        if (!invoice.LineItems.Any())
        {
            var subtotal = booking.Subtotal;
            var tax = booking.TaxAmount;

            invoice.LineItems.Add(new InvoiceLineItem
            {
                LineNumber = 1,
                Description = "Event Booking Services",
                ItemCode = $"BOOKING-{bookingId}",
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

        // Generate ZATCA QR Code
        invoice.QRCode = await GenerateZATCAQRCodeAsync(invoice);

        // Generate Invoice Hash
        invoice.InvoiceHash = await GenerateInvoiceHashAsync(invoice);

        // Generate UUID for ZATCA
        invoice.ZATCA_UUID = Guid.NewGuid().ToString();

        // Save invoice
        await _unitOfWork.InvoiceRepository.AddAsync(invoice);
        await _unitOfWork.Complete();

        _logger.LogInformation("Invoice {InvoiceNumber} generated successfully for booking {BookingId}", invoice.InvoiceNumber, bookingId);

        return invoice;
    }

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _unitOfWork.InvoiceRepository.GetInvoiceCountForYearAsync(year);
        var sequence = count + 1;

        // Format: INV-YYYY-XXXXXX (e.g., INV-2026-000001)
        return $"INV-{year}-{sequence:D6}";
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

    public async Task<bool> CancelInvoiceAsync(int invoiceId, string reason)
    {
        var invoice = await _unitOfWork.InvoiceRepository.GetByIdAsync(invoiceId);
        if (invoice == null) return false;

        invoice.IsCancelled = true;
        invoice.CancelledAt = DateTime.UtcNow;
        invoice.CancellationReason = reason;
        invoice.PaymentStatus = "Cancelled";
        invoice.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.InvoiceRepository.Update(invoice);
        await _unitOfWork.Complete();

        _logger.LogInformation("Invoice {InvoiceNumber} cancelled. Reason: {Reason}", invoice.InvoiceNumber, reason);

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
        var valueBytes = Encoding.UTF8.GetBytes(value);
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
}
