using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using HallApp.Application.DTOs.Halls.Hall;
using HallApp.Application.Services;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using System.Security.Claims;

namespace HallApp.Web.Controllers.Hall;

/// <summary>
/// Hall statistics and analytics endpoints.
/// Provides per-hall aggregated data for the enhanced dashboard UI.
/// Requires HallManager or Admin role. HallManagers can only access stats for their own halls.
/// </summary>
[Authorize(Roles = "Admin,HallOrganizationManager,HallManager")]
[Route("api/halls")]
[ApiController]
public class HallStatsController : BaseApiController
{
    private readonly IHallStatsService _hallStatsService;
    private readonly IHallQueryService _hallQueryService;
    private readonly IOrganizationService _organizationService;
    private readonly IMapper _mapper;
    private readonly ILogger<HallStatsController> _logger;

    public HallStatsController(
        IHallStatsService hallStatsService,
        IHallQueryService hallQueryService,
        IOrganizationService organizationService,
        IMapper mapper,
        ILogger<HallStatsController> logger)
    {
        _hallStatsService = hallStatsService;
        _hallQueryService = hallQueryService;
        _organizationService = organizationService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Verifies the current user has access to the specified hall.
    /// Admins have access to all halls. HallManagers only have access to halls they manage.
    /// </summary>
    // AUTH-FIX: Also checks organization ownership for HallOrganizationManager users,
    // who access halls through Organization -> Hall (not through HallManager join table).
    private async Task<bool> UserOwnsHall(int hallId)
    {
        if (User.IsInRole("Admin")) return true;

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return false;

        // Check 1: HallManager team members -- halls linked via HallManager join table
        var managerHalls = await _hallQueryService.GetHallsByManagerAsync(userId);
        if (managerHalls.Any(h => h.ID == hallId))
            return true;

        // Check 2: HallOrganizationManager owners -- halls linked via Organization
        // Organization owners do NOT have HallManager entities; they access halls
        // through their Organization -> Hall relationship.
        if (User.IsInRole("HallOrganizationManager"))
        {
            var userIdInt = int.Parse(userId);
            var organization = await _organizationService.GetOrganizationByOwnerId(userIdInt);
            if (organization != null)
            {
                var orgHalls = await _hallQueryService.GetOrganizationHallsAsync(organization.Id);
                if (orgHalls.Any(h => h.ID == hallId))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get aggregated statistics for a specific hall
    /// </summary>
    /// <remarks>
    /// Returns booking counts, revenue, invoice counts, and review statistics.
    ///
    /// Response includes:
    /// - totalBookings, completedBookings, cancelledBookings, pendingBookings, activeBookings
    /// - totalRevenue, paidInvoices, pendingInvoices
    /// - averageRating, totalReviews
    ///
    /// Authorization: Admin can view any hall. HallManagers can only view halls they manage.
    /// </remarks>
    /// <param name="id">The hall ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns the hall statistics</response>
    /// <response code="403">User does not have access to this hall</response>
    /// <response code="404">Hall not found</response>
    [HttpGet("{id:int}/stats")]
    [ProducesResponseType(typeof(ApiResponse<HallStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<HallStatsDto>>> GetHallStats(
        int id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify hall exists
            var hall = await _hallQueryService.GetHallByIdAsync(id);
            if (hall == null)
            {
                return Error<HallStatsDto>("Hall not found", 404);
            }

            // Verify ownership
            if (!await UserOwnsHall(id))
            {
                return Error<HallStatsDto>("You do not have permission to view statistics for this hall", 403);
            }

            var stats = await _hallStatsService.GetHallStatsAsync(id, cancellationToken);
            return Success(stats, "Hall statistics retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Stats request cancelled for Hall {HallId}", id);
            return StatusCode(499, new ApiResponse<HallStatsDto>
            {
                StatusCode = 499,
                Message = "Request cancelled",
                IsSuccess = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for Hall {HallId}", id);
            return Error<HallStatsDto>("An error occurred while loading hall statistics. Please try again.", 500);
        }
    }

    /// <summary>
    /// Get recent bookings for a specific hall
    /// </summary>
    /// <remarks>
    /// Returns a list of the most recent bookings for the hall, ordered by creation date descending.
    /// Customer names are included but no other PII is exposed.
    ///
    /// Authorization: Admin can view any hall. HallManagers can only view halls they manage.
    /// </remarks>
    /// <param name="id">The hall ID</param>
    /// <param name="limit">Maximum number of bookings to return (default: 5, max: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns the recent bookings list</response>
    /// <response code="403">User does not have access to this hall</response>
    /// <response code="404">Hall not found</response>
    [HttpGet("{id:int}/bookings/recent")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BookingSimpleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IEnumerable<BookingSimpleDto>>>> GetHallRecentBookings(
        int id, [FromQuery] int limit = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            // Clamp limit to prevent excessive queries
            limit = Math.Clamp(limit, 1, 20);

            // Verify hall exists
            var hall = await _hallQueryService.GetHallByIdAsync(id);
            if (hall == null)
            {
                return Error<IEnumerable<BookingSimpleDto>>("Hall not found", 404);
            }

            // Verify ownership
            if (!await UserOwnsHall(id))
            {
                return Error<IEnumerable<BookingSimpleDto>>(
                    "You do not have permission to view bookings for this hall", 403);
            }

            var bookings = await _hallStatsService.GetHallRecentBookingsAsync(id, limit, cancellationToken);
            return Success<IEnumerable<BookingSimpleDto>>(
                bookings, $"Retrieved {bookings.Count()} recent bookings for hall");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Recent bookings request cancelled for Hall {HallId}", id);
            return StatusCode(499, new ApiResponse<IEnumerable<BookingSimpleDto>>
            {
                StatusCode = 499,
                Message = "Request cancelled",
                IsSuccess = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent bookings for Hall {HallId}", id);
            return Error<IEnumerable<BookingSimpleDto>>(
                "An error occurred while loading recent bookings. Please try again.", 500);
        }
    }

    /// <summary>
    /// Get monthly revenue data for a specific hall
    /// </summary>
    /// <remarks>
    /// Returns revenue and booking count per month for the specified number of months.
    /// Always includes all months in the range, even months with zero revenue.
    /// Month format is YYYY-MM (e.g., "2026-01").
    ///
    /// Authorization: Admin can view any hall. HallManagers can only view halls they manage.
    /// </remarks>
    /// <param name="id">The hall ID</param>
    /// <param name="months">Number of months to include (default: 6, max: 24)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns the monthly revenue data</response>
    /// <response code="403">User does not have access to this hall</response>
    /// <response code="404">Hall not found</response>
    [HttpGet("{id:int}/revenue/monthly")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RevenueByMonthDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IEnumerable<RevenueByMonthDto>>>> GetHallMonthlyRevenue(
        int id, [FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        try
        {
            // Clamp months to prevent excessive queries
            months = Math.Clamp(months, 1, 24);

            // Verify hall exists
            var hall = await _hallQueryService.GetHallByIdAsync(id);
            if (hall == null)
            {
                return Error<IEnumerable<RevenueByMonthDto>>("Hall not found", 404);
            }

            // Verify ownership
            if (!await UserOwnsHall(id))
            {
                return Error<IEnumerable<RevenueByMonthDto>>(
                    "You do not have permission to view revenue data for this hall", 403);
            }

            var revenueData = await _hallStatsService.GetHallMonthlyRevenueAsync(id, months, cancellationToken);
            return Success<IEnumerable<RevenueByMonthDto>>(
                revenueData, $"Retrieved {months} months of revenue data for hall");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Monthly revenue request cancelled for Hall {HallId}", id);
            return StatusCode(499, new ApiResponse<IEnumerable<RevenueByMonthDto>>
            {
                StatusCode = 499,
                Message = "Request cancelled",
                IsSuccess = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monthly revenue for Hall {HallId}", id);
            return Error<IEnumerable<RevenueByMonthDto>>(
                "An error occurred while loading revenue data. Please try again.", 500);
        }
    }

    /// <summary>
    /// Get the current hall manager's halls with basic statistics included.
    /// </summary>
    /// <remarks>
    /// Returns the same hall data as GET /api/halls/my-halls but enriched with:
    /// - totalBookings: total number of bookings for the hall
    /// - totalRevenue: sum of paid invoices
    /// - paidInvoices: count of paid invoices
    /// - pendingInvoices: count of pending invoices
    /// - totalReviews: count of reviews
    ///
    /// This is an authenticated endpoint specifically for the hall management dashboard.
    /// </remarks>
    /// <response code="200">Returns halls with statistics</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet("my-halls/with-stats")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<HallWithStatsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<HallWithStatsDto>>>> GetMyHallsWithStats(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Error<IEnumerable<HallWithStatsDto>>("User not authenticated", 401);
            }

            // Get manager's halls
            var halls = await _hallQueryService.GetHallsByManagerAsync(userId);
            if (!halls.Any())
            {
                return Success<IEnumerable<HallWithStatsDto>>(
                    [], "No halls found for manager");
            }

            // Map to HallWithStatsDto
            var hallDtos = _mapper.Map<List<HallWithStatsDto>>(halls);

            // Batch-load stats for all halls
            var hallIds = halls.Select(h => h.ID).ToList();
            var statsMap = await _hallStatsService.GetStatsForHallsAsync(hallIds, cancellationToken);

            // Enrich DTOs with stats
            foreach (var dto in hallDtos)
            {
                if (statsMap.TryGetValue(dto.ID, out var stats))
                {
                    dto.TotalBookings = stats.TotalBookings;
                    dto.TotalRevenue = stats.TotalRevenue;
                    dto.PaidInvoices = stats.PaidInvoices;
                    dto.PendingInvoices = stats.PendingInvoices;
                    dto.TotalReviews = stats.TotalReviews;
                }
            }

            return Success<IEnumerable<HallWithStatsDto>>(
                hallDtos, $"Found {hallDtos.Count} halls with statistics for manager");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("My halls with stats request cancelled for user {UserId}", UserId);
            return StatusCode(499, new ApiResponse<IEnumerable<HallWithStatsDto>>
            {
                StatusCode = 499,
                Message = "Request cancelled",
                IsSuccess = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting halls with stats for user {UserId}", UserId);
            return Error<IEnumerable<HallWithStatsDto>>(
                "An error occurred processing your request. Please try again.", 500);
        }
    }
}
