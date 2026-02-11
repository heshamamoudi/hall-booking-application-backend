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
[Authorize(Roles = "HallManager,Admin")]
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
    /// Requires: HallManager or Admin role
    /// </remarks>
    /// <param name="pageNumber">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns the paginated list of bookings for the manager's halls</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have the HallManager or Admin role</response>
    [HttpGet("my-hall-bookings")]
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
}
