namespace HallApp.Application.DTOs.VendorManager;

/// <summary>
/// Dashboard statistics and data for the VendorManager role.
/// Contains only data relevant to the manager's assigned vendors,
/// enforcing data isolation at the DTO level.
/// </summary>
public class VendorManagerDashboardDto
{
    public VendorManagerDashboardStatistics Statistics { get; set; } = new();
    public VendorManagerRevenueData Revenue { get; set; } = new();
    public IEnumerable<VendorManagerPendingApprovalDto> PendingApprovals { get; set; } = [];
    public IEnumerable<VendorManagerRecentBookingDto> RecentBookings { get; set; } = [];
    public VendorManagerServiceStatusDistribution ServiceStatusDistribution { get; set; } = new();
}

/// <summary>
/// Aggregated statistics for the vendor manager's assigned vendors
/// </summary>
public class VendorManagerDashboardStatistics
{
    public int TotalVendors { get; set; }
    public int ActiveVendors { get; set; }
    public int TotalBookings { get; set; }
    public int PendingApprovals { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
}

/// <summary>
/// Revenue breakdown for the vendor manager's vendors
/// </summary>
public class VendorManagerRevenueData
{
    public decimal ThisMonth { get; set; }
    public decimal LastMonth { get; set; }
    public decimal ThisYear { get; set; }
    public decimal PercentageChange { get; set; }
    public IEnumerable<VendorManagerMonthlyRevenueDto> MonthlyBreakdown { get; set; } = [];
}

/// <summary>
/// Monthly revenue data point for vendor manager charts
/// </summary>
public class VendorManagerMonthlyRevenueDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int BookingCount { get; set; }
}

/// <summary>
/// Vendor booking pending the vendor manager's approval
/// </summary>
public class VendorManagerPendingApprovalDto
{
    public int BookingId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public decimal Amount { get; set; }
    public DateTime RequestedAt { get; set; }
}

/// <summary>
/// Recent vendor booking for the vendor manager's dashboard
/// </summary>
public class VendorManagerRecentBookingDto
{
    public int BookingId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// Distribution of vendor booking statuses for the donut chart
/// </summary>
public class VendorManagerServiceStatusDistribution
{
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Cancelled { get; set; }
    public int Completed { get; set; }
}
