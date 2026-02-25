using AutoMapper;
using HallApp.Application.DTOs.Booking;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.HallManagement;

/// <summary>
/// HallManager-specific booking operations.
/// Follows vertical slice architecture - all HallManager booking concerns in one place.
/// Separated from BookingController to enforce Single Responsibility Principle.
/// </summary>
[Authorize(Roles = "HallOrganizationManager,HallManager,Admin")]
[Route("api/bookings")]
[ApiController]
public class HallManagerBookingsController : BaseApiController
{
    private readonly IHallManagerBookingService _hallManagerBookingService;
    private readonly IMapper _mapper;
    private readonly ILogger<HallManagerBookingsController> _logger;

    public HallManagerBookingsController(
        IHallManagerBookingService hallManagerBookingService,
        IMapper mapper,
        ILogger<HallManagerBookingsController> logger)
    {
        _hallManagerBookingService = hallManagerBookingService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated bookings for the current HallManager's assigned halls
    /// </summary>
    /// <remarks>
    /// Returns bookings only for halls that the authenticated HallManager is assigned to.
    /// Data isolation is enforced at the service layer - a manager can only see bookings
    /// for their own halls.
    ///
    /// Supports pagination via pageNumber and pageSize query parameters.
    ///
    /// Requires: HallOrganizationManager, HallManager, or Admin role
    /// </remarks>
    /// <param name="pageNumber">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns the paginated list of bookings for the manager's halls</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have the required role</response>
    [HttpGet("my-hall-bookings")]
    [Authorize(Roles = "HallOrganizationManager,HallManager,Admin")]
    [ProducesResponseType(typeof(PaginatedApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedApiResponse<BookingDto>>> GetMyHallBookings(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (UserId == 0)
            {
                _logger.LogWarning("GetMyHallBookings called with invalid user ID from token");
                return StatusCode(401, new PaginatedApiResponse<BookingDto>
                {
                    StatusCode = 401,
                    Message = "User authentication failed",
                    IsSuccess = false
                });
            }

            // Clamp pagination values
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 50);

            _logger.LogInformation(
                "HallManager {UserId} requesting hall bookings (page {Page}, size {Size})",
                UserId, pageNumber, pageSize);

            var (bookings, totalCount) = await _hallManagerBookingService.GetBookingsForManagedHallsPagedAsync(
                UserId, pageNumber, pageSize, cancellationToken);

            var bookingDtos = _mapper.Map<List<BookingDto>>(bookings);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            _logger.LogInformation(
                "Returned {Count}/{Total} bookings (page {Page}) for HallManager {UserId}",
                bookingDtos.Count, totalCount, pageNumber, UserId);

            return Ok(new PaginatedApiResponse<BookingDto>
            {
                StatusCode = 200,
                Message = $"Retrieved {bookingDtos.Count} of {totalCount} bookings successfully",
                IsSuccess = true,
                Data = bookingDtos,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetMyHallBookings request was cancelled for user {UserId}", UserId);
            return StatusCode(499, new PaginatedApiResponse<BookingDto>
            {
                StatusCode = 499,
                Message = "Request cancelled",
                IsSuccess = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving bookings for HallManager {UserId}", UserId);
            return StatusCode(500, new PaginatedApiResponse<BookingDto>
            {
                StatusCode = 500,
                Message = "An error occurred while retrieving bookings. Please try again.",
                IsSuccess = false
            });
        }
    }

    /// <summary>
    /// Get paginated bookings for all halls in the current HallOrganizationManager's organization
    /// </summary>
    /// <remarks>
    /// Returns bookings for ALL halls belonging to the organization owned by the authenticated user.
    /// Supports optional filtering by hallId and city.
    /// Data isolation is enforced at the service layer - only the organization owner's halls are queried.
    ///
    /// Requires: HallOrganizationManager or Admin role
    /// </remarks>
    /// <param name="hallId">Optional hall ID filter</param>
    /// <param name="city">Optional city filter (case-insensitive)</param>
    /// <param name="pageNumber">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns the paginated list of bookings for the organization's halls</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have the HallOrganizationManager or Admin role</response>
    [HttpGet("my-org-bookings")]
    [Authorize(Roles = "HallOrganizationManager,Admin")]
    [ProducesResponseType(typeof(PaginatedApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedApiResponse<BookingDto>>> GetMyOrgBookings(
        [FromQuery] int? hallId = null,
        [FromQuery] string? city = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (UserId == 0)
            {
                _logger.LogWarning("GetMyOrgBookings called with invalid user ID from token");
                return StatusCode(401, new PaginatedApiResponse<BookingDto>
                {
                    StatusCode = 401,
                    Message = "User authentication failed",
                    IsSuccess = false
                });
            }

            // Clamp pagination values
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 50);

            _logger.LogInformation(
                "HallOrganizationManager {UserId} requesting org bookings (page {Page}, size {Size}, hallId {HallId}, city {City})",
                UserId, pageNumber, pageSize, hallId, city);

            var (bookings, totalCount) = await _hallManagerBookingService.GetBookingsForOrganizationPagedAsync(
                UserId, hallId, city, pageNumber, pageSize, cancellationToken);

            var bookingDtos = _mapper.Map<List<BookingDto>>(bookings);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            _logger.LogInformation(
                "Returned {Count}/{Total} org bookings (page {Page}) for HallOrganizationManager {UserId}",
                bookingDtos.Count, totalCount, pageNumber, UserId);

            return Ok(new PaginatedApiResponse<BookingDto>
            {
                StatusCode = 200,
                Message = $"Retrieved {bookingDtos.Count} of {totalCount} bookings successfully",
                IsSuccess = true,
                Data = bookingDtos,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetMyOrgBookings request was cancelled for user {UserId}", UserId);
            return StatusCode(499, new PaginatedApiResponse<BookingDto>
            {
                StatusCode = 499,
                Message = "Request cancelled",
                IsSuccess = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving org bookings for HallOrganizationManager {UserId}", UserId);
            return StatusCode(500, new PaginatedApiResponse<BookingDto>
            {
                StatusCode = 500,
                Message = "An error occurred while retrieving bookings. Please try again.",
                IsSuccess = false
            });
        }
    }

    /// <summary>
    /// Returns dashboard statistics for the current HallManager.
    /// Counts are future-only (EventDate >= today). Revenue is all-time.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns the dashboard statistics for the manager's halls</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have the required role</response>
    [HttpGet("my-hall-stats")]
    [Authorize(Roles = "HallOrganizationManager,HallManager,Admin")]
    [ProducesResponseType(typeof(ApiResponse<BookingDashboardStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<BookingDashboardStatsDto>>> GetMyHallStats(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (UserId == 0)
            {
                _logger.LogWarning("GetMyHallStats called with invalid user ID from token");
                return StatusCode(401, new ApiResponse<BookingDashboardStatsDto>
                {
                    StatusCode = 401,
                    Message = "User authentication failed",
                    IsSuccess = false
                });
            }

            _logger.LogInformation(
                "HallManager {UserId} requesting dashboard stats", UserId);

            var stats = await _hallManagerBookingService.GetHallManagerDashboardStatsAsync(
                UserId, cancellationToken);

            _logger.LogInformation(
                "Returned dashboard stats for HallManager {UserId}", UserId);

            return Ok(new ApiResponse<BookingDashboardStatsDto>
            {
                StatusCode = 200,
                Message = "Dashboard statistics retrieved successfully",
                IsSuccess = true,
                Data = stats
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetMyHallStats request was cancelled for user {UserId}", UserId);
            return StatusCode(499, new ApiResponse<BookingDashboardStatsDto>
            {
                StatusCode = 499,
                Message = "Request cancelled",
                IsSuccess = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving dashboard stats for HallManager {UserId}", UserId);
            return StatusCode(500, new ApiResponse<BookingDashboardStatsDto>
            {
                StatusCode = 500,
                Message = "An error occurred while retrieving dashboard statistics. Please try again.",
                IsSuccess = false
            });
        }
    }

    /// <summary>
    /// Returns dashboard statistics for the current HallOrganizationManager.
    /// Counts are future-only (EventDate >= today). Revenue is all-time.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns the dashboard statistics for the organization's halls</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have the HallOrganizationManager or Admin role</response>
    [HttpGet("my-org-stats")]
    [Authorize(Roles = "HallOrganizationManager,Admin")]
    [ProducesResponseType(typeof(ApiResponse<BookingDashboardStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<BookingDashboardStatsDto>>> GetMyOrgStats(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (UserId == 0)
            {
                _logger.LogWarning("GetMyOrgStats called with invalid user ID from token");
                return StatusCode(401, new ApiResponse<BookingDashboardStatsDto>
                {
                    StatusCode = 401,
                    Message = "User authentication failed",
                    IsSuccess = false
                });
            }

            _logger.LogInformation(
                "HallOrganizationManager {UserId} requesting dashboard stats", UserId);

            var stats = await _hallManagerBookingService.GetOrgManagerDashboardStatsAsync(
                UserId, cancellationToken);

            _logger.LogInformation(
                "Returned dashboard stats for HallOrganizationManager {UserId}", UserId);

            return Ok(new ApiResponse<BookingDashboardStatsDto>
            {
                StatusCode = 200,
                Message = "Dashboard statistics retrieved successfully",
                IsSuccess = true,
                Data = stats
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetMyOrgStats request was cancelled for user {UserId}", UserId);
            return StatusCode(499, new ApiResponse<BookingDashboardStatsDto>
            {
                StatusCode = 499,
                Message = "Request cancelled",
                IsSuccess = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving dashboard stats for HallOrganizationManager {UserId}", UserId);
            return StatusCode(500, new ApiResponse<BookingDashboardStatsDto>
            {
                StatusCode = 500,
                Message = "An error occurred while retrieving dashboard statistics. Please try again.",
                IsSuccess = false
            });
        }
    }
}
