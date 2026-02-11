using HallApp.Application.DTOs.HallManager;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Enums;
using HallApp.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

/// <summary>
/// Service interface for HallManager dashboard operations.
/// Defined alongside implementation following the established DashboardService pattern,
/// since it returns DTOs (Application layer concern) rather than domain entities.
/// </summary>
public interface IHallManagerDashboardService
{
    /// <summary>
    /// Gets aggregated dashboard data for the specified hall manager,
    /// scoped to the manager's assigned halls only.
    /// </summary>
    Task<HallManagerDashboardDto> GetDashboardDataAsync(int appUserId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for aggregating HallManager dashboard data.
/// Follows Single Responsibility Principle - focused exclusively on
/// building dashboard statistics and data for hall managers.
/// </summary>
public class HallManagerDashboardService : IHallManagerDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HallManagerDashboardService> _logger;

    public HallManagerDashboardService(
        IUnitOfWork unitOfWork,
        ILogger<HallManagerDashboardService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HallManagerDashboardDto> GetDashboardDataAsync(
        int appUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Building dashboard for HallManager with AppUserId {AppUserId}", appUserId);

        // CRIT-001 FIX: Direct query instead of GetAllAsync().FirstOrDefault()
        var hallManager = await _unitOfWork.HallManagerRepository.GetByAppUserIdWithHallsAsync(appUserId);

        if (hallManager?.Halls == null || !hallManager.Halls.Any())
        {
            _logger.LogWarning(
                "HallManager {AppUserId} has no halls - returning empty dashboard", appUserId);
            return new HallManagerDashboardDto();
        }

        var hallIds = hallManager.Halls.Select(h => h.ID).ToList();

        // Get bookings for managed halls using the efficient repository method
        var managerBookings = (await _unitOfWork.BookingRepository.GetBookingsByHallIdsAsync(hallIds)).ToList();

        // HIGH-001 FIX: Batch invoice query instead of N+1 parallel calls
        var managerInvoices = (await _unitOfWork.InvoiceRepository.GetInvoicesByHallIdsAsync(hallIds)).ToList();

        var dashboard = new HallManagerDashboardDto
        {
            Statistics = CalculateStatistics(hallManager, managerBookings, managerInvoices),
            Revenue = CalculateRevenue(managerBookings, managerInvoices),
            PendingApprovals = GetPendingApprovals(managerBookings),
            RecentBookings = GetRecentBookings(managerBookings)
        };

        _logger.LogInformation(
            "Dashboard built for HallManager {AppUserId}: {HallCount} halls, {BookingCount} bookings",
            appUserId, dashboard.Statistics.TotalHalls, dashboard.Statistics.TotalBookings);

        return dashboard;
    }

    private static HallManagerDashboardStatistics CalculateStatistics(
        HallManager hallManager,
        List<Booking> bookings,
        List<Invoice> invoices)
    {
        // DUP-005 FIX: Use centralized BookingStatusConstants instead of magic strings
        return new HallManagerDashboardStatistics
        {
            TotalHalls = hallManager.Halls?.Count ?? 0,
            TotalRevenue = invoices
                .Where(i => i.PaymentStatus == BookingStatusConstants.Paid)
                .Sum(i => i.TotalAmountWithTax),
            TotalBookings = bookings.Count,
            PendingApprovals = bookings.Count(b => BookingStatusConstants.PendingStatuses.Contains(b.Status)),
            ActiveBookings = bookings.Count(b => BookingStatusConstants.ActiveStatuses.Contains(b.Status)),
            CompletedBookings = bookings.Count(b => BookingStatusConstants.CompletedStatuses.Contains(b.Status)),
            CancelledBookings = bookings.Count(b => BookingStatusConstants.CancelledStatuses.Contains(b.Status))
        };
    }

    private static HallManagerRevenueData CalculateRevenue(
        List<Booking> bookings,
        List<Invoice> invoices)
    {
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthDate = now.AddMonths(-1);
        var lastMonthStart = new DateTime(lastMonthDate.Year, lastMonthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthEnd = thisMonthStart.AddTicks(-1);
        var thisYearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var paidInvoices = invoices.Where(i => i.PaymentStatus == BookingStatusConstants.Paid).ToList();

        var thisMonth = paidInvoices
            .Where(i => i.InvoiceDate >= thisMonthStart)
            .Sum(i => i.TotalAmountWithTax);

        var lastMonth = paidInvoices
            .Where(i => i.InvoiceDate >= lastMonthStart && i.InvoiceDate <= lastMonthEnd)
            .Sum(i => i.TotalAmountWithTax);

        var thisYear = paidInvoices
            .Where(i => i.InvoiceDate >= thisYearStart)
            .Sum(i => i.TotalAmountWithTax);

        var percentageChange = lastMonth > 0
            ? Math.Round((thisMonth - lastMonth) / lastMonth * 100, 2)
            : 0;

        // Monthly breakdown for last 12 months using paid bookings
        var twelveMonthsAgo = now.AddMonths(-11);
        var breakdownStart = new DateTime(twelveMonthsAgo.Year, twelveMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var monthlyBreakdown = bookings
            .Where(b => BookingStatusConstants.PaidStatuses.Contains(b.Status) && b.BookingDate >= breakdownStart)
            .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
            .Select(g => new HallManagerMonthlyRevenueDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                Amount = g.Sum(b => b.TotalAmount),
                BookingCount = g.Count()
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        return new HallManagerRevenueData
        {
            ThisMonth = thisMonth,
            LastMonth = lastMonth,
            ThisYear = thisYear,
            PercentageChange = percentageChange,
            MonthlyBreakdown = monthlyBreakdown
        };
    }

    private static IEnumerable<HallManagerPendingApprovalDto> GetPendingApprovals(List<Booking> bookings)
    {
        return bookings
            .Where(b => BookingStatusConstants.PendingStatuses.Contains(b.Status))
            .OrderBy(b => b.EventDate)
            .Take(10)
            .Select(b => new HallManagerPendingApprovalDto
            {
                BookingId = b.Id,
                CustomerName = b.Customer?.AppUser != null
                    ? $"{b.Customer.AppUser.FirstName} {b.Customer.AppUser.LastName}".Trim()
                    : "Unknown",
                HallName = b.Hall?.Name ?? "Unknown",
                EventDate = b.EventDate,
                Amount = b.TotalAmount,
                RequestedAt = b.Created
            })
            .ToList();
    }

    private static IEnumerable<HallManagerRecentBookingDto> GetRecentBookings(List<Booking> bookings)
    {
        return bookings
            .OrderByDescending(b => b.Created)
            .Take(10)
            .Select(b => new HallManagerRecentBookingDto
            {
                BookingId = b.Id,
                CustomerName = b.Customer?.AppUser != null
                    ? $"{b.Customer.AppUser.FirstName} {b.Customer.AppUser.LastName}".Trim()
                    : "Unknown",
                HallName = b.Hall?.Name ?? "Unknown",
                EventDate = b.EventDate,
                Status = b.Status ?? "Unknown",
                Amount = b.TotalAmount
            })
            .ToList();
    }
}
