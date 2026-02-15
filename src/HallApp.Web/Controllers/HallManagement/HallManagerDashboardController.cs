using HallApp.Application.DTOs.HallManager;
using HallApp.Application.Services;
using HallApp.Core.Exceptions;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.HallManagement;

/// <summary>
/// HallManager dashboard operations.
/// Provides aggregated statistics and data for the HallManager's assigned halls.
/// Follows vertical slice architecture - all HallManager dashboard concerns in one place.
/// </summary>
[Authorize(Roles = "HallOrganizationManager,HallManager,Admin")]
[Route("api/hall-manager")]
[ApiController]
public class HallManagerDashboardController : BaseApiController
{
    private readonly IHallManagerDashboardService _dashboardService;
    private readonly ILogger<HallManagerDashboardController> _logger;

    public HallManagerDashboardController(
        IHallManagerDashboardService dashboardService,
        ILogger<HallManagerDashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics and data for the current HallManager
    /// </summary>
    /// <remarks>
    /// Returns aggregated dashboard data including:
    /// - Statistics (total halls, bookings, revenue, pending approvals)
    /// - Revenue breakdown (this month, last month, yearly, monthly chart data)
    /// - Pending approval list (bookings awaiting manager's approval)
    /// - Recent bookings list
    ///
    /// All data is scoped to the authenticated manager's assigned halls only.
    ///
    /// Requires: HallManager or Admin role
    /// </remarks>
    /// <response code="200">Returns the dashboard data</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have the HallManager or Admin role</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<HallManagerDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<HallManagerDashboardDto>>> GetDashboard(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (UserId == 0)
            {
                _logger.LogWarning("GetDashboard called with invalid user ID from token");
                return Error<HallManagerDashboardDto>("User authentication failed", 401);
            }

            _logger.LogInformation(
                "HallManager {UserId} requesting dashboard data", UserId);

            var dashboard = await _dashboardService.GetDashboardDataAsync(
                UserId, cancellationToken);

            return Success(dashboard, "Dashboard data retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Dashboard request was cancelled for user {UserId}", UserId);
            return StatusCode(499, new ApiResponse<HallManagerDashboardDto>
            {
                StatusCode = 499,
                Message = "Request cancelled",
                IsSuccess = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving dashboard data for HallManager {UserId}", UserId);
            return Error<HallManagerDashboardDto>(
                "An error occurred while loading the dashboard. Please try again.", 500);
        }
    }
}
