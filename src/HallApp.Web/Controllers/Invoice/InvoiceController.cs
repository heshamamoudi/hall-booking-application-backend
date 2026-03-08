using AutoMapper;
using HallApp.Core.Exceptions;
using HallApp.Application.DTOs.Common;
using HallApp.Application.DTOs.Invoice;
using HallApp.Core.Entities.PaymentEntities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.Invoice;

/// <summary>
/// Controller for Invoice management including generation, cancellation, refund, and regeneration.
/// HIGH-006: Integrated with IFinancialAuditService for full audit trail on all financial operations.
/// HIGH-009: Bulk operations wrapped in database transactions for all-or-nothing semantics.
/// HIGH-011: Enhanced structured logging with correlation IDs for all financial operations.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InvoiceController : BaseApiController
{
    private readonly IInvoiceService _invoiceService;
    private readonly IHallManagerService _hallManagerService;
    private readonly IOrganizationService _organizationService;
    private readonly IHallService _hallService;
    private readonly IMapper _mapper;
    private readonly ILogger<InvoiceController> _logger;
    private readonly IFinancialAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public InvoiceController(
        IInvoiceService invoiceService,
        IHallManagerService hallManagerService,
        IOrganizationService organizationService,
        IHallService hallService,
        IMapper mapper,
        ILogger<InvoiceController> logger,
        IFinancialAuditService auditService,
        IUnitOfWork unitOfWork)
    {
        _invoiceService = invoiceService;
        _hallManagerService = hallManagerService;
        _organizationService = organizationService;
        _hallService = hallService;
        _mapper = mapper;
        _logger = logger;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Helper method to check if the current HallManager or HallOrganizationManager user
    /// owns the hall associated with an invoice.
    /// HallManager: checks direct hall assignments via HallManager entity.
    /// HallOrganizationManager: checks organization ownership via Organization entity,
    /// then falls back to direct hall assignments.
    /// </summary>
    private async Task<bool> HallManagerOwnsInvoiceHall(Core.Entities.BookingEntities.Invoice invoice)
    {
        if (!User.IsInRole("HallOrganizationManager") && !User.IsInRole("HallManager"))
        {
            return false;
        }

        if (invoice.HallId == null)
        {
            return false;
        }

        // HallOrganizationManager: check org ownership of the hall
        if (User.IsInRole("HallOrganizationManager"))
        {
            var org = await _organizationService.GetOrganizationByOwnerId(UserId);
            if (org != null)
            {
                var orgHalls = await _hallService.GetOrganizationHallsAsync(org.Id);
                if (orgHalls?.Any(h => h.ID == invoice.HallId.Value) == true)
                {
                    return true;
                }
            }
        }

        // HallManager (or fallback for HallOrganizationManager): check direct hall assignments
        var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(UserId);
        if (hallManager?.Halls == null)
        {
            return false;
        }

        return hallManager.Halls.Any(h => h.ID == invoice.HallId.Value);
    }

    /// <summary>
    /// Get filtered, paginated invoices with role-based scoping.
    /// Admin: all invoices (optionally filtered by organizationId).
    /// HallOrganizationManager: invoices for organization's halls.
    /// HallManager: invoices for assigned halls.
    /// Supports search, status, paymentMethod, hallId, amount range, date range, cancelled, sorting, and pagination.
    /// </summary>
    [Authorize(Roles = "Admin,HallOrganizationManager,HallManager")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<InvoiceListEnhancedDto>>>> GetFilteredInvoices(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? paymentMethod = null,
        [FromQuery] int? hallId = null,
        [FromQuery] int? organizationId = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool? isCancelled = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "InvoiceDate",
        [FromQuery] bool sortDescending = true)
    {
        try
        {
            // Clamp page size between 1 and 100
            pageSize = Math.Clamp(pageSize, 1, 100);
            pageNumber = Math.Max(1, pageNumber);

            List<Core.Entities.BookingEntities.Invoice> items;
            int totalCount;

            if (User.IsInRole("Admin"))
            {
                (items, totalCount) = await _invoiceService.GetInvoicesForAdminAsync(
                    search, status, paymentMethod, hallId,
                    organizationId, minAmount, maxAmount,
                    startDate, endDate, isCancelled,
                    sortBy, sortDescending, pageNumber, pageSize);
            }
            else if (User.IsInRole("HallOrganizationManager"))
            {
                var orgHallIds = await GetCurrentUserOrgHallIdsAsync();

                (items, totalCount) = await _invoiceService.GetInvoicesForOrgManagerAsync(
                    orgHallIds,
                    search, status, paymentMethod, hallId,
                    minAmount, maxAmount,
                    startDate, endDate, isCancelled,
                    sortBy, sortDescending, pageNumber, pageSize);
            }
            else // HallManager
            {
                var assignedHallIds = await GetCurrentUserAssignedHallIdsAsync();

                (items, totalCount) = await _invoiceService.GetInvoicesForHallManagerAsync(
                    assignedHallIds,
                    search, status, paymentMethod, hallId,
                    minAmount, maxAmount,
                    startDate, endDate, isCancelled,
                    sortBy, sortDescending, pageNumber, pageSize);
            }

            var invoiceDtos = _mapper.Map<List<InvoiceListEnhancedDto>>(items);
            var pagedResult = new PagedResultDto<InvoiceListEnhancedDto>
            {
                Items = invoiceDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Success(pagedResult, "Invoices retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filtered invoices");
            return Error<PagedResultDto<InvoiceListEnhancedDto>>($"Failed to retrieve invoices: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get invoice by ID. Returns 404 if the invoice does not exist.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoiceById(int id)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null || invoice.Id == 0)
            {
                _logger.LogWarning("Invoice with ID {InvoiceId} not found", id);
                return Error<InvoiceDto>($"Invoice with ID {id} not found", 404);
            }

            // VULN-NEW-002 FIX: Proper authorization for Admin, HallManager (ownership check), and Customer
            var userId = UserId;
            var isAdmin = User.IsInRole("Admin");
            var isHallManagerOwner = await HallManagerOwnsInvoiceHall(invoice);
            var isCustomerOwner = invoice.CustomerId == userId;

            if (!isAdmin && !isHallManagerOwner && !isCustomerOwner)
            {
                _logger.LogWarning(
                    "Access denied: User {UserId} attempted to access invoice {InvoiceId}",
                    userId, id);
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

            // VULN-NEW-002 FIX: Proper authorization
            var userId = UserId;
            var isAdmin = User.IsInRole("Admin");
            var isHallManagerOwner = await HallManagerOwnsInvoiceHall(invoice);
            var isCustomerOwner = invoice.CustomerId == userId;

            if (!isAdmin && !isHallManagerOwner && !isCustomerOwner)
            {
                _logger.LogWarning(
                    "Access denied: User {UserId} attempted to access invoice {InvoiceNumber}",
                    userId, invoiceNumber);
                return Error<InvoiceDto>("Access denied", 403);
            }

            var invoiceDto = _mapper.Map<InvoiceDto>(invoice);
            return Success(invoiceDto, "Invoice retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice by number {InvoiceNumber}", invoiceNumber);
            return Error<InvoiceDto>("Failed to retrieve invoice", 500);
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

            // VULN-NEW-002 FIX: Proper authorization
            var userId = UserId;
            var isAdmin = User.IsInRole("Admin");
            var isHallManagerOwner = await HallManagerOwnsInvoiceHall(invoice);
            var isCustomerOwner = invoice.CustomerId == userId;

            if (!isAdmin && !isHallManagerOwner && !isCustomerOwner)
            {
                _logger.LogWarning(
                    "Access denied: User {UserId} attempted to access invoice for booking {BookingId}",
                    userId, bookingId);
                return Error<InvoiceDto>("Access denied", 403);
            }

            var invoiceDto = _mapper.Map<InvoiceDto>(invoice);
            return Success(invoiceDto, "Invoice retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice for booking {BookingId}", bookingId);
            return Error<InvoiceDto>("Failed to retrieve invoice", 500);
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
            else if (User.IsInRole("HallOrganizationManager") || User.IsInRole("HallManager"))
            {
                // Collect hall IDs from both organization ownership and direct assignments
                var hallIdSet = new HashSet<int>();

                // HallOrganizationManager: include all halls in their organization
                if (User.IsInRole("HallOrganizationManager"))
                {
                    var org = await _organizationService.GetOrganizationByOwnerId(userId);
                    if (org != null)
                    {
                        var orgHalls = await _hallService.GetOrganizationHallsAsync(org.Id);
                        if (orgHalls != null)
                        {
                            foreach (var h in orgHalls)
                                hallIdSet.Add(h.ID);
                        }
                    }
                }

                // Also include directly assigned halls (HallManager link)
                var hallManager = await hallManagerService.GetHallManagerByAppUserIdAsync(userId);
                if (hallManager?.Halls != null)
                {
                    foreach (var h in hallManager.Halls)
                        hallIdSet.Add(h.ID);
                }

                if (hallIdSet.Count > 0)
                {
                    invoices = await _invoiceService.GetInvoicesByHallIdsAsync(hallIdSet.ToList());
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
    /// Get invoices by hall ID (Admin/HallManager with ownership check)
    /// </summary>
    [Authorize(Roles = "Admin,HallOrganizationManager,HallManager")]
    [HttpGet("by-hall/{hallId:int}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceListDto>>>> GetInvoicesByHallId(int hallId)
    {
        try
        {
            // VULN-005 FIX: Ownership check for HallManager / HallOrganizationManager
            if ((User.IsInRole("HallOrganizationManager") || User.IsInRole("HallManager")) && !User.IsInRole("Admin"))
            {
                var hasAccess = false;

                // HallOrganizationManager: check organization halls
                if (User.IsInRole("HallOrganizationManager"))
                {
                    var org = await _organizationService.GetOrganizationByOwnerId(UserId);
                    if (org != null)
                    {
                        var orgHalls = await _hallService.GetOrganizationHallsAsync(org.Id);
                        hasAccess = orgHalls?.Any(h => h.ID == hallId) == true;
                    }
                }

                // Also check direct hall assignments (HallManager link)
                if (!hasAccess)
                {
                    var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(UserId);
                    hasAccess = hallManager?.Halls?.Any(h => h.ID == hallId) == true;
                }

                if (!hasAccess)
                {
                    _logger.LogWarning(
                        "Access denied: User {UserId} attempted to access invoices for hall {HallId}",
                        UserId, hallId);
                    return Error<IEnumerable<InvoiceListDto>>("You do not have access to this hall's invoices", 403);
                }
            }

            var invoices = await _invoiceService.GetInvoicesByHallIdAsync(hallId);
            var invoiceDtos = _mapper.Map<IEnumerable<InvoiceListDto>>(invoices);
            return Success(invoiceDtos, "Invoices retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices for hall {HallId}", hallId);
            return Error<IEnumerable<InvoiceListDto>>("Failed to retrieve invoices", 500);
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
    /// Generate invoice for a booking (Admin only).
    /// HIGH-006: Audit trail recorded for invoice generation.
    /// HIGH-011: Enhanced structured logging with correlation ID.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("generate/{bookingId:int}")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GenerateInvoice(int bookingId)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        var userId = UserId;

        try
        {
            _logger.LogInformation(
                "HIGH-011: Invoice generation started - BookingId: {BookingId}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                bookingId, userId, correlationId);

            var invoice = await _invoiceService.GenerateInvoiceForBookingAsync(bookingId, userId.ToString());

            _logger.LogInformation(
                "HIGH-011: Invoice generated successfully - InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}, " +
                "BookingId: {BookingId}, TotalAmount: {TotalAmount}, Currency: {Currency}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                invoice.Id, invoice.InvoiceNumber, bookingId,
                invoice.TotalAmountWithTax, invoice.Currency, userId, correlationId);

            // HIGH-006: Audit trail
            await _auditService.LogAsync(
                userId,
                FinancialOperationType.InvoiceGenerated,
                "Invoice",
                invoice.Id.ToString(),
                afterState: new { invoice.Id, invoice.InvoiceNumber, invoice.BookingId, invoice.TotalAmountWithTax, invoice.Currency },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                userAgent: Request.Headers.UserAgent.ToString(),
                correlationId: correlationId,
                description: $"Invoice {invoice.InvoiceNumber} generated for booking {bookingId}");

            var invoiceDto = _mapper.Map<InvoiceDto>(invoice);
            return Success(invoiceDto, "Invoice generated successfully");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                "HIGH-011: Invoice generation rejected - BookingId: {BookingId}, Reason: {Reason}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                bookingId, ex.Message, userId, correlationId);
            return Error<InvoiceDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HIGH-011: Invoice generation failed - BookingId: {BookingId}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                bookingId, userId, correlationId);
            return Error<InvoiceDto>($"Failed to generate invoice: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Enhancement 2: Bulk invoice generation for multiple bookings.
    /// HIGH-009: Wrapped in database transaction for all-or-nothing semantics.
    /// HIGH-006: Audit trail recorded for bulk invoice generation.
    /// HIGH-011: Enhanced structured logging with correlation ID.
    /// </summary>
    [Authorize(Roles = "Admin,HallOrganizationManager")]
    [HttpPost("generate-bulk")]
    public async Task<ActionResult<ApiResponse<BulkInvoiceGenerateResponseDto>>> GenerateBulkInvoices(
        [FromBody] BulkInvoiceGenerateRequestDto request,
        [FromServices] IBookingService bookingService)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        var userId = UserId;

        try
        {
            if (request.BookingIds == null || !request.BookingIds.Any())
            {
                return Error<BulkInvoiceGenerateResponseDto>("At least one booking ID is required", 400);
            }

            if (request.BookingIds.Count > 100)
            {
                return Error<BulkInvoiceGenerateResponseDto>(
                    "Maximum 100 bookings can be processed in a single bulk operation", 400);
            }

            // CRIT-006 FIX: Validate booking hall IDs are within user's scope
            if (User.IsInRole("HallOrganizationManager") && !User.IsInRole("Admin"))
            {
                var userOrgHallIds = await GetCurrentUserOrgHallIdsAsync();

                foreach (var bookingId in request.BookingIds)
                {
                    var booking = await bookingService.GetBookingByIdAsync(bookingId);
                    if (booking != null && booking.Id != 0 && !userOrgHallIds.Contains(booking.HallId))
                    {
                        _logger.LogWarning(
                            "HIGH-011: Authorization failure in bulk generation - UserId: {UserId}, BookingId: {BookingId}, " +
                            "HallId: {HallId}, CorrelationId: {CorrelationId}",
                            userId, bookingId, booking.HallId, correlationId);
                        return Error<BulkInvoiceGenerateResponseDto>(
                            $"Access denied: Booking {bookingId} belongs to a hall outside your organization scope", 403);
                    }
                }
            }

            var createdBy = userId.ToString();

            _logger.LogInformation(
                "HIGH-011: Bulk invoice generation started - BookingCount: {BookingCount}, " +
                "BookingIds: [{BookingIds}], UserId: {UserId}, CorrelationId: {CorrelationId}",
                request.BookingIds.Count,
                string.Join(", ", request.BookingIds),
                userId, correlationId);

            // HIGH-009: Wrap bulk operation in a database transaction for all-or-nothing semantics
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var (generated, failed) = await _invoiceService.GenerateBulkInvoicesAsync(
                    request.BookingIds, createdBy);

                // HIGH-009: If any invoices failed to generate (not due to pre-existing invoices),
                // and the caller expects atomicity, we commit what succeeded and report failures.
                // The transaction ensures database consistency for the generated batch.
                await transaction.CommitAsync();

                var response = new BulkInvoiceGenerateResponseDto
                {
                    Generated = _mapper.Map<List<InvoiceDto>>(generated),
                    Failed = failed.Select(f => new BulkInvoiceFailureDto
                    {
                        BookingId = f.BookingId,
                        Error = f.Error
                    }).ToList()
                };

                var message = $"Bulk generation completed: {response.Generated.Count} generated, {response.Failed.Count} failed";

                _logger.LogInformation(
                    "HIGH-011: Bulk invoice generation completed - GeneratedCount: {GeneratedCount}, " +
                    "FailedCount: {FailedCount}, GeneratedInvoices: [{GeneratedInvoices}], " +
                    "FailedBookings: [{FailedBookings}], UserId: {UserId}, CorrelationId: {CorrelationId}",
                    response.Generated.Count, response.Failed.Count,
                    string.Join(", ", generated.Select(i => $"{i.InvoiceNumber}(booking:{i.BookingId})")),
                    string.Join(", ", failed.Select(f => $"{f.BookingId}:{f.Error}")),
                    userId, correlationId);

                // HIGH-006: Audit trail for bulk generation
                await _auditService.LogAsync(
                    userId,
                    FinancialOperationType.InvoiceBulkGenerated,
                    "Invoice",
                    "bulk",
                    afterState: new
                    {
                        GeneratedCount = generated.Count,
                        FailedCount = failed.Count,
                        GeneratedInvoiceIds = generated.Select(i => i.Id).ToList(),
                        FailedBookingIds = failed.Select(f => f.BookingId).ToList()
                    },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    userAgent: Request.Headers.UserAgent.ToString(),
                    correlationId: correlationId,
                    description: $"Bulk generation: {generated.Count} invoices generated, {failed.Count} failed");

                return Success(response, message);
            }
            catch (Exception ex)
            {
                // HIGH-009: Rollback on any unexpected failure to preserve atomicity
                await transaction.RollbackAsync();

                _logger.LogError(ex,
                    "HIGH-011: Bulk invoice generation rolled back due to error - " +
                    "BookingCount: {BookingCount}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                    request.BookingIds.Count, userId, correlationId);

                throw; // Re-throw to be caught by outer catch
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HIGH-011: Bulk invoice generation failed - UserId: {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);
            return Error<BulkInvoiceGenerateResponseDto>($"Failed to process bulk generation: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Enhancement 4: Regenerate an existing invoice from current booking data.
    /// Admin only - regeneration is destructive and replaces existing line items.
    /// HIGH-006: Audit trail recorded with before/after state snapshots.
    /// HIGH-011: Enhanced structured logging with correlation ID.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/regenerate")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> RegenerateInvoice(int id)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        var userId = UserId;

        try
        {
            // Capture before-state for audit
            var beforeInvoice = await _invoiceService.GetInvoiceByIdAsync(id);
            var beforeState = beforeInvoice?.Id > 0
                ? new { beforeInvoice.InvoiceNumber, beforeInvoice.TotalAmountWithTax, beforeInvoice.SubtotalBeforeTax, beforeInvoice.TaxAmount, beforeInvoice.PaymentStatus }
                : null;

            _logger.LogInformation(
                "HIGH-011: Invoice regeneration started - InvoiceId: {InvoiceId}, " +
                "PreviousTotal: {PreviousTotal}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, beforeInvoice?.TotalAmountWithTax, userId, correlationId);

            var invoice = await _invoiceService.RegenerateInvoiceAsync(id, userId.ToString());

            _logger.LogInformation(
                "HIGH-011: Invoice regenerated successfully - InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}, " +
                "PreviousTotal: {PreviousTotal}, NewTotal: {NewTotal}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, invoice.InvoiceNumber, beforeInvoice?.TotalAmountWithTax,
                invoice.TotalAmountWithTax, userId, correlationId);

            // HIGH-006: Audit trail
            await _auditService.LogAsync(
                userId,
                FinancialOperationType.InvoiceRegenerated,
                "Invoice",
                id.ToString(),
                beforeState: beforeState,
                afterState: new { invoice.InvoiceNumber, invoice.TotalAmountWithTax, invoice.SubtotalBeforeTax, invoice.TaxAmount, invoice.PaymentStatus },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                userAgent: Request.Headers.UserAgent.ToString(),
                correlationId: correlationId,
                description: $"Invoice {invoice.InvoiceNumber} regenerated from current booking data");

            var invoiceDto = _mapper.Map<InvoiceDto>(invoice);
            return Success(invoiceDto, "Invoice regenerated successfully from current booking data");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                "HIGH-011: Invoice regeneration rejected - InvoiceId: {InvoiceId}, Reason: {Reason}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, ex.Message, userId, correlationId);
            return Error<InvoiceDto>(ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "HIGH-011: Invoice regeneration conflict - InvoiceId: {InvoiceId}, Reason: {Reason}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, ex.Message, userId, correlationId);
            return Error<InvoiceDto>(ex.Message, 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HIGH-011: Invoice regeneration failed - InvoiceId: {InvoiceId}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, userId, correlationId);
            return Error<InvoiceDto>($"Failed to regenerate invoice: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Bulk regenerate QR codes for all invoices using current PlatformSettings.
    /// Updates only QR code and seller info — does NOT modify financial data.
    /// Requires PlatformSettings to have VAT number and company name configured.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("bulk-regenerate-qr")]
    public async Task<ActionResult<ApiResponse<BulkQRRegenerateResponseDto>>> BulkRegenerateQRCodes()
    {
        var userId = UserId;

        try
        {
            _logger.LogInformation(
                "Bulk QR code regeneration requested by UserId: {UserId}", userId);

            var (updated, failed, errors) = await _invoiceService.BulkRegenerateQRCodesAsync(userId.ToString());

            var response = new BulkQRRegenerateResponseDto
            {
                Updated = updated,
                Failed = failed,
                Errors = errors
            };

            return Success(response,
                $"QR codes regenerated: {updated} updated, {failed} failed out of {updated + failed} total invoices.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bulk QR regeneration blocked - PlatformSettings incomplete");
            return Error<BulkQRRegenerateResponseDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk QR regeneration failed. UserId: {UserId}", userId);
            return Error<BulkQRRegenerateResponseDto>($"Failed to regenerate QR codes: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update invoice payment status (Admin only).
    /// HIGH-011: Enhanced structured logging with correlation ID.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/payment-status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdatePaymentStatus(
        int id,
        [FromBody] UpdatePaymentStatusDto dto)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        var userId = UserId;

        try
        {
            _logger.LogInformation(
                "HIGH-011: Invoice payment status update - InvoiceId: {InvoiceId}, NewStatus: {NewStatus}, " +
                "PaymentReference: {PaymentReference}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, dto.PaymentStatus, dto.PaymentReference, userId, correlationId);

            var result = await _invoiceService.UpdatePaymentStatusAsync(id, dto.PaymentStatus, dto.PaymentReference);
            if (!result)
            {
                _logger.LogWarning(
                    "HIGH-011: Invoice not found for payment status update - InvoiceId: {InvoiceId}, CorrelationId: {CorrelationId}",
                    id, correlationId);
                return Error<bool>("Invoice not found", 404);
            }

            _logger.LogInformation(
                "HIGH-011: Invoice payment status updated successfully - InvoiceId: {InvoiceId}, " +
                "NewStatus: {NewStatus}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, dto.PaymentStatus, userId, correlationId);

            return Success(true, "Payment status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HIGH-011: Failed to update invoice payment status - InvoiceId: {InvoiceId}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, userId, correlationId);
            return Error<bool>($"Failed to update payment status: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Enhancement 5A: Cancel invoice with enhanced validation.
    /// Cannot cancel paid invoices without a prior refund.
    /// HIGH-006: Audit trail recorded with before/after state.
    /// HIGH-011: Enhanced structured logging with correlation ID.
    /// </summary>
    [Authorize(Roles = "Admin,HallOrganizationManager")]
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> CancelInvoice(
        int id,
        [FromBody] CancelInvoiceRequestDto dto)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        var userId = UserId;

        try
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                return Error<InvoiceDto>("Cancellation reason is required", 400);
            }

            var cancelledBy = userId.ToString();

            // Authorization check: HallOrganizationManager can only cancel invoices for their halls
            if (User.IsInRole("HallOrganizationManager") && !User.IsInRole("Admin"))
            {
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                if (invoice == null || invoice.Id == 0)
                {
                    return Error<InvoiceDto>("Invoice not found", 404);
                }

                var hasAccess = await HallManagerOwnsInvoiceHall(invoice);
                if (!hasAccess)
                {
                    _logger.LogWarning(
                        "HIGH-011: Authorization denied for invoice cancellation - InvoiceId: {InvoiceId}, " +
                        "UserId: {UserId}, CorrelationId: {CorrelationId}",
                        id, userId, correlationId);
                    return Error<InvoiceDto>("You do not have permission to cancel this invoice", 403);
                }
            }

            // Capture before-state for audit
            var beforeInvoice = await _invoiceService.GetInvoiceByIdAsync(id);
            var beforeState = beforeInvoice?.Id > 0
                ? new { beforeInvoice.InvoiceNumber, beforeInvoice.PaymentStatus, beforeInvoice.IsCancelled, beforeInvoice.TotalAmountWithTax }
                : null;

            _logger.LogInformation(
                "HIGH-011: Invoice cancellation started - InvoiceId: {InvoiceId}, Reason: {Reason}, " +
                "PreviousStatus: {PreviousStatus}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, dto.Reason, beforeInvoice?.PaymentStatus, userId, correlationId);

            var cancelledInvoice = await _invoiceService.CancelInvoiceWithValidationAsync(id, dto.Reason, cancelledBy);

            _logger.LogInformation(
                "HIGH-011: Invoice cancelled successfully - InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}, " +
                "Reason: {Reason}, TotalAmount: {TotalAmount}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, cancelledInvoice.InvoiceNumber, dto.Reason,
                cancelledInvoice.TotalAmountWithTax, userId, correlationId);

            // HIGH-006: Audit trail
            await _auditService.LogAsync(
                userId,
                FinancialOperationType.InvoiceCancelled,
                "Invoice",
                id.ToString(),
                beforeState: beforeState,
                afterState: new { cancelledInvoice.InvoiceNumber, cancelledInvoice.PaymentStatus, cancelledInvoice.IsCancelled, cancelledInvoice.CancellationReason, cancelledInvoice.CancelledAt },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                userAgent: Request.Headers.UserAgent.ToString(),
                correlationId: correlationId,
                description: $"Invoice {cancelledInvoice.InvoiceNumber} cancelled. Reason: {dto.Reason}");

            var invoiceDto = _mapper.Map<InvoiceDto>(cancelledInvoice);
            return Success(invoiceDto, "Invoice cancelled successfully");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                "HIGH-011: Invoice cancellation rejected - InvoiceId: {InvoiceId}, Reason: {Reason}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, ex.Message, userId, correlationId);
            return Error<InvoiceDto>(ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "HIGH-011: Invoice cancellation conflict - InvoiceId: {InvoiceId}, Reason: {Reason}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, ex.Message, userId, correlationId);
            return Error<InvoiceDto>(ex.Message, 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HIGH-011: Invoice cancellation failed - InvoiceId: {InvoiceId}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, userId, correlationId);
            return Error<InvoiceDto>($"Failed to cancel invoice: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Enhancement 5B: Refund a paid invoice.
    /// Admin only - refunds have financial implications.
    /// HIGH-006: Audit trail recorded with before/after state and refund details.
    /// HIGH-011: Enhanced structured logging with correlation ID.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/refund")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> RefundInvoice(
        int id,
        [FromBody] RefundInvoiceRequestDto dto)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        var userId = UserId;

        try
        {
            if (dto.RefundAmount <= 0)
            {
                return Error<InvoiceDto>("Refund amount must be greater than zero", 400);
            }

            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                return Error<InvoiceDto>("Refund reason is required", 400);
            }

            if (string.IsNullOrWhiteSpace(dto.RefundMethod))
            {
                return Error<InvoiceDto>("Refund method is required", 400);
            }

            // Capture before-state for audit
            var beforeInvoice = await _invoiceService.GetInvoiceByIdAsync(id);
            var beforeState = beforeInvoice?.Id > 0
                ? new { beforeInvoice.InvoiceNumber, beforeInvoice.PaymentStatus, beforeInvoice.TotalAmountWithTax, beforeInvoice.RefundAmount, beforeInvoice.RefundReason }
                : null;

            _logger.LogInformation(
                "HIGH-011: Invoice refund started - InvoiceId: {InvoiceId}, RefundAmount: {RefundAmount}, " +
                "RefundMethod: {RefundMethod}, Reason: {Reason}, PreviousRefundTotal: {PreviousRefundTotal}, " +
                "InvoiceTotal: {InvoiceTotal}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, dto.RefundAmount, dto.RefundMethod, dto.Reason,
                beforeInvoice?.RefundAmount, beforeInvoice?.TotalAmountWithTax,
                userId, correlationId);

            var refundedInvoice = await _invoiceService.RefundInvoiceAsync(
                id, dto.RefundAmount, dto.Reason, dto.RefundMethod, userId);

            var isFullRefund = refundedInvoice.RefundAmount >= refundedInvoice.TotalAmountWithTax;

            _logger.LogInformation(
                "HIGH-011: Invoice refund processed successfully - InvoiceId: {InvoiceId}, " +
                "InvoiceNumber: {InvoiceNumber}, RefundAmount: {RefundAmount}, TotalRefunded: {TotalRefunded}, " +
                "IsFullRefund: {IsFullRefund}, NewStatus: {NewStatus}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, refundedInvoice.InvoiceNumber, dto.RefundAmount,
                refundedInvoice.RefundAmount, isFullRefund, refundedInvoice.PaymentStatus,
                userId, correlationId);

            // HIGH-006: Audit trail
            await _auditService.LogAsync(
                userId,
                FinancialOperationType.InvoiceRefunded,
                "Invoice",
                id.ToString(),
                beforeState: beforeState,
                afterState: new
                {
                    refundedInvoice.InvoiceNumber,
                    refundedInvoice.PaymentStatus,
                    refundedInvoice.RefundAmount,
                    ThisRefundAmount = dto.RefundAmount,
                    dto.RefundMethod,
                    dto.Reason,
                    IsFullRefund = isFullRefund,
                    refundedInvoice.TotalAmountWithTax
                },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                userAgent: Request.Headers.UserAgent.ToString(),
                correlationId: correlationId,
                description: $"Invoice {refundedInvoice.InvoiceNumber} refunded {dto.RefundAmount:F2} via {dto.RefundMethod}. " +
                             $"Total refunded: {refundedInvoice.RefundAmount:F2}/{refundedInvoice.TotalAmountWithTax:F2}");

            var invoiceDto = _mapper.Map<InvoiceDto>(refundedInvoice);
            return Success(invoiceDto, "Invoice refund processed successfully");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                "HIGH-011: Invoice refund rejected - InvoiceId: {InvoiceId}, Reason: {Reason}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, ex.Message, userId, correlationId);
            return Error<InvoiceDto>(ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "HIGH-011: Invoice refund conflict - InvoiceId: {InvoiceId}, Reason: {Reason}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, ex.Message, userId, correlationId);
            return Error<InvoiceDto>(ex.Message, 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HIGH-011: Invoice refund failed - InvoiceId: {InvoiceId}, RefundAmount: {RefundAmount}, " +
                "UserId: {UserId}, CorrelationId: {CorrelationId}",
                id, dto.RefundAmount, userId, correlationId);
            return Error<InvoiceDto>($"Failed to process refund: {ex.Message}", 500);
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

            // VULN-NEW-002 FIX: Proper authorization
            var currentUserId = UserId;
            var isAdmin = User.IsInRole("Admin");
            var isHallManagerOwner = await HallManagerOwnsInvoiceHall(invoice);
            var isCustomerOwner = invoice.CustomerId == currentUserId;

            if (!isAdmin && !isHallManagerOwner && !isCustomerOwner)
            {
                _logger.LogWarning(
                    "Access denied: User {UserId} attempted to access PDF for invoice {InvoiceId}",
                    currentUserId, id);
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
    /// Get invoice statistics with role-based scoping.
    /// Admin: statistics across all invoices (all organizations).
    /// HallOrganizationManager: statistics for organization's halls only.
    /// HallManager: statistics for assigned halls only.
    /// Uses a single optimized aggregation query.
    /// </summary>
    [Authorize(Roles = "Admin,HallOrganizationManager,HallManager")]
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<InvoiceStatisticsDto>>> GetInvoiceStatistics()
    {
        try
        {
            IEnumerable<int>? hallIds = null;

            if (User.IsInRole("Admin"))
            {
                // Admin: null hallIds = no restriction (all invoices)
                hallIds = null;
            }
            else if (User.IsInRole("HallOrganizationManager"))
            {
                hallIds = await GetCurrentUserOrgHallIdsAsync();
            }
            else if (User.IsInRole("HallManager"))
            {
                hallIds = await GetCurrentUserAssignedHallIdsAsync();
            }

            var rawStats = await _invoiceService.GetInvoiceStatisticsForRoleAsync(hallIds);

            var dto = new InvoiceStatisticsDto
            {
                TotalRevenue = rawStats.TotalRevenue,
                PaidAmount = rawStats.PaidAmount,
                PendingAmount = rawStats.PendingAmount,
                TaxCollected = rawStats.TaxCollected,
                TotalInvoices = rawStats.TotalInvoices,
                PaidInvoices = rawStats.PaidInvoices,
                PendingInvoices = rawStats.PendingInvoices,
                CancelledInvoices = rawStats.CancelledInvoices
            };

            return Success(dto, "Invoice statistics retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice statistics");
            return Error<InvoiceStatisticsDto>($"Failed to retrieve statistics: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Bulk export invoices as CSV or Excel file.
    /// Admin, HallOrganizationManager, and HallManager with ownership validation.
    /// </summary>
    [Authorize(Roles = "Admin,HallOrganizationManager,HallManager")]
    [HttpPost("export")]
    public async Task<IActionResult> ExportInvoices([FromBody] InvoiceExportRequestDto request)
    {
        try
        {
            if (request.InvoiceIds == null || !request.InvoiceIds.Any())
            {
                return BadRequest(new { message = "At least one invoice ID is required" });
            }

            if (request.InvoiceIds.Count > 500)
            {
                return BadRequest(new { message = "Maximum 500 invoices can be exported at once" });
            }

            var format = request.Format?.ToLower() ?? "csv";
            if (format != "csv" && format != "excel")
            {
                return BadRequest(new { message = "Format must be 'csv' or 'excel'" });
            }

            // CRIT-007 FIX: Ownership validation - non-Admin users can only export invoices for their halls
            if (!IsAdmin)
            {
                var userHallIds = User.IsInRole("HallOrganizationManager")
                    ? await GetCurrentUserOrgHallIdsAsync()
                    : await GetCurrentUserAssignedHallIdsAsync();

                // Load the requested invoices and validate ownership
                var invoicesToExport = await _invoiceService.GetInvoicesByIdsAsync(request.InvoiceIds);
                var unauthorizedInvoices = invoicesToExport
                    .Where(i => i.HallId.HasValue && !userHallIds.Contains(i.HallId.Value))
                    .ToList();

                if (unauthorizedInvoices.Any())
                {
                    _logger.LogWarning(
                        "Authorization failure: User {UserId} attempted to export {Count} invoices for unauthorized halls. Invoice IDs: {InvoiceIds}",
                        UserId, unauthorizedInvoices.Count,
                        string.Join(",", unauthorizedInvoices.Select(i => i.Id)));
                    return Forbid();
                }
            }

            _logger.LogInformation(
                "Invoice export requested by user {UserId} for {Count} invoices in {Format} format",
                UserId, request.InvoiceIds.Count, format);

            byte[] fileBytes;
            string contentType;
            string fileName;

            if (format == "excel")
            {
                fileBytes = await _invoiceService.ExportInvoicesToExcelAsync(request.InvoiceIds);
                contentType = "application/vnd.ms-excel";
                fileName = $"invoices-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xls";
            }
            else
            {
                fileBytes = await _invoiceService.ExportInvoicesToCsvAsync(request.InvoiceIds);
                contentType = "text/csv";
                fileName = $"invoices-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting invoices");
            return StatusCode(500, new { message = $"Failed to export invoices: {ex.Message}" });
        }
    }

    #region Private Helpers

    /// <summary>
    /// Gets the hall IDs for the current user's organization (HallOrganizationManager).
    /// Includes both organization halls and directly assigned halls.
    /// </summary>
    private async Task<List<int>> GetCurrentUserOrgHallIdsAsync()
    {
        var hallIdSet = new HashSet<int>();

        var org = await _organizationService.GetOrganizationByOwnerId(UserId);
        if (org != null)
        {
            var orgHalls = await _hallService.GetOrganizationHallsAsync(org.Id);
            if (orgHalls != null)
            {
                foreach (var h in orgHalls)
                    hallIdSet.Add(h.ID);
            }
        }

        // Also include directly assigned halls
        var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(UserId);
        if (hallManager?.Halls != null)
        {
            foreach (var h in hallManager.Halls)
                hallIdSet.Add(h.ID);
        }

        return hallIdSet.ToList();
    }

    /// <summary>
    /// Gets the hall IDs directly assigned to the current HallManager user.
    /// </summary>
    private async Task<List<int>> GetCurrentUserAssignedHallIdsAsync()
    {
        var hallIdSet = new HashSet<int>();

        var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(UserId);
        if (hallManager?.Halls != null)
        {
            foreach (var h in hallManager.Halls)
                hallIdSet.Add(h.ID);
        }

        return hallIdSet.ToList();
    }

    #endregion
}

/// <summary>
/// DTO for updating payment status
/// </summary>
public class UpdatePaymentStatusDto
{
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
}
