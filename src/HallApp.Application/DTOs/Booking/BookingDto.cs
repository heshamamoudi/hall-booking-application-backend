using HallApp.Application.DTOs.Booking;
using HallApp.Application.DTOs.Champer.Hall;
using HallApp.Application.DTOs.Customer;
using HallApp.Application.DTOs.Vendors;
using HallApp.Core.Entities.BookingEntities;

namespace HallApp.Application.DTOs.Booking;

public class BookingDto
{
    public int Id { get; set; }
    public int HallId { get; set; }

    public HallBookingDto Hall {  get; set; }
    public HallInfo HallInfo { get; set; }
    public int CustomerId { get; set; }
    public BookingCustomerDto Customer { get; set; }
    public string PaymentMethod { get; set; }
    public string Coupon { get; set; }
    public string Status { get; set; }
    public double Tax { get; set; }
    public double TotalPrice { get; set; }
    public string Comments { get; set; }
    public double Discount { get; set; }
    public DateTime VisitDate { get; set; }
    public bool IsVisitCompleted { get; set; }
    public bool IsBookingConfirmed { get; set; }
    public DateTime BookingDate { get; set; }
    public BookingPackage PackageDetails { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string PaymentStatus { get; set; } = "Pending";
    
    // Enhanced booking information for comprehensive customer view
    public List<VendorBookingDto> VendorServices { get; set; } = new();
    public BookingFinancialSummary FinancialSummary { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string EventType { get; set; }
    public int GuestCount { get; set; }
}

public class BookingFinancialSummary
{
    public double HallCost { get; set; }
    public double VendorsCost { get; set; }
    public double SubTotal { get; set; }
    public double Discount { get; set; }
    public double Tax { get; set; }
    public double TotalAmount { get; set; }
    public string Currency { get; set; } = "SAR";
}
