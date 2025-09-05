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
    public int CustomerId { get; set; }

    public HallBookingDto Hall {  get; set; }
    public CustomerDto Customer { get; set; }
    public BookingCustomerDto BookingCustomer { get; set; }
    public string PaymentMethod { get; set; }
    public string Coupon { get; set; }
    public string Status { get; set; }
    public string Comments { get; set; }
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
    
    // Event details
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string EventType { get; set; }
    public int GuestCount { get; set; }
    public int GenderPreference { get; set; } // 0=Male, 1=Female, 2=Both
    
    // Financial information - comprehensive breakdown
    public decimal HallCost { get; set; }
    public decimal VendorServicesCost { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "SAR";
    public DateTime? PaidAt { get; set; }
    
    // Enhanced booking information for comprehensive customer view
    public List<VendorBookingDto> VendorServices { get; set; } = new();
    public BookingFinancialSummary FinancialSummary { get; set; }
}

public class BookingFinancialSummary
{
    public decimal HallCost { get; set; }
    public decimal VendorsCost { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "SAR";
    public DateTime CalculatedAt { get; set; }
    public List<VendorFinancialBreakdownDto> VendorBreakdown { get; set; } = new();
}

public class VendorFinancialBreakdownDto
{
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<ServiceFinancialDetailDto> Services { get; set; } = new();
}

public class ServiceFinancialDetailDto
{
    public int ServiceItemId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

