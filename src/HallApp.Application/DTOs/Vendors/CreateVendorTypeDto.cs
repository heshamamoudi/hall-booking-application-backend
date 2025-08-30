namespace HallApp.Application.DTOs.Vendors;

public class CreateVendorTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresHallBooking { get; set; }
    public bool AllowsMultipleBookings { get; set; }
    public int MaxSimultaneousBookings { get; set; }
    public bool RequiresTimeSlot { get; set; }
    public int? DefaultDuration { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
