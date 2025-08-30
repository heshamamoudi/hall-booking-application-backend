using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Auth
{
    public class LoginDto
    {
        [Required]
        public string Login { get; set; } = string.Empty; // Can be email or username

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
