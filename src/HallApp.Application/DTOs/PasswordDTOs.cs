using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs;

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; }

    [Required]
    public string NewPassword { get; set; }
}

public class ResetPasswordRequestDto
{
    public string Email { get; set; }
}

public class ResetPasswordDto
{
    public string Email { get; set; }
    public string Token { get; set; }
    public string NewPassword { get; set; }
}
