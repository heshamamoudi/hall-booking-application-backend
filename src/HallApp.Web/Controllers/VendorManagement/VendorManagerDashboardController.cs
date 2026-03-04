using HallApp.Application.DTOs.VendorManager;
using HallApp.Application.Services;
using HallApp.Core.Exceptions;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.VendorManagement;

/// <summary>
/// VendorManager dashboard operations.
/// Provides aggregated statistics and data for the VendorManager's assigned vendors.
/// </summary>
[Authorize(Roles = "VendorOrganizationManager,VendorManager,Admin")]
[Route("api/vendor-manager")]
[ApiController]
public class VendorManagerDashboardController : BaseApiController
{
    private readonly IVendorManagerDashboardService _dashboardService;
    private readonly ILogger<VendorManagerDashboardController> _logger;

    public VendorManagerDashboardController(
        IVendorManagerDashboardService dashboardService,
        ILogger<VendorManagerDashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics and data for the current VendorManager
    /// </summary>
    /// <remarks>
    /// Returns aggregated dashboard data including:
    /// - Statistics (total vendors, active vendors, bookings, pending approvals, revenue)
    /// - Revenue breakdown (this month, last month, yearly, monthly chart data)
    /// - Pending approval list (vendor bookings awaiting manager's approval)
    /// - Recent bookings list
    /// - Service status distribution (for donut chart)
    ///
    /// All data is scoped to the authenticated manager's assigned vendors only.
    ///
    /// Requires: VendorManager, VendorOrganizationManager, or Admin role
    /// </remarks>
    /// <response code="200">Returns the dashboard data</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have the required role</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<VendorManagerDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<VendorManagerDashboardDto>>> GetDashboard(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (UserId == 0)
            {
                _logger.LogWarning("GetDashboard called with invalid user ID from token");
                return Error<VendorManagerDashboardDto>("User authentication failed", 401);
            }

            _logger.LogInformation(
                "VendorManager {UserId} requesting dashboard data", UserId);

            var dashboard = await _dashboardService.GetDashboardDataAsync(
                UserId, cancellationToken);

            return Success(dashboard, "Dashboard data retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Dashboard request was cancelled for user {UserId}", UserId);
            return StatusCode(499, new ApiResponse<VendorManagerDashboardDto>
            {
                StatusCode = 499,
                Message = "Request cancelled",
                IsSuccess = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving dashboard data for VendorManager {UserId}", UserId);
            return Error<VendorManagerDashboardDto>(
                "An error occurred while loading the dashboard. Please try again.", 500);
        }
    }
}
