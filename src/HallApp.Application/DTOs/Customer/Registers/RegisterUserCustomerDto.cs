using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Customer.Registers;

public class RegisterUserCustomerDto
{
    [Required]
    public string UserName { get; set; }
    [Required]
    public string FirstName { get; set; }
    [Required]
    public string LastName { get; set; }
    [Required]
    public string Gender { get; set; }
    
    [Required]
    [Phone]
    public string PhoneNumber { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }

    //public RegisterCustomerDto Customer { get; set; }
}
