namespace HallApp.Application.DTOs.HallManager;

/// <summary>
/// Dashboard statistics and data for the HallManager role.
/// Contains only data relevant to the manager's assigned halls,
/// enforcing data isolation at the DTO level.
/// </summary>
public class HallManagerDashboardDto
{
    public HallManagerDashboardStatistics Statistics { get; set; } = new();
    public HallManagerRevenueData Revenue { get; set; } = new();
    public IEnumerable<HallManagerPendingApprovalDto> PendingApprovals { get; set; } = [];
    public IEnumerable<HallManagerRecentBookingDto> RecentBookings { get; set; } = [];
}

/// <summary>
/// Aggregated statistics for the hall manager's assigned halls
/// </summary>
public class HallManagerDashboardStatistics
{
    public int TotalHalls { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalBookings { get; set; }
    public int PendingApprovals { get; set; }
    public int ActiveBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
}

/// <summary>
/// Revenue breakdown for the hall manager's halls
/// </summary>
public class HallManagerRevenueData
{
    public decimal ThisMonth { get; set; }
    public decimal LastMonth { get; set; }
    public decimal ThisYear { get; set; }
    public decimal PercentageChange { get; set; }
    public IEnumerable<HallManagerMonthlyRevenueDto> MonthlyBreakdown { get; set; } = [];
}

/// <summary>
/// Monthly revenue data point
/// </summary>
public class HallManagerMonthlyRevenueDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int BookingCount { get; set; }
}

/// <summary>
/// Booking pending the hall manager's approval
/// </summary>
public class HallManagerPendingApprovalDto
{
    public int BookingId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string HallName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal Amount { get; set; }
    public DateTime RequestedAt { get; set; }
}

/// <summary>
/// Recent booking for the hall manager's dashboard
/// </summary>
public class HallManagerRecentBookingDto
{
    public int BookingId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string HallName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
