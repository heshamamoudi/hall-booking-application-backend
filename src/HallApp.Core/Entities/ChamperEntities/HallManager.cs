using System.ComponentModel.DataAnnotations;

namespace HallApp.Core.Entities.ChamperEntities;

/// <summary>
/// Represents a user who manages one or more halls.
/// Business properties (commercial registration, VAT) belong to the Hall entity.
/// </summary>
public class HallManager
{
    public int Id { get; set; }
    
    [Required]
    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties - Many-to-Many with Halls
    public List<Hall> Halls { get; set; } = new();
}
