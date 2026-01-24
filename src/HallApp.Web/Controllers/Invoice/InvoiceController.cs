using AutoMapper;
using HallApp.Core.Exceptions;
using HallApp.Application.DTOs.Invoice;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.Invoice;

/// <summary>
/// Controller for Invoice management
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InvoiceController : BaseApiController
{
    private readonly IInvoiceService _invoiceService;
    private readonly IMapper _mapper;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(
        IInvoiceService invoiceService,
        IMapper mapper,
        ILogger<InvoiceController> logger)
    {
        _invoiceService = invoiceService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all invoices (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceListDto>>>> GetAllInvoices()
    {
        try
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            var invoiceDtos = _mapper.Map<IEnumerable<InvoiceListDto>>(invoices);
            return Success(invoiceDtos, "Invoices retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices");
            return Error<IEnumerable<InvoiceListDto>>($"Failed to retrieve invoices: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get invoice by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoiceById(int id)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null || invoice.Id == 0)
            {
                return Error<InvoiceDto>("Invoice not found", 404);
            }

            // Authorization check: customer can only see their own invoices
            var userId = UserId;
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("HallManager");
            if (!isAdmin && invoice.CustomerId != userId)
            {
                return Error<InvoiceDto>("Access denied", 403);
            }

            var invoiceDto = _mapper.Map<InvoiceDto>(invoice);
            return Success(invoiceDto, "Invoice retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice {InvoiceId}", id);
            return Error<InvoiceDto>($"Failed to retrieve invoice: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get invoice by invoice number
    /// </summary>
    [HttpGet("by-number/{invoiceNumber}")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoiceByNumber(string invoiceNumber)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceByNumberAsync(invoiceNumber);
            if (invoice == null)
            {
                return Error<InvoiceDto>("Invoice not found", 404);
            }

            // Authorization check
            var userId = UserId;
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("HallManager");
            if (!isAdmin && invoice.CustomerId != userId)
            {
                return Error<InvoiceDto>("Access denied", 403);
            }

            var invoiceDto = _mapper.Map<InvoiceDto>(invoice);
            return Success(invoiceDto, "Invoice retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice by number {InvoiceNumber}", invoiceNumber);
            return Error<InvoiceDto>($"Failed to retrieve invoice: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get invoice by booking ID
    /// </summary>
    [HttpGet("by-booking/{bookingId:int}")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoiceByBookingId(int bookingId)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceByBookingIdAsync(bookingId);
            if (invoice == null)
            {
                return Error<InvoiceDto>("Invoice not found for this booking", 404);
            }

            // Authorization check
            var userId = UserId;
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("HallManager");
            if (!isAdmin && invoice.CustomerId != userId)
            {
                return Error<InvoiceDto>("Access denied", 403);
            }

            var invoiceDto = _mapper.Map<InvoiceDto>(invoice);
            return Success(invoiceDto, "Invoice retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice for booking {BookingId}", bookingId);
            return Error<InvoiceDto>($"Failed to retrieve invoice: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get invoices based on user role:
    /// - Customer: their own invoices
    /// - HallManager: invoices for their assigned halls
    /// - VendorManager: invoices containing their assigned vendors
    /// - Admin: all invoices
    /// </summary>
    [HttpGet("my-invoices")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceListDto>>>> GetMyInvoices(
        [FromServices] IHallManagerService hallManagerService,
        [FromServices] IVendorManagerService vendorManagerService)
    {
        try
        {
            var userId = UserId;
            IEnumerable<Core.Entities.BookingEntities.Invoice> invoices;

            if (User.IsInRole("Admin"))
            {
                // Admin sees all invoices
                invoices = await _invoiceService.GetAllInvoicesAsync();
            }
            else if (User.IsInRole("HallManager"))
            {
                // Hall Manager sees invoices for their assigned halls
                var hallManager = await hallManagerService.GetHallManagerByAppUserIdAsync(userId);
                if (hallManager != null && hallManager.Halls.Any())
                {
                    var hallIds = hallManager.Halls.Select(h => h.ID).ToList();
                    var allInvoices = new List<Core.Entities.BookingEntities.Invoice>();
                    foreach (var hallId in hallIds)
                    {
                        var hallInvoices = await _invoiceService.GetInvoicesByHallIdAsync(hallId);
                        allInvoices.AddRange(hallInvoices);
                    }
                    invoices = allInvoices.DistinctBy(i => i.Id).ToList();
                }
                else
                {
                    invoices = new List<Core.Entities.BookingEntities.Invoice>();
                }
            }
            else if (User.IsInRole("VendorManager"))
            {
                // Vendor Manager sees invoices containing their assigned vendors
                var vendorManager = await vendorManagerService.GetVendorManagerByAppUserIdAsync(userId);
                if (vendorManager != null && vendorManager.Vendors.Any())
                {
                    var vendorIds = vendorManager.Vendors.Select(v => v.Id).ToList();
                    var allInvoices = new List<Core.Entities.BookingEntities.Invoice>();
                    foreach (var vendorId in vendorIds)
                    {
                        var vendorInvoices = await _invoiceService.GetInvoicesByVendorIdAsync(vendorId);
                        allInvoices.AddRange(vendorInvoices);
                    }
                    invoices = allInvoices.DistinctBy(i => i.Id).ToList();
                }
                else
                {
                    invoices = new List<Core.Entities.BookingEntities.Invoice>();
                }
            }
            else
            {
                // Customer sees their own invoices
                invoices = await _invoiceService.GetInvoicesByCustomerIdAsync(userId);
            }

            var invoiceDtos = _mapper.Map<IEnumerable<InvoiceListDto>>(invoices);
            return Success(invoiceDtos, "Your invoices retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices for user {UserId}", UserId);
            return Error<IEnumerable<InvoiceListDto>>($"Failed to retrieve invoices: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get invoices by hall ID (Admin/HallManager)
    /// </summary>
    [Authorize(Roles = "Admin,HallManager")]
    [HttpGet("by-hall/{hallId:int}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceListDto>>>> GetInvoicesByHallId(int hallId)
    {
        try
        {
            var invoices = await _invoiceService.GetInvoicesByHallIdAsync(hallId);
            var invoiceDtos = _mapper.Map<IEnumerable<InvoiceListDto>>(invoices);
            return Success(invoiceDtos, "Invoices retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices for hall {HallId}", hallId);
            return Error<IEnumerable<InvoiceListDto>>($"Failed to retrieve invoices: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get invoices by payment status (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("by-status/{status}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceListDto>>>> GetInvoicesByStatus(string status)
    {
        try
        {
            var invoices = await _invoiceService.GetInvoicesByStatusAsync(status);
            var invoiceDtos = _mapper.Map<IEnumerable<InvoiceListDto>>(invoices);
            return Success(invoiceDtos, "Invoices retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices by status {Status}", status);
            return Error<IEnumerable<InvoiceListDto>>($"Failed to retrieve invoices: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get invoices by date range (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("by-date-range")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceListDto>>>> GetInvoicesByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var invoices = await _invoiceService.GetInvoicesByDateRangeAsync(startDate, endDate);
            var invoiceDtos = _mapper.Map<IEnumerable<InvoiceListDto>>(invoices);
            return Success(invoiceDtos, "Invoices retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices by date range");
            return Error<IEnumerable<InvoiceListDto>>($"Failed to retrieve invoices: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Generate invoice for a booking (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("generate/{bookingId:int}")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GenerateInvoice(int bookingId)
    {
        try
        {
            var userId = UserId;
            var invoice = await _invoiceService.GenerateInvoiceForBookingAsync(bookingId, userId.ToString());
            var invoiceDto = _mapper.Map<InvoiceDto>(invoice);
            return Success(invoiceDto, "Invoice generated successfully");
        }
        catch (ArgumentException ex)
        {
            return Error<InvoiceDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice for booking {BookingId}", bookingId);
            return Error<InvoiceDto>($"Failed to generate invoice: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update invoice payment status (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/payment-status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdatePaymentStatus(
        int id,
        [FromBody] UpdatePaymentStatusDto dto)
    {
        try
        {
            var result = await _invoiceService.UpdatePaymentStatusAsync(id, dto.PaymentStatus, dto.PaymentReference);
            if (!result)
            {
                return Error<bool>("Invoice not found", 404);
            }
            return Success(true, "Payment status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status for invoice {InvoiceId}", id);
            return Error<bool>($"Failed to update payment status: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Cancel invoice (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelInvoice(
        int id,
        [FromBody] CancelInvoiceDto dto)
    {
        try
        {
            var result = await _invoiceService.CancelInvoiceAsync(id, dto.Reason);
            if (!result)
            {
                return Error<bool>("Invoice not found", 404);
            }
            return Success(true, "Invoice cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling invoice {InvoiceId}", id);
            return Error<bool>($"Failed to cancel invoice: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Generate PDF for invoice
    /// </summary>
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(int id)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null || invoice.Id == 0)
            {
                return NotFound(new { message = "Invoice not found" });
            }

            // Authorization check
            var currentUserId = UserId;
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("HallManager");
            if (!isAdmin && invoice.CustomerId != currentUserId)
            {
                return Forbid();
            }

            var pdfBytes = await _invoiceService.GetInvoicePdfBytesAsync(id);
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return NotFound(new { message = "PDF not available" });
            }

            return File(pdfBytes, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PDF for invoice {InvoiceId}", id);
            return StatusCode(500, new { message = $"Failed to get PDF: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get invoice statistics (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<InvoiceStatistics>>> GetInvoiceStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var statistics = await _invoiceService.GetInvoiceStatisticsAsync(startDate, endDate);
            return Success(statistics, "Invoice statistics retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice statistics");
            return Error<InvoiceStatistics>($"Failed to retrieve statistics: {ex.Message}", 500);
        }
    }
}

/// <summary>
/// DTO for updating payment status
/// </summary>
public class UpdatePaymentStatusDto
{
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
}

/// <summary>
/// DTO for cancelling invoice
/// </summary>
public class CancelInvoiceDto
{
    public string Reason { get; set; } = string.Empty;
}
