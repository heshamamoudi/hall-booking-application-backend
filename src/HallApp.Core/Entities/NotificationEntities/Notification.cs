using System.ComponentModel.DataAnnotations;

namespace HallApp.Core.Entities.NotificationEntities;

public class Notification : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    [StringLength(50)]
    public string Type { get; set; } = "Info"; // Info, Warning, Error, Success

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // References
    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;
}
