using System.ComponentModel.DataAnnotations;

namespace HallApp.Core.Entities.VendorEntities;

public class VendorType
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool RequiresHallBooking { get; set; }
    public bool AllowsMultipleBookings { get; set; }
    public int MaxSimultaneousBookings { get; set; } = 1;
    public bool RequiresTimeSlot { get; set; }
    public int? DefaultDuration { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public List<Vendor> Vendors { get; set; } = new();
}
