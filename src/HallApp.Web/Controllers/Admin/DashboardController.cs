using HallApp.Application.Common.Models;
using HallApp.Application.DTOs.Dashboard;
using HallApp.Application.Services;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("api/admin/dashboard")]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _dashboardService;
    private readonly IDashboardExportService _exportService;

    public DashboardController(IDashboardService dashboardService, IDashboardExportService exportService)
    {
        _dashboardService = dashboardService;
        _exportService = exportService;
    }

    /// <summary>
    /// Get complete admin dashboard data
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> GetDashboard()
    {
        try
        {
            var dashboard = await _dashboardService.GetAdminDashboardAsync();
            return Success(dashboard, "Dashboard data retrieved successfully");
        }
        catch (Exception ex)
        {
            return Error<AdminDashboardDto>($"Failed to retrieve dashboard: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<DashboardStatistics>>> GetStatistics()
    {
        try
        {
            var statistics = await _dashboardService.GetStatisticsAsync();
            return Success(statistics, "Statistics retrieved successfully");
        }
        catch (Exception ex)
        {
            return Error<DashboardStatistics>($"Failed to retrieve statistics: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get revenue statistics with optional date range filter
    /// </summary>
    [HttpGet("revenue")]
    public async Task<ActionResult<ApiResponse<RevenueStatistics>>> GetRevenue(
        [FromQuery] int months = 6, 
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var revenue = await _dashboardService.GetRevenueStatisticsAsync(months, startDate, endDate);
            return Success(revenue, "Revenue statistics retrieved successfully");
        }
        catch (Exception ex)
        {
            return Error<RevenueStatistics>($"Failed to retrieve revenue: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get booking statistics
    /// </summary>
    [HttpGet("bookings")]
    public async Task<ActionResult<ApiResponse<BookingStatistics>>> GetBookings()
    {
        try
        {
            var bookings = await _dashboardService.GetBookingStatisticsAsync();
            return Success(bookings, "Booking statistics retrieved successfully");
        }
        catch (Exception ex)
        {
            return Error<BookingStatistics>($"Failed to retrieve booking statistics: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get recent activities
    /// </summary>
    [HttpGet("activities")]
    public async Task<ActionResult<ApiResponse<List<RecentActivityDto>>>> GetActivities([FromQuery] int count = 10)
    {
        try
        {
            var activities = await _dashboardService.GetRecentActivitiesAsync(count);
            return Success(activities, "Recent activities retrieved successfully");
        }
        catch (Exception ex)
        {
            return Error<List<RecentActivityDto>>($"Failed to retrieve activities: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get popular halls
    /// </summary>
    [HttpGet("popular-halls")]
    public async Task<ActionResult<ApiResponse<List<PopularItemDto>>>> GetPopularHalls([FromQuery] int count = 5)
    {
        try
        {
            var halls = await _dashboardService.GetPopularHallsAsync(count);
            return Success(halls, "Popular halls retrieved successfully");
        }
        catch (Exception ex)
        {
            return Error<List<PopularItemDto>>($"Failed to retrieve popular halls: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get popular vendors
    /// </summary>
    [HttpGet("popular-vendors")]
    public async Task<ActionResult<ApiResponse<List<PopularItemDto>>>> GetPopularVendors([FromQuery] int count = 5)
    {
        try
        {
            var vendors = await _dashboardService.GetPopularVendorsAsync(count);
            return Success(vendors, "Popular vendors retrieved successfully");
        }
        catch (Exception ex)
        {
            return Error<List<PopularItemDto>>($"Failed to retrieve popular vendors: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Export dashboard to PDF
    /// </summary>
    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportToPdf()
    {
        try
        {
            var dashboard = await _dashboardService.GetAdminDashboardAsync();
            var pdfBytes = await _exportService.ExportToPdfAsync(dashboard);
            
            return File(pdfBytes, "text/html", $"dashboard-report-{DateTime.Now:yyyy-MM-dd}.html");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Failed to export PDF: {ex.Message}" });
        }
    }

    /// <summary>
    /// Export dashboard to Excel (CSV format)
    /// </summary>
    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportToExcel()
    {
        try
        {
            var dashboard = await _dashboardService.GetAdminDashboardAsync();
            var excelBytes = await _exportService.ExportToExcelAsync(dashboard);
            
            return File(excelBytes, "text/csv", $"dashboard-report-{DateTime.Now:yyyy-MM-dd}.csv");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Failed to export Excel: {ex.Message}" });
        }
    }

    /// <summary>
    /// Export dashboard to CSV
    /// </summary>
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportToCsv()
    {
        try
        {
            var dashboard = await _dashboardService.GetAdminDashboardAsync();
            var csvBytes = await _exportService.ExportToCsvAsync(dashboard);
            
            return File(csvBytes, "text/csv", $"dashboard-report-{DateTime.Now:yyyy-MM-dd}.csv");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Failed to export CSV: {ex.Message}" });
        }
    }
}
