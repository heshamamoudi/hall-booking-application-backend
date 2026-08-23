using AutoMapper;
using HallApp.Application.DTOs.Vendors;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.VendorManagement;

/// <summary>
/// VendorManager-specific booking operations.
///
/// The vendor mirror of HallManagerBookingsController. Before this existed the
/// vendor bookings screen called api/bookings/my-vendor-bookings, which was never
/// implemented - the request 404'd and the frontend rendered it as an empty list,
/// so the whole vendor approval flow was unreachable through the UI.
/// </summary>
[Authorize(Roles = "VendorOrganizationManager,VendorManager,Admin")]
[Route("api/bookings")]
[ApiController]
public class VendorManagerBookingsController : BaseApiController
{
    private readonly IVendorManagerBookingService _vendorManagerBookingService;
    private readonly IMapper _mapper;
    private readonly ILogger<VendorManagerBookingsController> _logger;

    public VendorManagerBookingsController(
        IVendorManagerBookingService vendorManagerBookingService,
        IMapper mapper,
        ILogger<VendorManagerBookingsController> logger)
    {
        _vendorManagerBookingService = vendorManagerBookingService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated bookings for the current VendorManager's assigned vendors
    /// </summary>
    /// <remarks>
    /// Returns vendor bookings only for vendors the authenticated manager is assigned
    /// to. Data isolation is enforced at the service layer.
    ///
    /// Requires: VendorOrganizationManager, VendorManager, or Admin role
    /// </remarks>
    /// <param name="pageNumber">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns the paginated list of bookings for the manager's vendors</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have the required role</response>
    [HttpGet("my-vendor-bookings")]
    [Authorize(Roles = "VendorOrganizationManager,VendorManager,Admin")]
    [ProducesResponseType(typeof(PaginatedApiResponse<VendorBookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedApiResponse<VendorBookingDto>>> GetMyVendorBookings(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? page = null,
        [FromQuery] int? size = null,
        CancellationToken cancellationToken = default)
    {
        (pageNumber, pageSize) = ResolvePaging(pageNumber, pageSize, page, size);

        return await RunPagedAsync(
            nameof(GetMyVendorBookings),
            pageNumber,
            pageSize,
            (page, size, ct) => _vendorManagerBookingService
                .GetBookingsForManagedVendorsPagedAsync(UserId, page, size, ct),
            cancellationToken);
    }

    /// <summary>
    /// Get paginated bookings for every vendor in the current VendorOrganizationManager's organization
    /// </summary>
    /// <remarks>
    /// Returns vendor bookings across ALL vendors belonging to the organization owned
    /// by the authenticated user, optionally filtered by vendor or status.
    ///
    /// Requires: VendorOrganizationManager or Admin role
    /// </remarks>
    /// <param name="vendorId">Optional vendor ID filter</param>
    /// <param name="status">Optional status filter (case-insensitive)</param>
    /// <param name="pageNumber">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns the paginated list of bookings for the organization's vendors</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have the VendorOrganizationManager or Admin role</response>
    [HttpGet("my-vendor-org-bookings")]
    [Authorize(Roles = "VendorOrganizationManager,Admin")]
    [ProducesResponseType(typeof(PaginatedApiResponse<VendorBookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedApiResponse<VendorBookingDto>>> GetMyVendorOrgBookings(
        [FromQuery] int? vendorId = null,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? page = null,
        [FromQuery] int? size = null,
        CancellationToken cancellationToken = default)
    {
        (pageNumber, pageSize) = ResolvePaging(pageNumber, pageSize, page, size);

        return await RunPagedAsync(
            nameof(GetMyVendorOrgBookings),
            pageNumber,
            pageSize,
            (page, size, ct) => _vendorManagerBookingService
                .GetBookingsForOrganizationPagedAsync(UserId, vendorId, status, page, size, ct),
            cancellationToken);
    }

    /// <summary>
    /// Get dashboard statistics for the current VendorManager's assigned vendors
    /// </summary>
    /// <remarks>Requires: VendorOrganizationManager, VendorManager, or Admin role</remarks>
    /// <response code="200">Returns the statistics</response>
    [HttpGet("my-vendor-stats")]
    [Authorize(Roles = "VendorOrganizationManager,VendorManager,Admin")]
    [ProducesResponseType(typeof(ApiResponse<BookingDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BookingDashboardStatsDto>>> GetMyVendorStats(
        CancellationToken cancellationToken = default)
    {
        return await RunStatsAsync(
            nameof(GetMyVendorStats),
            ct => _vendorManagerBookingService.GetVendorManagerDashboardStatsAsync(UserId, ct),
            cancellationToken);
    }

    /// <summary>
    /// Get dashboard statistics across every vendor in the organization
    /// </summary>
    /// <remarks>Requires: VendorOrganizationManager or Admin role</remarks>
    /// <response code="200">Returns the statistics</response>
    [HttpGet("my-vendor-org-stats")]
    [Authorize(Roles = "VendorOrganizationManager,Admin")]
    [ProducesResponseType(typeof(ApiResponse<BookingDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BookingDashboardStatsDto>>> GetMyVendorOrgStats(
        CancellationToken cancellationToken = default)
    {
        return await RunStatsAsync(
            nameof(GetMyVendorOrgStats),
            ct => _vendorManagerBookingService.GetVendorOrgManagerDashboardStatsAsync(UserId, ct),
            cancellationToken);
    }

    // ===================================================================
    // Shared plumbing
    //
    // The four actions differ only in which service call they make. Keeping the
    // auth guard, clamping, logging and error handling in one place stops the
    // four from drifting apart the way hand-copied handlers do.
    // ===================================================================

    private async Task<ActionResult<PaginatedApiResponse<VendorBookingDto>>> RunPagedAsync(
        string action,
        int pageNumber,
        int pageSize,
        Func<int, int, CancellationToken, Task<(IEnumerable<Core.Entities.VendorEntities.VendorBooking> Bookings, int TotalCount)>> query,
        CancellationToken cancellationToken)
    {
        try
        {
            if (UserId == 0)
            {
                _logger.LogWarning("{Action} called with invalid user ID from token", action);
                return StatusCode(401, new PaginatedApiResponse<VendorBookingDto>
                {
                    StatusCode = 401,
                    Message = "User authentication failed",
                    IsSuccess = false
                });
            }

            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var (bookings, totalCount) = await query(pageNumber, pageSize, cancellationToken);
            var dtos = _mapper.Map<List<VendorBookingDto>>(bookings);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            _logger.LogInformation(
                "{Action} returned {Count}/{Total} bookings (page {Page}) for user {UserId}",
                action, dtos.Count, totalCount, pageNumber, UserId);

            return Ok(new PaginatedApiResponse<VendorBookingDto>
            {
                StatusCode = 200,
                Message = $"Retrieved {dtos.Count} of {totalCount} bookings successfully",
                IsSuccess = true,
                Data = dtos,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("{Action} was cancelled for user {UserId}", action, UserId);
            return StatusCode(499, new PaginatedApiResponse<VendorBookingDto>
            {
                StatusCode = 499,
                Message = "Request cancelled",
                IsSuccess = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {Action} for user {UserId}", action, UserId);
            return StatusCode(500, new PaginatedApiResponse<VendorBookingDto>
            {
                StatusCode = 500,
                Message = "An error occurred while retrieving bookings. Please try again.",
                IsSuccess = false
            });
        }
    }

    private async Task<ActionResult<ApiResponse<BookingDashboardStatsDto>>> RunStatsAsync(
        string action,
        Func<CancellationToken, Task<BookingDashboardStatsDto>> query,
        CancellationToken cancellationToken)
    {
        try
        {
            if (UserId == 0)
            {
                return Error<BookingDashboardStatsDto>("User authentication failed", 401);
            }

            var stats = await query(cancellationToken);
            return Success(stats, "Statistics retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("{Action} was cancelled for user {UserId}", action, UserId);
            return Error<BookingDashboardStatsDto>("Request cancelled", 499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {Action} for user {UserId}", action, UserId);
            return Error<BookingDashboardStatsDto>(
                "An error occurred while retrieving statistics. Please try again.", 500);
        }
    }
}
