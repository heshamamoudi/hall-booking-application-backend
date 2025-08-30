namespace HallApp.Application.DTOs.Auth;

/// <summary>
/// Pure identity/auth DTO for AppUser
/// Used for authentication, profile updates, user management
/// </summary>
public class UserProfileDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DOB { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool Active { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    
    // Role information
    public List<string> Roles { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool IsModerator { get; set; }
    public bool IsHallManager { get; set; }
}
