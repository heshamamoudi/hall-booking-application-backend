using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HallApp.Application.DTOs.Dashboard;
using HallApp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Application.Services;

public interface IDashboardService
{
    Task<AdminDashboardDto> GetAdminDashboardAsync();
    Task<DashboardStatistics> GetStatisticsAsync();
    Task<RevenueStatistics> GetRevenueStatisticsAsync(int months = 6, DateTime? startDate = null, DateTime? endDate = null);
    Task<BookingStatistics> GetBookingStatisticsAsync();
    Task<List<PaymentMethodStatDto>> GetPaymentMethodStatisticsAsync();
    Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int count = 10);
    Task<List<PopularItemDto>> GetPopularHallsAsync(int count = 5);
    Task<List<PopularItemDto>> GetPopularVendorsAsync(int count = 5);
}

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        var dashboard = new AdminDashboardDto
        {
            Statistics = await GetStatisticsAsync(),
            Revenue = await GetRevenueStatisticsAsync(6),
            Bookings = await GetBookingStatisticsAsync(),
            PaymentMethods = await GetPaymentMethodStatisticsAsync(),
            RecentActivities = await GetRecentActivitiesAsync(10),
            PopularHalls = await GetPopularHallsAsync(5),
            PopularVendors = await GetPopularVendorsAsync(5)
        };

        return dashboard;
    }

    public async Task<DashboardStatistics> GetStatisticsAsync()
    {
        var halls = await _unitOfWork.HallRepository.GetAllAsync();
        var vendors = await _unitOfWork.VendorRepository.GetAllVendorsAsync();
        var customers = await _unitOfWork.CustomerRepository.GetAllAsync();
        var bookings = await _unitOfWork.BookingRepository.GetAllAsync();

        var today = DateTime.UtcNow.Date;

        return new DashboardStatistics
        {
            TotalHalls = halls.Count(),
            ActiveHalls = halls.Count(h => h.IsActive),
            TotalVendors = vendors.Count(),
            ActiveVendors = vendors.Count(v => v.IsActive),
            TotalCustomers = customers.Count(),
            ActiveCustomers = customers.Count(c => c.Active),
            TotalBookings = bookings.Count(),
            TodayBookings = bookings.Count(b => b.BookingDate.Date == today)
        };
    }

    public async Task<RevenueStatistics> GetRevenueStatisticsAsync(int months = 6, DateTime? startDate = null, DateTime? endDate = null)
    {
        var bookings = await _unitOfWork.BookingRepository.GetAllAsync();
        var paidBookings = bookings.Where(b => b.PaymentStatus == "Paid" || b.PaymentStatus == "Completed").ToList();

        var today = DateTime.UtcNow.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        
        // Use custom date range if provided, otherwise use months parameter
        var startOfPeriod = startDate ?? startOfMonth.AddMonths(-months + 1);
        var endOfPeriod = endDate ?? today;

        // Filter bookings by date range
        var filteredBookings = paidBookings
            .Where(b => b.BookingDate >= startOfPeriod && b.BookingDate <= endOfPeriod)
            .ToList();

        var totalRevenue = filteredBookings.Sum(b => (decimal)b.TotalAmount);
        var monthlyRevenue = filteredBookings
            .Where(b => b.BookingDate >= startOfMonth)
            .Sum(b => (decimal)b.TotalAmount);
        var todayRevenue = filteredBookings
            .Where(b => b.BookingDate.Date == today)
            .Sum(b => (decimal)b.TotalAmount);
        var averageBookingValue = filteredBookings.Any() ? totalRevenue / filteredBookings.Count : 0;

        // Monthly breakdown
        var monthlyBreakdown = filteredBookings
            .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
            .Select(g => new MonthlyRevenueDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                Revenue = g.Sum(b => (decimal)b.TotalAmount),
                BookingCount = g.Count()
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();

        return new RevenueStatistics
        {
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            TodayRevenue = todayRevenue,
            AverageBookingValue = averageBookingValue,
            MonthlyBreakdown = monthlyBreakdown
        };
    }

    public async Task<BookingStatistics> GetBookingStatisticsAsync()
    {
        var bookings = await _unitOfWork.BookingRepository.GetAllAsync();
        var today = DateTime.UtcNow.Date;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        var totalBookings = bookings.Count();
        
        // BookingStatus enum values: Draft=0, Pending=1, HallApproved=2, VendorsApproving=3, 
        // ReadyForPayment=4, Paid=5, Confirmed=6, Cancelled=7, HallRejected=8, VendorRejected=9
        
        // Group statuses logically for dashboard
        // Pending: Draft, Pending, HallApproved, VendorsApproving, ReadyForPayment
        var pendingStatuses = new[] { "Draft", "Pending", "HallApproved", "VendorsApproving", "ReadyForPayment" };
        var pendingCount = bookings.Count(b => pendingStatuses.Contains(b.Status ?? ""));
        
        // Paid/Confirmed: Paid, Confirmed
        var confirmedStatuses = new[] { "Paid", "Confirmed" };
        var confirmedCount = bookings.Count(b => confirmedStatuses.Contains(b.Status ?? ""));
        
        // Cancelled/Rejected: Cancelled, HallRejected, VendorRejected
        var cancelledStatuses = new[] { "Cancelled", "HallRejected", "VendorRejected" };
        var cancelledCount = bookings.Count(b => cancelledStatuses.Contains(b.Status ?? ""));
        
        Console.WriteLine($"📊 Booking Statistics Calculation:");
        Console.WriteLine($"   Total Bookings: {totalBookings}");
        Console.WriteLine($"   Pending (Draft+Pending+HallApproved+VendorsApproving+ReadyForPayment): {pendingCount}");
        Console.WriteLine($"   Confirmed (Paid+Confirmed): {confirmedCount}");
        Console.WriteLine($"   Cancelled (Cancelled+HallRejected+VendorRejected): {cancelledCount}");
        
        var statusBreakdown = bookings
            .GroupBy(b => b.Status ?? "Unknown")
            .Select(g => new BookingStatusCountDto
            {
                Status = g.Key,
                Count = g.Count(),
                Percentage = totalBookings > 0 ? (decimal)g.Count() / totalBookings * 100 : 0
            })
            .OrderByDescending(s => s.Count)
            .ToList();
        
        Console.WriteLine($"   Detailed Status Breakdown: {string.Join(", ", statusBreakdown.Select(s => $"{s.Status}={s.Count}"))}");

        return new BookingStatistics
        {
            PendingBookings = pendingCount,
            ConfirmedBookings = confirmedCount,
            CompletedBookings = 0, // Not used - keeping for DTO compatibility
            CancelledBookings = cancelledCount,
            TodayBookings = bookings.Count(b => b.BookingDate.Date == today),
            ThisWeekBookings = bookings.Count(b => b.BookingDate >= startOfWeek),
            ThisMonthBookings = bookings.Count(b => b.BookingDate >= startOfMonth),
            StatusBreakdown = statusBreakdown
        };
    }

    public async Task<List<PaymentMethodStatDto>> GetPaymentMethodStatisticsAsync()
    {
        var bookings = await _unitOfWork.BookingRepository.GetAllAsync();
        
        // Filter to paid/confirmed bookings (using Status field from BookingStatus enum)
        var paidStatuses = new[] { "Paid", "Confirmed" };
        var paidBookings = bookings
            .Where(b => paidStatuses.Contains(b.Status ?? ""))
            .ToList();

        Console.WriteLine($"💳 Found {paidBookings.Count} bookings with Paid/Confirmed status");
        
        // Get bookings with payment methods
        var bookingsWithPayment = paidBookings
            .Where(b => !string.IsNullOrWhiteSpace(b.PaymentMethod))
            .ToList();

        var totalCount = bookingsWithPayment.Count;
        var totalAmount = bookingsWithPayment.Sum(b => (decimal)b.TotalAmount);

        // Group by payment method and calculate statistics
        var paymentMethodStats = bookingsWithPayment
            .GroupBy(b => b.PaymentMethod ?? "Unknown")
            .Select(g => new PaymentMethodStatDto
            {
                Method = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(b => (decimal)b.TotalAmount),
                Percentage = totalCount > 0 ? (decimal)g.Count() / totalCount * 100 : 0
            })
            .OrderByDescending(p => p.Count)
            .ToList();

        // If no payment methods yet, return empty list (frontend will show placeholder)
        if (paymentMethodStats.Count == 0)
        {
            Console.WriteLine($"💳 Payment Method Statistics: No payment methods recorded yet");
        }
        else
        {
            Console.WriteLine($"💳 Payment Method Statistics:");
            Console.WriteLine($"   Total Paid Bookings with Payment Methods: {totalCount}");
            Console.WriteLine($"   Payment Methods: {string.Join(", ", paymentMethodStats.Select(p => $"{p.Method}={p.Count} ({p.Percentage:F1}%)"))}");
        }

        return paymentMethodStats;
    }

    public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int count = 10)
    {
        var bookings = await _unitOfWork.BookingRepository.GetAllAsync();
        var customers = await _unitOfWork.CustomerRepository.GetAllAsync();
        
        Console.WriteLine($"📋 Recent Activities - Total Bookings: {bookings.Count()}");
        
        // Join with customers to get actual names
        var customersMap = customers.ToDictionary(c => c.Id, c => c);
        
        var recentBookings = bookings
            .OrderByDescending(b => b.Created)
            .Take(count)
            .Select(b => {
                var amount = (decimal)b.TotalAmount;
                var customerName = "Unknown Customer";
                
                if (customersMap.TryGetValue(b.CustomerId, out var customer) && customer.AppUser != null)
                {
                    var fullName = $"{customer.AppUser.FirstName} {customer.AppUser.LastName}".Trim();
                    customerName = !string.IsNullOrWhiteSpace(fullName) 
                        ? fullName 
                        : customer.AppUser.UserName ?? $"Customer {customer.Id}";
                }
                else
                {
                    customerName = $"Customer {b.CustomerId}";
                }
                
                Console.WriteLine($"📋 Booking {b.Id}: Customer={customerName}, TotalAmount={b.TotalAmount}, Status={b.Status}");
                
                return new RecentActivityDto
                {
                    Id = b.Id,
                    Type = "Booking",
                    Action = b.Status ?? "Pending",
                    Description = $"Hall Booking #{b.Id}",
                    UserName = customerName,
                    Timestamp = b.Created,
                    Amount = amount
                };
            })
            .ToList();

        Console.WriteLine($"📋 Returning {recentBookings.Count} recent activities");
        return recentBookings;
    }

    public async Task<List<PopularItemDto>> GetPopularHallsAsync(int count = 5)
    {
        var bookings = await _unitOfWork.BookingRepository.GetAllAsync();
        var halls = await _unitOfWork.HallRepository.GetAllAsync();

        var popularHalls = bookings
            .Where(b => b.PaymentStatus == "Paid" || b.PaymentStatus == "Completed")
            .GroupBy(b => b.HallId)
            .Select(g => new
            {
                HallId = g.Key,
                BookingCount = g.Count(),
                TotalRevenue = g.Sum(b => (decimal)b.TotalAmount)
            })
            .OrderByDescending(h => h.BookingCount)
            .Take(count)
            .ToList();

        var result = new List<PopularItemDto>();
        foreach (var item in popularHalls)
        {
            var hall = halls.FirstOrDefault(h => h.ID == item.HallId);
            if (hall != null)
            {
                result.Add(new PopularItemDto
                {
                    Id = hall.ID,
                    Name = hall.Name,
                    BookingCount = item.BookingCount,
                    TotalRevenue = item.TotalRevenue,
                    Rating = 0, // Hall entity doesn't have rating property
                    ImageUrl = hall.Logo ?? string.Empty
                });
            }
        }

        return result;
    }

    public async Task<List<PopularItemDto>> GetPopularVendorsAsync(int count = 5)
    {
        var bookings = await _unitOfWork.BookingRepository.GetAllAsync();
        var vendors = await _unitOfWork.VendorRepository.GetAllVendorsAsync();

        // Get vendor bookings from VendorBookings navigation
        var vendorBookings = bookings
            .SelectMany(b => b.VendorBookings ?? new List<Core.Entities.VendorEntities.VendorBooking>())
            .Where(vb => vb.Status == "Confirmed" || vb.Status == "Completed")
            .ToList();

        var popularVendors = vendorBookings
            .GroupBy(vb => vb.VendorId)
            .Select(g => new
            {
                VendorId = g.Key,
                BookingCount = g.Count(),
                TotalRevenue = g.Sum(vb => vb.TotalAmount)
            })
            .OrderByDescending(v => v.BookingCount)
            .Take(count)
            .ToList();

        var result = new List<PopularItemDto>();
        foreach (var item in popularVendors)
        {
            var vendor = vendors.FirstOrDefault(v => v.Id == item.VendorId);
            if (vendor != null)
            {
                result.Add(new PopularItemDto
                {
                    Id = vendor.Id,
                    Name = vendor.Name,
                    BookingCount = item.BookingCount,
                    TotalRevenue = item.TotalRevenue,
                    Rating = vendor.Rating,
                    ImageUrl = vendor.LogoUrl ?? string.Empty
                });
            }
        }

        return result;
    }
}
