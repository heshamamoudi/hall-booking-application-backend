using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    [Required]
    public string UserName { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; } = false;
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime DOB { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
