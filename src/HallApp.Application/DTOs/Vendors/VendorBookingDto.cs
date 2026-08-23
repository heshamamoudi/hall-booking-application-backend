using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Vendors;

public class VendorBookingDto
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public int BookingId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string BookingReference { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string CancellationReason { get; set; } = string.Empty;
    public DateTime? CancelledAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<VendorBookingServiceDto> ServiceItems { get; set; } = new();
    public VendorContactInfo? ContactInfo { get; set; }

    // --- Context from the parent booking -----------------------------------
    // A vendor cannot act on a request without knowing what the event is, when
    // it is, where it is and who booked it. Without these the bookings list
    // shows a row of amounts and nothing to decide on.

    /// <summary>When the vendor is expected to deliver the service.</summary>
    public DateTime ServiceDate { get; set; }

    /// <summary>Date of the event this service belongs to.</summary>
    public DateTime? EventDate { get; set; }

    /// <summary>Wedding, Engagement, Corporate, and so on.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Expected number of guests, which drives most vendor quoting.</summary>
    public int GuestCount { get; set; }

    /// <summary>The kind of service booked, e.g. Catering or Photography.</summary>
    public string ServiceType { get; set; } = string.Empty;

    public int HallId { get; set; }
    public string HallName { get; set; } = string.Empty;
    public string HallCity { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
}

public class VendorContactInfo
{
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
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
    
    public string Notes { get; set; } = string.Empty;
    
    [Required]
    public decimal TotalAmount { get; set; }
}

public class UpdateVendorBookingStatusDto
{
    [Required]
    [RegularExpression("^(Approved|Rejected|Cancelled|Completed|Pending)$", 
        ErrorMessage = "Status must be 'Approved', 'Rejected', 'Cancelled', 'Completed', or 'Pending'")]
    public string Status { get; set; } = string.Empty;
    
    public string Reason { get; set; } = string.Empty;
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
    
    public string Notes { get; set; } = string.Empty;
    
    [Required]
    public decimal TotalAmount { get; set; }
}
