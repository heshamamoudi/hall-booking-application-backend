using HallApp.Core.Entities.BookingEntities;
using System.ComponentModel.DataAnnotations.Schema;

namespace HallApp.Core.Entities.VendorEntities;

public class VendorBooking : BaseEntity
{
    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; } = DateTime.UtcNow.AddHours(2);
    public string Status { get; set; } = "Pending";
    public string Notes { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string CancellationReason { get; set; } = string.Empty;
    public DateTime? CancelledAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    
    // Additional properties to match original
    public DateTime ServiceDate { get; set; } = DateTime.UtcNow;
    public string ServiceType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property for grouped services
    public List<VendorBookingService> Services { get; set; } = new();
}
