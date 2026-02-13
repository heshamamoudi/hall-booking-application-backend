using HallApp.Application.DTOs.Halls.Hall;
using HallApp.Core.Enums;
using HallApp.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

/// <summary>
/// Service interface for per-hall statistics operations.
/// Defined alongside implementation following the established HallManagerDashboardService pattern,
/// since it returns DTOs (Application layer concern) rather than domain entities.
/// </summary>
public interface IHallStatsService
{
    /// <summary>
    /// Gets aggregated statistics for a specific hall.
    /// </summary>
    Task<HallStatsDto> GetHallStatsAsync(int hallId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent bookings for a specific hall with limited customer details.
    /// </summary>
    Task<IEnumerable<BookingSimpleDto>> GetHallRecentBookingsAsync(int hallId, int limit = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets monthly revenue data for a specific hall over the specified number of months.
    /// </summary>
    Task<IEnumerable<RevenueByMonthDto>> GetHallMonthlyRevenueAsync(int hallId, int months = 6, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets basic statistics for multiple halls efficiently (for list view).
    /// Returns a dictionary keyed by hall ID.
    /// </summary>
    Task<Dictionary<int, HallListStatsDto>> GetStatsForHallsAsync(IEnumerable<int> hallIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight stats DTO for the hall list endpoint.
/// Kept minimal to avoid N+1 overhead.
/// </summary>
public class HallListStatsDto
{
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PaidInvoices { get; set; }
    public int PendingInvoices { get; set; }
    public int TotalReviews { get; set; }
}

/// <summary>
/// Service for computing per-hall statistics and analytics.
/// Follows Single Responsibility Principle -- focused exclusively on
/// building statistics for individual halls.
/// </summary>
public class HallStatsService : IHallStatsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HallStatsService> _logger;

    public HallStatsService(
        IUnitOfWork unitOfWork,
        ILogger<HallStatsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HallStatsDto> GetHallStatsAsync(int hallId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building stats for Hall {HallId}", hallId);

        // Execute count queries sequentially to avoid DbContext concurrency issues
        // EF Core's DbContext is not thread-safe and cannot handle concurrent operations
        var totalBookings = await _unitOfWork.BookingRepository.GetBookingCountByHallIdAsync(hallId);
        var completedBookings = await _unitOfWork.BookingRepository.GetBookingCountByHallIdAsync(hallId, BookingStatusConstants.CompletedStatuses);
        var cancelledBookings = await _unitOfWork.BookingRepository.GetBookingCountByHallIdAsync(hallId, BookingStatusConstants.CancelledStatuses);
        var pendingBookings = await _unitOfWork.BookingRepository.GetBookingCountByHallIdAsync(hallId, BookingStatusConstants.PendingStatuses);
        var activeBookings = await _unitOfWork.BookingRepository.GetBookingCountByHallIdAsync(hallId, BookingStatusConstants.ActiveStatuses);
        var totalRevenue = await _unitOfWork.InvoiceRepository.GetTotalRevenueByHallIdAsync(hallId);
        var paidInvoices = await _unitOfWork.InvoiceRepository.GetInvoiceCountByHallIdAsync(hallId, "Paid");
        var pendingInvoices = await _unitOfWork.InvoiceRepository.GetInvoiceCountByHallIdAsync(hallId, "Pending");

        // Review data from existing repository
        var averageRating = await _unitOfWork.ReviewRepository.GetAverageRatingByHallIdAsync(hallId);
        var reviews = await _unitOfWork.ReviewRepository.GetReviewsByHallIdAsync(hallId);

        return new HallStatsDto
        {
            TotalBookings = totalBookings,
            CompletedBookings = completedBookings,
            CancelledBookings = cancelledBookings,
            PendingBookings = pendingBookings,
            ActiveBookings = activeBookings,
            TotalRevenue = totalRevenue,
            PaidInvoices = paidInvoices,
            PendingInvoices = pendingInvoices,
            AverageRating = averageRating,
            TotalReviews = reviews.Count()
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BookingSimpleDto>> GetHallRecentBookingsAsync(
        int hallId, int limit = 5, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting {Limit} recent bookings for Hall {HallId}", limit, hallId);

        var bookings = await _unitOfWork.BookingRepository.GetRecentBookingsByHallIdAsync(hallId, limit);

        return bookings.Select(b => new BookingSimpleDto
        {
            Id = b.Id,
            CustomerName = b.Customer?.AppUser != null
                ? $"{b.Customer.AppUser.FirstName} {b.Customer.AppUser.LastName}".Trim()
                : "Unknown",
            EventDate = b.EventDate,
            EventType = b.EventType,
            Status = b.Status,
            TotalAmount = b.TotalAmount,
            CreatedAt = b.CreatedAt
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RevenueByMonthDto>> GetHallMonthlyRevenueAsync(
        int hallId, int months = 6, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting {Months} months of revenue data for Hall {HallId}", months, hallId);

        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(months - 1));
        var endDate = now;

        var invoices = await _unitOfWork.InvoiceRepository.GetInvoicesByHallIdAndDateRangeAsync(hallId, startDate, endDate);
        var bookings = await _unitOfWork.BookingRepository.GetBookingsByHallIdAndDateRangeAsync(hallId, startDate, endDate);

        // Group invoices by month for revenue
        var invoicesByMonth = invoices
            .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
            .ToDictionary(
                g => $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                g => g.Sum(i => i.TotalAmountWithTax));

        // Group bookings by month for booking count
        var bookingsByMonth = bookings
            .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
            .ToDictionary(
                g => $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                g => g.Count());

        // Generate all months in the range (even months with zero data)
        var result = new List<RevenueByMonthDto>();
        for (var i = 0; i < months; i++)
        {
            var monthDate = startDate.AddMonths(i);
            var monthKey = $"{monthDate.Year:D4}-{monthDate.Month:D2}";

            result.Add(new RevenueByMonthDto
            {
                Month = monthKey,
                Revenue = invoicesByMonth.TryGetValue(monthKey, out var revenue) ? revenue : 0m,
                Bookings = bookingsByMonth.TryGetValue(monthKey, out var count) ? count : 0
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Dictionary<int, HallListStatsDto>> GetStatsForHallsAsync(
        IEnumerable<int> hallIds, CancellationToken cancellationToken = default)
    {
        var hallIdList = hallIds.ToList();
        if (hallIdList.Count == 0)
            return new Dictionary<int, HallListStatsDto>();

        _logger.LogInformation("Getting list stats for {Count} halls", hallIdList.Count);

        // Batch-load bookings and invoices for all halls
        var allBookings = (await _unitOfWork.BookingRepository.GetBookingsByHallIdsAsync(hallIdList)).ToList();
        var allInvoices = (await _unitOfWork.InvoiceRepository.GetInvoicesByHallIdsAsync(hallIdList)).ToList();

        // Load reviews for all halls
        var result = new Dictionary<int, HallListStatsDto>();

        foreach (var hallId in hallIdList)
        {
            var hallBookings = allBookings.Where(b => b.HallId == hallId).ToList();
            var hallInvoices = allInvoices.Where(i => i.HallId == hallId).ToList();
            var reviews = await _unitOfWork.ReviewRepository.GetReviewsByHallIdAsync(hallId);

            result[hallId] = new HallListStatsDto
            {
                TotalBookings = hallBookings.Count,
                TotalRevenue = hallInvoices
                    .Where(i => i.PaymentStatus == "Paid" && !i.IsCancelled)
                    .Sum(i => i.TotalAmountWithTax),
                PaidInvoices = hallInvoices.Count(i => i.PaymentStatus == "Paid" && !i.IsCancelled),
                PendingInvoices = hallInvoices.Count(i => i.PaymentStatus == "Pending" && !i.IsCancelled),
                TotalReviews = reviews.Count()
            };
        }

        return result;
    }
}
