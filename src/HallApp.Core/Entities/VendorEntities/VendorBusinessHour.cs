using System.ComponentModel.DataAnnotations;

namespace HallApp.Core.Entities.VendorEntities;

public class VendorBusinessHour
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; }

    public bool IsOpen => !IsClosed;

    [StringLength(200)]
    public string SpecialNote { get; set; } = string.Empty;
    
    public Vendor Vendor { get; set; } = null!;
}
