using HallApp.Web.Controllers.Common;
using HallApp.Core.Constants;
using HallApp.Core.Entities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Web.Controllers.Admin
{
    /// <summary>
    /// Data migration extension for AdminController.
    /// Provides one-time migration endpoints to backfill organizations for existing managers.
    /// </summary>
    public partial class AdminController
    {
        /// <summary>
        /// Preview what the organization migration would do without making changes.
        /// Call this first to review before running the actual migration.
        /// </summary>
        /// <returns>Preview report of pending migrations</returns>
        [HttpGet("data-migration/organizations/preview")]
        public async Task<ActionResult<ApiResponse<object>>> PreviewOrganizationMigration()
        {
            try
            {
                var report = await BuildMigrationReport();
                return Success((object)report, "Migration preview generated successfully. No changes were made.");
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to generate migration preview: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Execute the organization data migration for existing managers.
        /// Creates organizations for HallOrganizationManagers and VendorOrganizationManagers
        /// that do not already have one. Links existing halls/vendors to their manager's organization.
        /// This operation is idempotent -- calling it multiple times is safe.
        /// </summary>
        /// <returns>Migration result report</returns>
        [HttpPost("data-migration/organizations/execute")]
        public async Task<ActionResult<ApiResponse<object>>> ExecuteOrganizationMigration()
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = new MigrationResult();

                // Step 1: Create organizations for HallOrganizationManagers without one
                await MigrateHallOrganizationManagers(result);

                // Step 2: Create organizations for VendorOrganizationManagers without one
                await MigrateVendorOrganizationManagers(result);

                // Step 3: Link halls to their manager's organization
                await LinkHallsToOrganizations(result);

                // Step 4: Link vendors to their manager's organization
                await LinkVendorsToOrganizations(result);

                await transaction.CommitAsync();

                var report = new
                {
                    result.HallOrgManagersProcessed,
                    result.HallOrganizationsCreated,
                    result.VendorOrgManagersProcessed,
                    result.VendorOrganizationsCreated,
                    result.HallsLinked,
                    result.VendorsLinked,
                    result.Skipped,
                    result.Warnings,
                    Timestamp = DateTime.UtcNow,
                    Message = "Migration completed successfully."
                };

                return Success((object)report, "Organization migration completed successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Error<object>($"Migration failed and was rolled back: {ex.Message}", 500);
            }
        }

        private async Task<object> BuildMigrationReport()
        {
            // Find HallOrganizationManagers without organizations
            var hallOrgManagers = await _userManager.GetUsersInRoleAsync(AppRoles.HallOrganizationManager);
            var hallManagersWithoutOrg = new List<object>();
            foreach (var user in hallOrgManagers)
            {
                var existingOrg = await _context.Organizations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OwnerId == user.Id && o.IsActive);
                if (existingOrg == null)
                {
                    hallManagersWithoutOrg.Add(new
                    {
                        userId = user.Id,
                        userName = user.UserName,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        proposedOrgName = BuildOrganizationName(user, "Hall")
                    });
                }
            }

            // Find VendorOrganizationManagers without organizations
            var vendorOrgManagers = await _userManager.GetUsersInRoleAsync(AppRoles.VendorOrganizationManager);
            var vendorManagersWithoutOrg = new List<object>();
            foreach (var user in vendorOrgManagers)
            {
                var existingOrg = await _context.Organizations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OwnerId == user.Id && o.IsActive);
                if (existingOrg == null)
                {
                    vendorManagersWithoutOrg.Add(new
                    {
                        userId = user.Id,
                        userName = user.UserName,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        proposedOrgName = BuildOrganizationName(user, "Vendor")
                    });
                }
            }

            // Find halls without organizationId that have manager assignments
            var hallsWithoutOrg = await _context.Halls
                .AsNoTracking()
                .Where(h => h.OrganizationId == null)
                .Include(h => h.Managers)
                    .ThenInclude(m => m.AppUser)
                .Select(h => new
                {
                    hallId = h.ID,
                    hallName = h.Name,
                    managerCount = h.Managers.Count,
                    managers = h.Managers.Select(m => new
                    {
                        hallManagerId = m.Id,
                        appUserId = m.AppUserId,
                        userName = m.AppUser.UserName
                    }).ToList()
                })
                .ToListAsync();

            // Find vendors without organizationId that have manager assignments
            var vendorsWithoutOrg = await _context.Vendors
                .AsNoTracking()
                .Where(v => v.OrganizationId == null)
                .Include(v => v.Managers)
                    .ThenInclude(m => m.AppUser)
                .Select(v => new
                {
                    vendorId = v.Id,
                    vendorName = v.Name,
                    managerCount = v.Managers.Count,
                    managers = v.Managers.Select(m => new
                    {
                        vendorManagerId = m.Id,
                        appUserId = m.AppUserId,
                        userName = m.AppUser.UserName
                    }).ToList()
                })
                .ToListAsync();

            return new
            {
                hallOrganizationManagersWithoutOrg = hallManagersWithoutOrg,
                vendorOrganizationManagersWithoutOrg = vendorManagersWithoutOrg,
                hallsWithoutOrganization = hallsWithoutOrg,
                vendorsWithoutOrganization = vendorsWithoutOrg,
                summary = new
                {
                    hallOrgManagersToMigrate = hallManagersWithoutOrg.Count,
                    vendorOrgManagersToMigrate = vendorManagersWithoutOrg.Count,
                    hallsToLink = hallsWithoutOrg.Count(h => h.managerCount > 0),
                    vendorsToLink = vendorsWithoutOrg.Count(v => v.managerCount > 0),
                    hallsWithMultipleManagers = hallsWithoutOrg.Count(h => h.managerCount > 1),
                    vendorsWithMultipleManagers = vendorsWithoutOrg.Count(v => v.managerCount > 1)
                },
                note = "This is a preview. Call POST .../execute to apply changes."
            };
        }

        private async Task MigrateHallOrganizationManagers(MigrationResult result)
        {
            var hallOrgManagers = await _userManager.GetUsersInRoleAsync(AppRoles.HallOrganizationManager);

            foreach (var user in hallOrgManagers)
            {
                result.HallOrgManagersProcessed++;

                // Check if already has an active organization
                var existingOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.OwnerId == user.Id && o.IsActive);

                if (existingOrg != null)
                {
                    result.Skipped.Add($"HallOrganizationManager '{user.UserName}' (ID: {user.Id}) already has organization '{existingOrg.Name}' (ID: {existingOrg.Id}).");
                    continue;
                }

                var orgName = BuildOrganizationName(user, "Hall");
                var organization = new HallApp.Core.Entities.Organization
                {
                    Name = orgName,
                    Type = "HallManagement",
                    OwnerId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();

                // Add owner as member
                var ownerMember = new HallApp.Core.Entities.OrganizationMember
                {
                    OrganizationId = organization.Id,
                    AppUserId = user.Id,
                    Role = "Owner",
                    CanManageTeam = true,
                    CanCreateResources = true,
                    JoinedAt = DateTime.UtcNow
                };

                _context.OrganizationMembers.Add(ownerMember);
                await _context.SaveChangesAsync();

                result.HallOrganizationsCreated++;
            }
        }

        private async Task MigrateVendorOrganizationManagers(MigrationResult result)
        {
            var vendorOrgManagers = await _userManager.GetUsersInRoleAsync(AppRoles.VendorOrganizationManager);

            foreach (var user in vendorOrgManagers)
            {
                result.VendorOrgManagersProcessed++;

                // Check if already has an active organization
                var existingOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.OwnerId == user.Id && o.IsActive);

                if (existingOrg != null)
                {
                    result.Skipped.Add($"VendorOrganizationManager '{user.UserName}' (ID: {user.Id}) already has organization '{existingOrg.Name}' (ID: {existingOrg.Id}).");
                    continue;
                }

                var orgName = BuildOrganizationName(user, "Vendor");
                var organization = new HallApp.Core.Entities.Organization
                {
                    Name = orgName,
                    Type = "VendorManagement",
                    OwnerId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();

                // Add owner as member
                var ownerMember = new HallApp.Core.Entities.OrganizationMember
                {
                    OrganizationId = organization.Id,
                    AppUserId = user.Id,
                    Role = "Owner",
                    CanManageTeam = true,
                    CanCreateResources = true,
                    JoinedAt = DateTime.UtcNow
                };

                _context.OrganizationMembers.Add(ownerMember);
                await _context.SaveChangesAsync();

                result.VendorOrganizationsCreated++;
            }
        }

        private async Task LinkHallsToOrganizations(MigrationResult result)
        {
            // Find halls without an organization that have manager assignments (many-to-many)
            var hallsWithoutOrg = await _context.Halls
                .Where(h => h.OrganizationId == null)
                .Include(h => h.Managers)
                .ToListAsync();

            foreach (var hall in hallsWithoutOrg)
            {
                if (hall.Managers == null || hall.Managers.Count == 0)
                {
                    result.Warnings.Add($"Hall '{hall.Name}' (ID: {hall.ID}) has no managers assigned. Cannot auto-link to organization.");
                    continue;
                }

                // For halls with multiple managers, use the first manager as primary.
                // This is the safest approach -- the admin can reassign later.
                if (hall.Managers.Count > 1)
                {
                    result.Warnings.Add($"Hall '{hall.Name}' (ID: {hall.ID}) has {hall.Managers.Count} managers. Using first manager (AppUserId: {hall.Managers[0].AppUserId}) as primary.");
                }

                var primaryManager = hall.Managers[0];

                // Find the organization owned by this manager's AppUser
                var organization = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.OwnerId == primaryManager.AppUserId && o.Type == "HallManagement" && o.IsActive);

                if (organization == null)
                {
                    // The manager might not be a HallOrganizationManager -- check if they belong to any org as a member
                    var membership = await _context.OrganizationMembers
                        .Include(m => m.Organization)
                        .FirstOrDefaultAsync(m => m.AppUserId == primaryManager.AppUserId && m.Organization.Type == "HallManagement" && m.Organization.IsActive);

                    if (membership != null)
                    {
                        organization = membership.Organization;
                    }
                    else
                    {
                        result.Warnings.Add($"Hall '{hall.Name}' (ID: {hall.ID}): Manager (AppUserId: {primaryManager.AppUserId}) has no HallManagement organization. Skipping.");
                        continue;
                    }
                }

                hall.OrganizationId = organization.Id;
                result.HallsLinked++;
            }

            await _context.SaveChangesAsync();
        }

        private async Task LinkVendorsToOrganizations(MigrationResult result)
        {
            // Find vendors without an organization that have manager assignments (many-to-many)
            var vendorsWithoutOrg = await _context.Vendors
                .Where(v => v.OrganizationId == null)
                .Include(v => v.Managers)
                .ToListAsync();

            foreach (var vendor in vendorsWithoutOrg)
            {
                if (vendor.Managers == null || vendor.Managers.Count == 0)
                {
                    result.Warnings.Add($"Vendor '{vendor.Name}' (ID: {vendor.Id}) has no managers assigned. Cannot auto-link to organization.");
                    continue;
                }

                // For vendors with multiple managers, use the first manager as primary.
                if (vendor.Managers.Count > 1)
                {
                    result.Warnings.Add($"Vendor '{vendor.Name}' (ID: {vendor.Id}) has {vendor.Managers.Count} managers. Using first manager (AppUserId: {vendor.Managers[0].AppUserId}) as primary.");
                }

                var primaryManager = vendor.Managers[0];

                // Find the organization owned by this manager's AppUser
                var organization = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.OwnerId == primaryManager.AppUserId && o.Type == "VendorManagement" && o.IsActive);

                if (organization == null)
                {
                    // Check membership fallback
                    var membership = await _context.OrganizationMembers
                        .Include(m => m.Organization)
                        .FirstOrDefaultAsync(m => m.AppUserId == primaryManager.AppUserId && m.Organization.Type == "VendorManagement" && m.Organization.IsActive);

                    if (membership != null)
                    {
                        organization = membership.Organization;
                    }
                    else
                    {
                        result.Warnings.Add($"Vendor '{vendor.Name}' (ID: {vendor.Id}): Manager (AppUserId: {primaryManager.AppUserId}) has no VendorManagement organization. Skipping.");
                        continue;
                    }
                }

                vendor.OrganizationId = organization.Id;
                result.VendorsLinked++;
            }

            await _context.SaveChangesAsync();
        }

        private static string BuildOrganizationName(AppUser user, string type)
        {
            if (!string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName))
                return $"{user.FirstName} {user.LastName}'s {type} Organization";

            if (!string.IsNullOrWhiteSpace(user.FirstName))
                return $"{user.FirstName}'s {type} Organization";

            if (!string.IsNullOrWhiteSpace(user.UserName))
                return $"{user.UserName}'s {type} Organization";

            return $"{type} Organization (User {user.Id})";
        }

        /// <summary>
        /// Internal tracking class for migration results.
        /// </summary>
        private class MigrationResult
        {
            public int HallOrgManagersProcessed { get; set; }
            public int HallOrganizationsCreated { get; set; }
            public int VendorOrgManagersProcessed { get; set; }
            public int VendorOrganizationsCreated { get; set; }
            public int HallsLinked { get; set; }
            public int VendorsLinked { get; set; }
            public List<string> Skipped { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
        }
    }
}
