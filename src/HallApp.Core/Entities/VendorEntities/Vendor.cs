using System.ComponentModel.DataAnnotations;
using HallApp.Core.Entities.ReviewEntities;

#nullable enable
namespace HallApp.Core.Entities.VendorEntities;

public class Vendor
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }

    // Navigation properties
    public VendorLocation? Location { get; set; }
    public List<VendorManager> Managers { get; set; } = new();
    public List<ServiceItem> ServiceItems { get; set; } = new();
    public List<VendorBusinessHour> BusinessHours { get; set; } = new();
    public List<VendorBlockedDate> BlockedDates { get; set; } = new();
    public List<VendorBooking> VendorBookings { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();

    // References
    public int VendorTypeId { get; set; }
    public VendorType VendorType { get; set; } = null!;
}
#nullable restore
