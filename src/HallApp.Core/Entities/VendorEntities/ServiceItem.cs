using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HallApp.Core.Entities.VendorEntities;

#nullable enable
public class ServiceItem
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(50)]
    public string ServiceType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountedPrice { get; set; }

    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // References
    public int VendorId { get; set; }
    public int VendorTypeId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public VendorType VendorType { get; set; } = null!;
    public ICollection<ServiceItemImage> Images { get; set; } = new List<ServiceItemImage>();
}
#nullable restore
