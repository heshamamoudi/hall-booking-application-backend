namespace HallApp.Application.DTOs.HallManager;

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
}
