using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Customer;

public class CreateFavoriteDto
{
    [Required]
    public int CustomerId { get; set; }
    
    [Required]
    public int VendorId { get; set; }
    
    public int? HallId { get; set; }
}
