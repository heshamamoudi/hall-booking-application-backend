namespace HallApp.Core.Entities;

/// <summary>
/// Represents a member of an organization. Members can be the owner, a manager, or a regular member.
/// For hall organizations, members are users with the HallManager role.
/// For vendor organizations, members are users with the VendorManager role.
/// </summary>
public class OrganizationMember
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// The AppUser who is a member of this organization.
    /// Can be a HallManager or VendorManager depending on the organization type.
    /// </summary>
    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    /// <summary>
    /// Role within the organization: 'Owner', 'Manager', or 'Member'.
    /// </summary>
    public string Role { get; set; } = "Member";

    /// <summary>
    /// Whether this member can manage team membership (invite/remove members).
    /// Typically true only for the Owner.
    /// </summary>
    public bool CanManageTeam { get; set; } = false;

    /// <summary>
    /// Whether this member can create new halls or vendors within the organization.
    /// </summary>
    public bool CanCreateResources { get; set; } = true;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The AppUserId of the user who invited this member. Null for the organization owner.
    /// </summary>
    public int? InvitedBy { get; set; }
    public AppUser InvitedByUser { get; set; }
}
