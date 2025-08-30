#nullable enable
using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Vendors;

public class VendorBusinessHourDto
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; }
    public bool IsOpen => !IsClosed;
    public string? SpecialNote { get; set; }
}

public class CreateVendorBusinessHourDto
{
    [Required]
    public int VendorId { get; set; }
    
    [Required]
    public DayOfWeek DayOfWeek { get; set; }
    
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; }
    
    [StringLength(200)]
    public string? SpecialNote { get; set; }
}
