using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Vendors;

public class VendorBlockedDateDto
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public string Reason { get; set; }
}
