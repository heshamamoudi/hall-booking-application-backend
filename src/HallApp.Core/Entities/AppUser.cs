using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.NotificationEntities;

namespace HallApp.Core.Entities;

public class AppUser : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;

    public ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>();
    public bool IsAdmin => UserRoles.Any(ur => ur.Role.Name == "Admin");
    public bool IsModerator => UserRoles.Any(ur => ur.Role.Name == "Moderator");
    public bool IsHallManager => UserRoles.Any(ur => ur.Role.Name == "HallManager");
    public bool IsRestaurantManager => UserRoles.Any(ur => ur.Role.Name == "RestaurantManager");

    // Navigation properties
    public HallManager HallManager { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new List<Notification>();

    [DataType(DataType.Date)]
    public DateTime DOB { get; set; } = DateTime.Today;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
    public bool Active { get; set; } = false;
    
    // Refresh token properties
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiryTime { get; set; }
}
