using Microsoft.AspNetCore.Authorization;

namespace HallApp.Web.Authorization;

/// <summary>
/// Authorization requirement that verifies a HallManager can only access
/// halls that are explicitly assigned to them. OrganizationManagers and
/// Admins bypass this check and can access any hall.
/// </summary>
public class HallAssignmentRequirement : IAuthorizationRequirement
{
}
