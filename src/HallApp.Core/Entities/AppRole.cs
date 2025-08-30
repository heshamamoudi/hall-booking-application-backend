using Microsoft.AspNetCore.Identity;

namespace HallApp.Core.Entities;

public class AppRole : IdentityRole<int>
{
    public ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>();
}
