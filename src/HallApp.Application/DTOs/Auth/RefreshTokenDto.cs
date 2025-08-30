using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Auth
{
    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; }
    }
}
