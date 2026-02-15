using HallApp.Core.Entities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// Service interface for team member management within organizations.
/// Handles adding/removing members, role assignments, and team queries.
/// </summary>
public interface ITeamMemberService
{
    /// <summary>
    /// Add a new team member to an organization. Creates AppUser, assigns role, and adds to organization.
    /// </summary>
    Task<OrganizationMember> AddTeamMember(int orgId, string email, string fullName, string role, string passwordOption, string password, int invitedByUserId);

    /// <summary>
    /// Remove a team member from an organization.
    /// </summary>
    Task<bool> RemoveTeamMember(int orgId, int userId);

    /// <summary>
    /// Get all team members in an organization with their assigned resources.
    /// </summary>
    Task<List<OrganizationMember>> GetTeamMembers(int orgId);

    /// <summary>
    /// Update a team member's organization role.
    /// </summary>
    Task<bool> UpdateMemberRole(int orgId, int userId, string newRole);

    /// <summary>
    /// Check if a user can manage team membership for an organization.
    /// </summary>
    Task<bool> CanManageTeam(int userId, int orgId);
}
