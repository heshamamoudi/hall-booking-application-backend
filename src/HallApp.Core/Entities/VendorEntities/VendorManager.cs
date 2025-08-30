using System.ComponentModel.DataAnnotations;

namespace HallApp.Core.Entities.VendorEntities;

#nullable enable
public class VendorManager
{
    public int Id { get; set; }

    [Required]
    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    [StringLength(50)]
    public string? CommercialRegistrationNumber { get; set; }

    [StringLength(50)]
    public string? VatNumber { get; set; }

    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public List<Vendor> Vendors { get; set; } = new();
}
#nullable restore
