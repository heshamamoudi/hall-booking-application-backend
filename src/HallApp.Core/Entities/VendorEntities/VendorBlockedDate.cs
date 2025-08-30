using System.ComponentModel.DataAnnotations;

namespace HallApp.Core.Entities.VendorEntities;

public class VendorBlockedDate
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;
    
    public bool IsRecurring { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Vendor Vendor { get; set; } = null!;
}
