using HallApp.Application.DTOs.VendorManager;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Enums;
using HallApp.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

/// <summary>
/// Service interface for VendorManager dashboard operations.
/// Defined alongside implementation following the established DashboardService pattern,
/// since it returns DTOs (Application layer concern) rather than domain entities.
/// </summary>
public interface IVendorManagerDashboardService
{
    /// <summary>
    /// Gets aggregated dashboard data for the specified vendor manager,
    /// scoped to the manager's assigned vendors only.
    /// </summary>
    Task<VendorManagerDashboardDto> GetDashboardDataAsync(int appUserId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for aggregating VendorManager dashboard data.
/// Follows Single Responsibility Principle - focused exclusively on
/// building dashboard statistics and data for vendor managers.
/// </summary>
public class VendorManagerDashboardService : IVendorManagerDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VendorManagerDashboardService> _logger;

    public VendorManagerDashboardService(
        IUnitOfWork unitOfWork,
        ILogger<VendorManagerDashboardService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<VendorManagerDashboardDto> GetDashboardDataAsync(
        int appUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Building dashboard for VendorManager with AppUserId {AppUserId}", appUserId);

        var vendorManager = await _unitOfWork.VendorManagerRepository.GetByAppUserIdWithVendorsAsync(appUserId);

        if (vendorManager?.Vendors == null || !vendorManager.Vendors.Any())
        {
            _logger.LogWarning(
                "VendorManager {AppUserId} has no vendors - returning empty dashboard", appUserId);
            return new VendorManagerDashboardDto();
        }

        var vendorIds = vendorManager.Vendors.Select(v => v.Id).ToList();

        // Batch query for all vendor bookings across managed vendors
        var vendorBookings = (await _unitOfWork.VendorBookingRepository
            .GetVendorBookingsByVendorIdsAsync(vendorIds)).ToList();

        var dashboard = new VendorManagerDashboardDto
        {
            Statistics = CalculateStatistics(vendorManager, vendorBookings),
            Revenue = CalculateRevenue(vendorBookings),
            PendingApprovals = GetPendingApprovals(vendorBookings),
            RecentBookings = GetRecentBookings(vendorBookings),
            ServiceStatusDistribution = CalculateServiceStatusDistribution(vendorBookings)
        };

        _logger.LogInformation(
            "Dashboard built for VendorManager {AppUserId}: {VendorCount} vendors, {BookingCount} bookings",
            appUserId, dashboard.Statistics.TotalVendors, dashboard.Statistics.TotalBookings);

        return dashboard;
    }

    private static VendorManagerDashboardStatistics CalculateStatistics(
        VendorManager vendorManager,
        List<VendorBooking> vendorBookings)
    {
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new VendorManagerDashboardStatistics
        {
            TotalVendors = vendorManager.Vendors?.Count ?? 0,
            ActiveVendors = vendorManager.Vendors?.Count(v => v.IsActive) ?? 0,
            TotalBookings = vendorBookings.Count,
            PendingApprovals = vendorBookings.Count(vb =>
                string.Equals(vb.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
            TotalRevenue = vendorBookings
                .Where(vb => vb.IsPaid)
                .Sum(vb => vb.TotalAmount),
            MonthlyRevenue = vendorBookings
                .Where(vb => vb.IsPaid && vb.PaidAt >= thisMonthStart)
                .Sum(vb => vb.TotalAmount)
        };
    }

    private static VendorManagerRevenueData CalculateRevenue(List<VendorBooking> vendorBookings)
    {
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthDate = now.AddMonths(-1);
        var lastMonthStart = new DateTime(lastMonthDate.Year, lastMonthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthEnd = thisMonthStart.AddTicks(-1);
        var thisYearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var paidBookings = vendorBookings.Where(vb => vb.IsPaid).ToList();

        var thisMonth = paidBookings
            .Where(vb => vb.PaidAt >= thisMonthStart)
            .Sum(vb => vb.TotalAmount);

        var lastMonth = paidBookings
            .Where(vb => vb.PaidAt >= lastMonthStart && vb.PaidAt <= lastMonthEnd)
            .Sum(vb => vb.TotalAmount);

        var thisYear = paidBookings
            .Where(vb => vb.PaidAt >= thisYearStart)
            .Sum(vb => vb.TotalAmount);

        var percentageChange = lastMonth > 0
            ? Math.Round((thisMonth - lastMonth) / lastMonth * 100, 2)
            : 0;

        // Monthly breakdown for last 12 months
        var twelveMonthsAgo = now.AddMonths(-11);
        var breakdownStart = new DateTime(twelveMonthsAgo.Year, twelveMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var monthlyBreakdown = paidBookings
            .Where(vb => vb.PaidAt >= breakdownStart)
            .GroupBy(vb => new { vb.PaidAt!.Value.Year, vb.PaidAt!.Value.Month })
            .Select(g => new VendorManagerMonthlyRevenueDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                Amount = g.Sum(vb => vb.TotalAmount),
                BookingCount = g.Count()
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        return new VendorManagerRevenueData
        {
            ThisMonth = thisMonth,
            LastMonth = lastMonth,
            ThisYear = thisYear,
            PercentageChange = percentageChange,
            MonthlyBreakdown = monthlyBreakdown
        };
    }

    private static IEnumerable<VendorManagerPendingApprovalDto> GetPendingApprovals(
        List<VendorBooking> vendorBookings)
    {
        return vendorBookings
            .Where(vb => string.Equals(vb.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            .OrderBy(vb => vb.ServiceDate)
            .Take(10)
            .Select(vb => new VendorManagerPendingApprovalDto
            {
                BookingId = vb.Id,
                CustomerName = vb.Booking?.Customer?.AppUser != null
                    ? $"{vb.Booking.Customer.AppUser.FirstName} {vb.Booking.Customer.AppUser.LastName}".Trim()
                    : "Unknown",
                VendorName = vb.Vendor?.Name ?? "Unknown",
                ServiceType = vb.ServiceType,
                ServiceDate = vb.ServiceDate,
                Amount = vb.TotalAmount,
                RequestedAt = vb.CreatedAt
            })
            .ToList();
    }

    private static IEnumerable<VendorManagerRecentBookingDto> GetRecentBookings(
        List<VendorBooking> vendorBookings)
    {
        return vendorBookings
            .OrderByDescending(vb => vb.CreatedAt)
            .Take(10)
            .Select(vb => new VendorManagerRecentBookingDto
            {
                BookingId = vb.Id,
                CustomerName = vb.Booking?.Customer?.AppUser != null
                    ? $"{vb.Booking.Customer.AppUser.FirstName} {vb.Booking.Customer.AppUser.LastName}".Trim()
                    : "Unknown",
                VendorName = vb.Vendor?.Name ?? "Unknown",
                ServiceType = vb.ServiceType,
                ServiceDate = vb.ServiceDate,
                Status = vb.Status ?? "Unknown",
                Amount = vb.TotalAmount
            })
            .ToList();
    }

    private static VendorManagerServiceStatusDistribution CalculateServiceStatusDistribution(
        List<VendorBooking> vendorBookings)
    {
        return new VendorManagerServiceStatusDistribution
        {
            Pending = vendorBookings.Count(vb =>
                string.Equals(vb.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
            Approved = vendorBookings.Count(vb =>
                string.Equals(vb.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(vb.Status, "Confirmed", StringComparison.OrdinalIgnoreCase)),
            Rejected = vendorBookings.Count(vb =>
                string.Equals(vb.Status, "Rejected", StringComparison.OrdinalIgnoreCase)),
            Cancelled = vendorBookings.Count(vb =>
                string.Equals(vb.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)),
            Completed = vendorBookings.Count(vb =>
                string.Equals(vb.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        };
    }
}
