using System.ComponentModel.DataAnnotations;
using HallApp.Application.DTOs;
using HallApp.Application.DTOs.Vendors;

namespace HallApp.Application.DTOs.Vendors.Registers;

public class RegisterUserVendorDto
{
    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string UserName { get; set; }

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; }

    [Required]
    [StringLength(50)]
    public string LastName { get; set; }

    [Required]
    [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Invalid gender")]
    public string Gender { get; set; }

    [Required]
    [Phone]
    [StringLength(15)]
    public string PhoneNumber { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; }
}

public class VendorManagerLoginDto
{
    [Required]
    [StringLength(100)]
    public string Login { get; set; }  // Can be email or username

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}

public class ChangeVendorPasswordDto
{
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; }
}

public class VendorManagerResponseDto
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Gender { get; set; }
    public string Token { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public List<VendorDto> ManagedVendors { get; set; } = new();
}

public class VendorManagerResetPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string ResetToken { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; }
}
