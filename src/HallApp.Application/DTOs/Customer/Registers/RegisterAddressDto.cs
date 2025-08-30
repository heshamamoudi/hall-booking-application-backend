using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Customer.Registers;

public class RegisterAddressDto
{
    [Required]
    public string AddressName { get; set; }
    [Required]
    public string City { get; set; } // Added City field
    [Required]
    public string Street1 { get; set; } // Added Street1 field
    public string Street2 { get; set; } // Added Street2 field
    [Required]
    public int PostalCode { get; set; } // Added PostalCode field
    [Required]
    public int ZipCode { get; set; } // Added ZipCode field
    [Required]
    public int CustomerId { get; set; }
}
