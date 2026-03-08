using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Customer.Registers;

public class RegisterUserCustomerDto
{
    [Required]
    public string UserName { get; set; } = string.Empty;
    [Required]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    public string LastName { get; set; } = string.Empty;
    [Required]
    public string Gender { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;

    //public RegisterCustomerDto Customer { get; set; }
}
