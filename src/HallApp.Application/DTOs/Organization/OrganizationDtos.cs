using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Organization;

/// <summary>
/// DTO for adding a new team member to an organization.
/// </summary>
public class AddTeamMemberDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [MinLength(2, ErrorMessage = "Full name must be at least 2 characters.")]
    [MaxLength(200, ErrorMessage = "Full name cannot exceed 200 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [RegularExpression("^(HallManager|VendorManager)$", ErrorMessage = "Role must be 'HallManager' or 'VendorManager'.")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 'set' = set password directly, 'invite' = generate invitation token.
    /// </summary>
    [Required(ErrorMessage = "Password option is required.")]
    [RegularExpression("^(set|invite)$", ErrorMessage = "Password option must be 'set' or 'invite'.")]
    public string PasswordOption { get; set; } = "set";

    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO representing a team member within an organization.
/// </summary>
public class TeamMemberDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public bool CanManageTeam { get; set; }
    public List<AssignedResourceDto> AssignedHalls { get; set; } = new();
    public List<AssignedResourceDto> AssignedVendors { get; set; } = new();
}

/// <summary>
/// DTO for a resource (hall or vendor) assigned to a team member.
/// </summary>
public class AssignedResourceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// DTO representing organization details including business registration info.
/// </summary>
public class OrganizationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int ResourceCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    // Business Registration
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;

    // Legal Address
    public string BuildingNumber { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "SA";

    // Contact Info
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    // Bank Account
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankIban { get; set; } = string.Empty;

    // Commission
    public decimal? CommissionRate { get; set; }

    // Metadata
    public DateTime? UpdatedAt { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

/// <summary>
/// DTO for updating organization details including business registration info.
/// All fields are optional -- only non-null fields will be applied.
/// </summary>
public class UpdateOrganizationDto
{
    public string Name { get; set; } = string.Empty;

    // Business Registration
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;

    // Legal Address
    public string BuildingNumber { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;

    // Contact Info
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    // Bank Account
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankIban { get; set; } = string.Empty;

    // Commission (Admin only - null means use platform default)
    [Range(0, 1)]
    public decimal? CommissionRate { get; set; }
}

/// <summary>
/// DTO for assigning a resource (hall or vendor) to a specific manager.
/// </summary>
public class AssignResourceDto
{
    public int ManagerId { get; set; }
}

/// <summary>
/// DTO for updating a team member's role.
/// </summary>
public class UpdateMemberRoleDto
{
    public string NewRole { get; set; } = string.Empty;
}

/// <summary>
/// DTO for organization-level statistics including halls, bookings, revenue, and team members.
/// </summary>
public class OrganizationStatisticsDto
{
    public int TotalHalls { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int ActiveTeamMembers { get; set; }
}
