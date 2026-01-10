using System.ComponentModel.DataAnnotations;

namespace HallApp.Core.Entities.VendorEntities;

/// <summary>
/// Represents a user who manages one or more vendors.
/// Business properties (commercial registration, VAT) belong to the Vendor entity.
/// </summary>
public class VendorManager
{
    public int Id { get; set; }

    [Required]
    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties - Many-to-Many with Vendors
    public List<Vendor> Vendors { get; set; } = new();
}
