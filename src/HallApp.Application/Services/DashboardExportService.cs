using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HallApp.Application.DTOs.Dashboard;

namespace HallApp.Application.Services;

public interface IDashboardExportService
{
    Task<byte[]> ExportToPdfAsync(AdminDashboardDto dashboard);
    Task<byte[]> ExportToExcelAsync(AdminDashboardDto dashboard);
    Task<byte[]> ExportToCsvAsync(AdminDashboardDto dashboard);
}

public class DashboardExportService : IDashboardExportService
{
    public async Task<byte[]> ExportToPdfAsync(AdminDashboardDto dashboard)
    {
        // Note: For production, use a library like iTextSharp or QuestPDF
        // This is a basic HTML-to-PDF approach
        await Task.CompletedTask;
        
        var html = GenerateHtmlReport(dashboard);
        var bytes = Encoding.UTF8.GetBytes(html);
        return bytes;
    }

    public async Task<byte[]> ExportToExcelAsync(AdminDashboardDto dashboard)
    {
        // Note: For production, use a library like EPPlus or ClosedXML
        // This is a basic CSV format for Excel compatibility
        await Task.CompletedTask;
        
        var csv = GenerateCsvReport(dashboard);
        var bytes = Encoding.UTF8.GetBytes(csv);
        return bytes;
    }

    public async Task<byte[]> ExportToCsvAsync(AdminDashboardDto dashboard)
    {
        await Task.CompletedTask;
        
        var csv = GenerateCsvReport(dashboard);
        var bytes = Encoding.UTF8.GetBytes(csv);
        return bytes;
    }

    private string GenerateHtmlReport(AdminDashboardDto dashboard)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='UTF-8'>");
        sb.AppendLine("<title>Admin Dashboard Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
        sb.AppendLine("h1 { color: #333; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; margin: 20px 0; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }");
        sb.AppendLine("th { background-color: #4CAF50; color: white; }");
        sb.AppendLine(".section { margin: 30px 0; }");
        sb.AppendLine(".stat-box { display: inline-block; margin: 10px; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        
        sb.AppendLine($"<h1>Admin Dashboard Report - {DateTime.Now:yyyy-MM-dd HH:mm}</h1>");
        
        // Statistics Section
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<h2>Statistics Overview</h2>");
        sb.AppendLine("<div class='stat-box'><strong>Total Halls:</strong> " + dashboard.Statistics.TotalHalls + " (Active: " + dashboard.Statistics.ActiveHalls + ")</div>");
        sb.AppendLine("<div class='stat-box'><strong>Total Vendors:</strong> " + dashboard.Statistics.TotalVendors + " (Active: " + dashboard.Statistics.ActiveVendors + ")</div>");
        sb.AppendLine("<div class='stat-box'><strong>Total Customers:</strong> " + dashboard.Statistics.TotalCustomers + " (Active: " + dashboard.Statistics.ActiveCustomers + ")</div>");
        sb.AppendLine("<div class='stat-box'><strong>Total Bookings:</strong> " + dashboard.Statistics.TotalBookings + " (Today: " + dashboard.Statistics.TodayBookings + ")</div>");
        sb.AppendLine("</div>");
        
        // Revenue Section
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<h2>Revenue Statistics</h2>");
        sb.AppendLine("<div class='stat-box'><strong>Total Revenue:</strong> " + dashboard.Revenue.TotalRevenue.ToString("N2") + " SAR</div>");
        sb.AppendLine("<div class='stat-box'><strong>Monthly Revenue:</strong> " + dashboard.Revenue.MonthlyRevenue.ToString("N2") + " SAR</div>");
        sb.AppendLine("<div class='stat-box'><strong>Today's Revenue:</strong> " + dashboard.Revenue.TodayRevenue.ToString("N2") + " SAR</div>");
        sb.AppendLine("<div class='stat-box'><strong>Avg Booking Value:</strong> " + dashboard.Revenue.AverageBookingValue.ToString("N2") + " SAR</div>");
        sb.AppendLine("</div>");
        
        // Booking Statistics
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<h2>Booking Statistics</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Status</th><th>Count</th></tr>");
        sb.AppendLine($"<tr><td>Confirmed</td><td>{dashboard.Bookings.ConfirmedBookings}</td></tr>");
        sb.AppendLine($"<tr><td>Pending</td><td>{dashboard.Bookings.PendingBookings}</td></tr>");
        sb.AppendLine($"<tr><td>Completed</td><td>{dashboard.Bookings.CompletedBookings}</td></tr>");
        sb.AppendLine($"<tr><td>Cancelled</td><td>{dashboard.Bookings.CancelledBookings}</td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine("</div>");
        
        // Popular Halls
        if (dashboard.PopularHalls.Any())
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>Popular Halls</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Hall Name</th><th>Bookings</th><th>Revenue (SAR)</th><th>Rating</th></tr>");
            foreach (var hall in dashboard.PopularHalls)
            {
                sb.AppendLine($"<tr><td>{hall.Name}</td><td>{hall.BookingCount}</td><td>{hall.TotalRevenue:N2}</td><td>{hall.Rating:F1}</td></tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");
        }
        
        // Popular Vendors
        if (dashboard.PopularVendors.Any())
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>Popular Vendors</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Vendor Name</th><th>Bookings</th><th>Revenue (SAR)</th><th>Rating</th></tr>");
            foreach (var vendor in dashboard.PopularVendors)
            {
                sb.AppendLine($"<tr><td>{vendor.Name}</td><td>{vendor.BookingCount}</td><td>{vendor.TotalRevenue:N2}</td><td>{vendor.Rating:F1}</td></tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");
        }
        
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }

    private string GenerateCsvReport(AdminDashboardDto dashboard)
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine($"Admin Dashboard Report - Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        
        // Statistics
        sb.AppendLine("STATISTICS");
        sb.AppendLine("Metric,Total,Active");
        sb.AppendLine($"Halls,{dashboard.Statistics.TotalHalls},{dashboard.Statistics.ActiveHalls}");
        sb.AppendLine($"Vendors,{dashboard.Statistics.TotalVendors},{dashboard.Statistics.ActiveVendors}");
        sb.AppendLine($"Customers,{dashboard.Statistics.TotalCustomers},{dashboard.Statistics.ActiveCustomers}");
        sb.AppendLine($"Bookings,{dashboard.Statistics.TotalBookings},{dashboard.Statistics.TodayBookings}");
        sb.AppendLine();
        
        // Revenue
        sb.AppendLine("REVENUE (SAR)");
        sb.AppendLine("Metric,Amount");
        sb.AppendLine($"Total Revenue,{dashboard.Revenue.TotalRevenue:F2}");
        sb.AppendLine($"Monthly Revenue,{dashboard.Revenue.MonthlyRevenue:F2}");
        sb.AppendLine($"Today's Revenue,{dashboard.Revenue.TodayRevenue:F2}");
        sb.AppendLine($"Average Booking Value,{dashboard.Revenue.AverageBookingValue:F2}");
        sb.AppendLine();
        
        // Bookings
        sb.AppendLine("BOOKINGS BY STATUS");
        sb.AppendLine("Status,Count");
        sb.AppendLine($"Confirmed,{dashboard.Bookings.ConfirmedBookings}");
        sb.AppendLine($"Pending,{dashboard.Bookings.PendingBookings}");
        sb.AppendLine($"Completed,{dashboard.Bookings.CompletedBookings}");
        sb.AppendLine($"Cancelled,{dashboard.Bookings.CancelledBookings}");
        sb.AppendLine();
        
        // Popular Halls
        if (dashboard.PopularHalls.Any())
        {
            sb.AppendLine("POPULAR HALLS");
            sb.AppendLine("Name,Bookings,Revenue (SAR),Rating");
            foreach (var hall in dashboard.PopularHalls)
            {
                sb.AppendLine($"\"{hall.Name}\",{hall.BookingCount},{hall.TotalRevenue:F2},{hall.Rating:F1}");
            }
            sb.AppendLine();
        }
        
        // Popular Vendors
        if (dashboard.PopularVendors.Any())
        {
            sb.AppendLine("POPULAR VENDORS");
            sb.AppendLine("Name,Bookings,Revenue (SAR),Rating");
            foreach (var vendor in dashboard.PopularVendors)
            {
                sb.AppendLine($"\"{vendor.Name}\",{vendor.BookingCount},{vendor.TotalRevenue:F2},{vendor.Rating:F1}");
            }
            sb.AppendLine();
        }
        
        // Monthly Breakdown
        if (dashboard.Revenue.MonthlyBreakdown.Any())
        {
            sb.AppendLine("MONTHLY REVENUE BREAKDOWN");
            sb.AppendLine("Month,Revenue (SAR),Booking Count");
            foreach (var month in dashboard.Revenue.MonthlyBreakdown)
            {
                sb.AppendLine($"{month.MonthName},{month.Revenue:F2},{month.BookingCount}");
            }
        }
        
        return sb.ToString();
    }
}
