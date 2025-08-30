using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HallApp.Core.Entities.VendorEntities;

public class VendorLocation
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(50)]
    public string State { get; set; } = string.Empty;

    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [StringLength(50)]
    public string Country { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;

    // References
    public int VendorId { get; set; }
    [ForeignKey("VendorId")]
    public Vendor Vendor { get; set; } = null!;
}
