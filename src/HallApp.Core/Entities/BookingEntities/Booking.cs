using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.CustomerEntities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HallApp.Core.Entities.BookingEntities;

public class Booking
{
    public int Id { get; set; }
    public int HallId { get; set; }
    public int CustomerId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Coupon { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsVisitCompleted { get; set; }
    public bool IsBookingConfirmed { get; set; }
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;
    public BookingPackage? PackageDetails { get; set; }
    // CRIT-004 FIX: Removed duplicate Created/Updated fields.
    // Use CreatedAt/UpdatedAt (lines 61-62) instead.

    // Event details
    public DateTime EventDate { get; set; } = DateTime.UtcNow.AddDays(14);
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public int GenderPreference { get; set; } // 0=Male, 1=Female, 2=Both
    
    // Financial information - using decimal for precision
    [Column(TypeName = "decimal(18,2)")]
    public decimal HallCost { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal VendorServicesCost { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    // HIGH-005 FIX: Added explicit decimal precision for tax rate
    [Column(TypeName = "decimal(5,4)")]
    public decimal TaxRate { get; set; } = 0.15m; // 15% VAT for Saudi Arabia (stored as 0.1500)

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    
    public string Currency { get; set; } = "SAR";
    
    // Payment status
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime? PaidAt { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // BONUS: Optimistic concurrency token
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    public Hall? Hall { get; set; }
    public Customer? Customer { get; set; }
    public List<VendorBooking> VendorBookings { get; set; } = new();
    public Invoice? Invoice { get; set; }
    public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
}
