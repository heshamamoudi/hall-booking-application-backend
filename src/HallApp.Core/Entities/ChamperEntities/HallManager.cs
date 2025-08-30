using System.ComponentModel.DataAnnotations;

namespace HallApp.Core.Entities.ChamperEntities;

public class HallManager
{
    public int Id { get; set; }
    
    [Required]
    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;
    
    [StringLength(100)]
    public string CompanyName { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    
    public bool IsApproved { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
}
