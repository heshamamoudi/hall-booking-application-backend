using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Auth
{
    // Public registration - Customer only
    public class CustomerRegisterDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public string? Gender { get; set; }

        public DateTime? DOB { get; set; }
    }

    // Admin-only user creation with role
    public class CreateUserDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public string? Gender { get; set; }

        public DateTime? DOB { get; set; }

        [Required]
        [RegularExpression("^(Admin|HallManager)$", ErrorMessage = "Role must be Admin or HallManager")]
        public string Role { get; set; } = string.Empty;
    }
}
