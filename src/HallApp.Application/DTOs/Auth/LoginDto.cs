using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Auth;

public class LoginDto
{
    [Required(ErrorMessage = "Username or email is required")]
    [RegularExpression(@"^[a-zA-Z0-9._@-]+$", ErrorMessage = "Only alphanumeric characters, dots, @, underscores and hyphens allowed")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Login must be between 3 and 100 characters")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    public string Password { get; set; } = string.Empty;
}
