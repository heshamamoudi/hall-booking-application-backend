using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Vendors;

public class VendorBookingDto
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public int BookingId { get; set; }
    public string VendorName { get; set; }
    public string BookingReference { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; }
    public string Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string PaymentMethod { get; set; }
    public string PaymentStatus { get; set; }
    public string CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateVendorBookingDto
{
    [Required]
    public int VendorId { get; set; }
    
    [Required]
    public int BookingId { get; set; }
    
    [Required]
    public DateTime StartTime { get; set; }
    
    [Required]
    public DateTime EndTime { get; set; }
    
    public string Notes { get; set; }
    
    [Required]
    public decimal TotalAmount { get; set; }
}

public class UpdateVendorBookingStatusDto
{
    [Required]
    [RegularExpression("^(Approved|Rejected|Cancelled|Completed|Pending)$", 
        ErrorMessage = "Status must be 'Approved', 'Rejected', 'Cancelled', 'Completed', or 'Pending'")]
    public string Status { get; set; }
    
    public string Reason { get; set; }
}

public class ReplaceVendorDto
{
    [Required]
    public int OldVendorBookingId { get; set; }
    
    [Required]
    public int NewVendorId { get; set; }
    
    [Required]
    public DateTime StartTime { get; set; }
    
    [Required]
    public DateTime EndTime { get; set; }
    
    public string Notes { get; set; }
    
    [Required]
    public decimal TotalAmount { get; set; }
}
