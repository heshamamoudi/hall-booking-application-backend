using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <summary>
    /// Data migration: Renames the existing 'HallManager' role to 'OrganizationManager'
    /// and creates a new 'HallManager' role for team members.
    ///
    /// Background:
    /// - The old 'HallManager' role was used for organization owners who create and manage halls.
    /// - The new 'OrganizationManager' role replaces it for organization owners.
    /// - A new 'HallManager' role is introduced for team members assigned to specific halls.
    ///
    /// Strategy:
    /// 1. Rename existing 'HallManager' role record to 'OrganizationManager' (preserves role ID and all user assignments).
    /// 2. Insert a new 'HallManager' role for future team member assignments.
    /// 3. All existing users who had 'HallManager' automatically become 'OrganizationManager'
    ///    because the UserRoles join table references the role ID which remains unchanged.
    ///
    /// This migration is fully reversible.
    /// </summary>
    public partial class RenameHallManagerRoleToOrganizationManager : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Rename existing 'HallManager' role to 'OrganizationManager'.
            // This preserves the role ID so all existing UserRoles FK references remain intact.
            // Users who were HallManagers (organization owners) become OrganizationManagers automatically.
            migrationBuilder.Sql(
                """
                UPDATE "Roles"
                SET "Name" = 'OrganizationManager',
                    "NormalizedName" = 'ORGANIZATIONMANAGER',
                    "ConcurrencyStamp" = gen_random_uuid()::text
                WHERE "NormalizedName" = 'HALLMANAGER';
                """);

            // Step 2: Insert a new 'HallManager' role for team members.
            // This is a completely new role record with a new ID.
            migrationBuilder.Sql(
                """
                INSERT INTO "Roles" ("Name", "NormalizedName", "ConcurrencyStamp")
                SELECT 'HallManager', 'HALLMANAGER', gen_random_uuid()::text
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Roles" WHERE "NormalizedName" = 'HALLMANAGER'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse Step 2: Remove the newly created 'HallManager' role.
            // Only delete it if no users are assigned to it (safety check).
            migrationBuilder.Sql(
                """
                DELETE FROM "Roles"
                WHERE "NormalizedName" = 'HALLMANAGER'
                AND "Id" NOT IN (
                    SELECT "RoleId" FROM "UserRoles"
                    WHERE "RoleId" = (SELECT "Id" FROM "Roles" WHERE "NormalizedName" = 'HALLMANAGER')
                );
                """);

            // Reverse Step 1: Rename 'OrganizationManager' back to 'HallManager'.
            migrationBuilder.Sql(
                """
                UPDATE "Roles"
                SET "Name" = 'HallManager',
                    "NormalizedName" = 'HALLMANAGER',
                    "ConcurrencyStamp" = gen_random_uuid()::text
                WHERE "NormalizedName" = 'ORGANIZATIONMANAGER';
                """);
        }
    }
}
