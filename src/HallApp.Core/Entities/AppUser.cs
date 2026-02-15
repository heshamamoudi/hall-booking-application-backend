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
    public bool IsHallOrganizationManager => UserRoles.Any(ur => ur.Role.Name == "HallOrganizationManager");
    public bool IsVendorOrganizationManager => UserRoles.Any(ur => ur.Role.Name == "VendorOrganizationManager");
    public bool IsHallManager => UserRoles.Any(ur => ur.Role.Name == "HallManager");
    public bool IsRestaurantManager => UserRoles.Any(ur => ur.Role.Name == "RestaurantManager");

    // Navigation properties
    public List<Notification> Notifications { get; set; } = new List<Notification>();

    [DataType(DataType.Date)]
    public DateTime DOB { get; set; } = DateTime.UtcNow;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
    public bool Active { get; set; } = true;

    // Refresh token properties
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiryTime { get; set; } = DateTime.UtcNow;

    // Profile photo URL (stored as relative path e.g. /uploads/avatars/guid.jpg)
#nullable enable
    public string? PhotoUrl { get; set; }
#nullable restore

    // Invitation token for team member invite flow
#nullable enable
    public string? InvitationToken { get; set; }
#nullable restore
    public DateTime? InvitationTokenExpiry { get; set; }
}
