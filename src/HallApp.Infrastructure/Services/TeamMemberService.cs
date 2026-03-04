using HallApp.Core.Entities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Interfaces.IServices;
using HallApp.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HallApp.Infrastructure.Services;

/// <summary>
/// Service for managing team members within organizations.
/// Handles user creation, role assignment, and organization membership.
/// </summary>
public class TeamMemberService : ITeamMemberService
{
    private readonly DataContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<TeamMemberService> _logger;

    public TeamMemberService(
        DataContext context,
        UserManager<AppUser> userManager,
        ILogger<TeamMemberService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<OrganizationMember> AddTeamMember(
        int orgId,
        string email,
        string fullName,
        string role,
        string passwordOption,
        string password,
        int invitedByUserId)
    {
        // Validate organization exists
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == orgId && o.IsActive);

        if (organization == null)
        {
            throw new ArgumentException("Organization not found or is inactive.");
        }

        // Validate role matches organization type
        ValidateRoleForOrganization(role, organization.Type);

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            // Check if already a member of this organization
            var existingMember = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AppUserId == existingUser.Id);

            if (existingMember != null)
            {
                throw new InvalidOperationException("User is already a member of this organization.");
            }

            // Add existing user to organization (wrapped in transaction)
            return await AddExistingUserToOrganization(orgId, existingUser, role, invitedByUserId, organization.Type);
        }

        // Wrap all mutating operations in a transaction so that user creation,
        // role assignment, manager entity creation, and membership are atomic.
        // UserManager commits immediately to the database, so the transaction
        // ensures we can roll back those writes on any subsequent failure.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Create new AppUser account
            var nameParts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : fullName;
            var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

            var newUser = new AppUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            };

            // Handle password option
            if (passwordOption == "invite")
            {
                // Generate invitation token
                newUser.InvitationToken = Guid.NewGuid().ToString("N");
                newUser.InvitationTokenExpiry = DateTime.UtcNow.AddDays(7);

                // Create user with a random password (they will set it via invitation)
                var tempPassword = GenerateTemporaryPassword();
                var createResult = await _userManager.CreateAsync(newUser, tempPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create user account: {errors}");
                }
            }
            else
            {
                // 'set' - use the provided password
                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new ArgumentException("Password is required when passwordOption is 'set'.");
                }

                var createResult = await _userManager.CreateAsync(newUser, password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create user account: {errors}");
                }
            }

            // Assign the appropriate role
            var roleResult = await _userManager.AddToRoleAsync(newUser, role);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign role {role} to user: {errors}");
            }

            // Create HallManager or VendorManager entity (no intermediate SaveChanges)
            await CreateManagerEntity(newUser, organization.Type);

            // Add to organization
            var member = new OrganizationMember
            {
                OrganizationId = orgId,
                AppUserId = newUser.Id,
                Role = "Manager",
                CanManageTeam = false,
                CanCreateResources = true,
                JoinedAt = DateTime.UtcNow,
                InvitedBy = invitedByUserId
            };

            _context.OrganizationMembers.Add(member);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Team member {Email} added to organization {OrgId} with role {Role} by user {InvitedBy}",
                email, orgId, role, invitedByUserId);

            return member;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex,
                "Failed to add team member {Email} to organization {OrgId}. Transaction rolled back",
                email, orgId);
            throw;
        }
    }

    public async Task<bool> RemoveTeamMember(int orgId, int userId)
    {
        var member = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AppUserId == userId);

        if (member == null) return false;

        // Cannot remove the owner
        var organization = await _context.Organizations.FindAsync(orgId);
        if (organization != null && organization.OwnerId == userId)
        {
            throw new InvalidOperationException("Cannot remove the organization owner.");
        }

        _context.OrganizationMembers.Remove(member);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Team member {UserId} removed from organization {OrgId}", userId, orgId);

        return true;
    }

    public async Task<List<OrganizationMember>> GetTeamMembers(int orgId)
    {
        return await _context.OrganizationMembers
            .Include(m => m.AppUser)
            .Where(m => m.OrganizationId == orgId)
            .OrderByDescending(m => m.Role == "Owner")
            .ThenBy(m => m.JoinedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdateMemberRole(int orgId, int userId, string newRole)
    {
        var member = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AppUserId == userId);

        if (member == null) return false;

        member.Role = newRole;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Team member {UserId} role updated to {NewRole} in organization {OrgId}",
            userId, newRole, orgId);

        return true;
    }

    public async Task<bool> CanManageTeam(int userId, int orgId)
    {
        var member = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AppUserId == userId);

        if (member == null) return false;

        return member.CanManageTeam || member.Role == "Owner";
    }

    private void ValidateRoleForOrganization(string role, string orgType)
    {
        if (orgType == "HallManagement" && role != "HallManager")
        {
            throw new ArgumentException("Hall management organizations can only add HallManager members.");
        }

        if (orgType == "VendorManagement" && role != "VendorManager")
        {
            throw new ArgumentException("Vendor management organizations can only add VendorManager members.");
        }
    }

    private async Task<OrganizationMember> AddExistingUserToOrganization(
        int orgId, AppUser user, string role, int invitedByUserId, string orgType)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Ensure they have the correct role
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(role))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to assign role {role} to user: {errors}");
                }
            }

            // Create manager entity if not exists (no intermediate SaveChanges)
            await CreateManagerEntity(user, orgType);

            var member = new OrganizationMember
            {
                OrganizationId = orgId,
                AppUserId = user.Id,
                Role = "Manager",
                CanManageTeam = false,
                CanCreateResources = true,
                JoinedAt = DateTime.UtcNow,
                InvitedBy = invitedByUserId
            };

            _context.OrganizationMembers.Add(member);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return member;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex,
                "Failed to add existing user {UserId} to organization {OrgId}. Transaction rolled back",
                user.Id, orgId);
            throw;
        }
    }

    /// <summary>
    /// Creates the appropriate manager entity (HallManager or VendorManager) if one does not exist.
    /// This method only adds the entity to the change tracker without calling SaveChangesAsync.
    /// The caller is responsible for flushing changes within its transaction scope.
    /// </summary>
    private async Task CreateManagerEntity(AppUser user, string orgType)
    {
        if (orgType == "HallManagement")
        {
            var existingHallManager = await _context.HallManagers
                .FirstOrDefaultAsync(hm => hm.AppUserId == user.Id);

            if (existingHallManager == null)
            {
                _context.HallManagers.Add(new HallManager
                {
                    AppUserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else if (orgType == "VendorManagement")
        {
            var existingVendorManager = await _context.VendorManagers
                .FirstOrDefaultAsync(vm => vm.AppUserId == user.Id);

            if (existingVendorManager == null)
            {
                _context.VendorManagers.Add(new VendorManager
                {
                    AppUserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }

    private static string GenerateTemporaryPassword()
    {
        // Generate a strong temporary password that meets Identity requirements
        return $"Tmp{Guid.NewGuid():N}!1aA";
    }
}
