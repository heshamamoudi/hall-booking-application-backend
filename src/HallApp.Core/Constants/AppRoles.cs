namespace HallApp.Core.Constants;

/// <summary>
/// Centralized role name constants used throughout the application.
/// Eliminates magic strings and ensures consistency across controllers,
/// services, and authorization policies.
/// </summary>
public static class AppRoles
{
    /// <summary>
    /// Platform administrator with full system access.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Content moderator with elevated review and management capabilities.
    /// </summary>
    public const string Moderator = "Moderator";

    /// <summary>
    /// Hall organization owner who creates and manages halls and hall team members.
    /// Previously called "OrganizationManager" - split for clarity between hall and vendor orgs.
    /// </summary>
    public const string HallOrganizationManager = "HallOrganizationManager";

    /// <summary>
    /// Team member assigned to manage specific halls within a hall organization.
    /// Can only access and manage halls explicitly assigned to them.
    /// </summary>
    public const string HallManager = "HallManager";

    /// <summary>
    /// Vendor organization owner who creates and manages vendors and vendor team members.
    /// Previously called "VendorManager" for org owners - split for clarity.
    /// </summary>
    public const string VendorOrganizationManager = "VendorOrganizationManager";

    /// <summary>
    /// Team member assigned to manage specific vendors within a vendor organization.
    /// Can only access and manage vendors explicitly assigned to them.
    /// </summary>
    public const string VendorManager = "VendorManager";

    /// <summary>
    /// End user who books halls and vendor services.
    /// </summary>
    public const string Customer = "Customer";

    /// <summary>
    /// Hall employee with limited operational access.
    /// </summary>
    public const string HallEmployee = "HallEmployee";

    /// <summary>
    /// Hall supervisor with mid-level management capabilities.
    /// </summary>
    public const string HallSuperVisor = "HallSuperVisor";

    // --- Composite role strings for [Authorize(Roles = "...")] attributes ---

    /// <summary>
    /// Either hall or vendor organization manager (generic organization-level management).
    /// </summary>
    public const string HallOrVendorOrgManager = $"{HallOrganizationManager},{VendorOrganizationManager}";

    /// <summary>
    /// Admin or any organization manager (hall or vendor).
    /// </summary>
    public const string AdminOrAnyOrgManager = $"{Admin},{HallOrganizationManager},{VendorOrganizationManager}";

    /// <summary>
    /// Admin or HallOrganizationManager (hall organization-level management).
    /// </summary>
    public const string AdminOrHallOrgManager = $"{Admin},{HallOrganizationManager}";

    /// <summary>
    /// Admin or VendorOrganizationManager (vendor organization-level management).
    /// </summary>
    public const string AdminOrVendorOrgManager = $"{Admin},{VendorOrganizationManager}";

    /// <summary>
    /// Admin, HallOrganizationManager, or HallManager (anyone who can work with halls).
    /// </summary>
    public const string AdminOrAnyHallRole = $"{Admin},{HallOrganizationManager},{HallManager}";

    /// <summary>
    /// Admin, VendorOrganizationManager, or VendorManager (anyone who can work with vendors).
    /// </summary>
    public const string AdminOrAnyVendorRole = $"{Admin},{VendorOrganizationManager},{VendorManager}";

    /// <summary>
    /// All manager-level roles that can participate in chat and approvals.
    /// </summary>
    public const string AllManagerRoles = $"{HallOrganizationManager},{HallManager},{VendorOrganizationManager},{VendorManager}";
}
