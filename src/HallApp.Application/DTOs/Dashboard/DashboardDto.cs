using System;
using System.Collections.Generic;

namespace HallApp.Application.DTOs.Dashboard;

public class AdminDashboardDto
{
    public DashboardStatistics Statistics { get; set; } = new();
    public RevenueStatistics Revenue { get; set; } = new();
    public BookingStatistics Bookings { get; set; } = new();
    public List<PaymentMethodStatDto> PaymentMethods { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
    public List<PopularItemDto> PopularHalls { get; set; } = new();
    public List<PopularItemDto> PopularVendors { get; set; } = new();
}

public class DashboardStatistics
{
    public int TotalHalls { get; set; }
    public int ActiveHalls { get; set; }
    public int TotalVendors { get; set; }
    public int ActiveVendors { get; set; }
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int TotalBookings { get; set; }
    public int TodayBookings { get; set; }
}

public class RevenueStatistics
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal AverageBookingValue { get; set; }
    public List<MonthlyRevenueDto> MonthlyBreakdown { get; set; } = new();
}

public class MonthlyRevenueDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int BookingCount { get; set; }
}

public class BookingStatistics
{
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int TodayBookings { get; set; }
    public int ThisWeekBookings { get; set; }
    public int ThisMonthBookings { get; set; }
    public List<BookingStatusCountDto> StatusBreakdown { get; set; } = new();
}

public class BookingStatusCountDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class PaymentMethodStatDto
{
    public string Method { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Percentage { get; set; }
}

public class RecentActivityDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // Booking, Hall, Vendor, Customer
    public string Action { get; set; } = string.Empty; // Created, Updated, Cancelled
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal Amount { get; set; } // Booking amount for financial tracking
}

public class PopularItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public double Rating { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}
