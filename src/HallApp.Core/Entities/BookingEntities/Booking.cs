using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Entities.BookingEntities;

public class Booking
{
    public int Id { get; set; }
    public int HallId { get; set; }
    public int CustomerId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Coupon { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double Tax { get; set; }
    public double TotalPrice { get; set; }
    public string Comments { get; set; } = string.Empty;
    public double Discount { get; set; }
    public DateTime VisitDate { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsVisitCompleted { get; set; }
    public bool IsBookingConfirmed { get; set; }
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;
    public BookingPackage PackageDetails { get; set; } = null!;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
    
    // Additional properties to match original
    public DateTime EventDate { get; set; } = DateTime.UtcNow.AddDays(14);
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public double TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Hall Hall { get; set; }
    public List<VendorBooking> VendorBookings { get; set; } = new();
}
