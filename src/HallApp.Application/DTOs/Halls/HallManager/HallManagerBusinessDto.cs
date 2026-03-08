namespace HallApp.Application.DTOs.Halls.HallManager;

/// <summary>
/// DTO for HallManager - simplified link entity
/// HallManager just links AppUser to managed Halls
/// Business properties (approval, registration) are on Hall entity
/// </summary>
public class HallManagerBusinessDto
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // AppUser details for display
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
