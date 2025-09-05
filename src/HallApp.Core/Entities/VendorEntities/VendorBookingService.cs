using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HallApp.Core.Entities.VendorEntities;

public class VendorBookingService
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int VendorBookingId { get; set; }
    
    [Required]
    public int ServiceItemId { get; set; }
    
    [Required]
    public int Quantity { get; set; }
    
    public string SpecialInstructions { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("VendorBookingId")]
    public VendorBooking? VendorBooking { get; set; }
    
    [ForeignKey("ServiceItemId")]
    public ServiceItem? ServiceItem { get; set; }
    
    // Base entity properties
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Updated { get; set; }
    public bool IsActive { get; set; } = true;
}
