using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <summary>
    /// Data migration: Splits the 'OrganizationManager' role into two distinct variants:
    /// - 'HallOrganizationManager' (for hall organization owners)
    /// - 'VendorOrganizationManager' (for vendor organization owners)
    ///
    /// Background:
    /// - The previous migration renamed 'HallManager' to 'OrganizationManager' for hall org owners.
    /// - Now we need separate roles for hall and vendor organization owners.
    ///
    /// Strategy:
    /// 1. Rename 'OrganizationManager' to 'HallOrganizationManager' (preserves role ID and all user assignments).
    /// 2. Insert a new 'VendorOrganizationManager' role for vendor organization owners.
    ///
    /// Note: Existing VendorManager users who are actually org owners will need to be
    /// reassigned to VendorOrganizationManager via admin tooling or a separate data fix,
    /// since distinguishing org owners from team members requires business context.
    ///
    /// This migration is fully reversible.
    /// </summary>
    public partial class SplitOrgManagerIntoHallAndVendorVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Rename 'OrganizationManager' to 'HallOrganizationManager'.
            // Preserves the role ID so all existing UserRoles FK references remain intact.
            // Users who were OrganizationManagers (hall org owners) become HallOrganizationManagers.
            migrationBuilder.Sql(
                """
                UPDATE "Roles"
                SET "Name" = 'HallOrganizationManager',
                    "NormalizedName" = 'HALLORGANIZATIONMANAGER',
                    "ConcurrencyStamp" = gen_random_uuid()::text
                WHERE "NormalizedName" = 'ORGANIZATIONMANAGER';
                """);

            // Step 2: Insert a new 'VendorOrganizationManager' role for vendor org owners.
            migrationBuilder.Sql(
                """
                INSERT INTO "Roles" ("Name", "NormalizedName", "ConcurrencyStamp")
                SELECT 'VendorOrganizationManager', 'VENDORORGANIZATIONMANAGER', gen_random_uuid()::text
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Roles" WHERE "NormalizedName" = 'VENDORORGANIZATIONMANAGER'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse Step 2: Remove the 'VendorOrganizationManager' role.
            // Only delete if no users are assigned to it (safety check).
            migrationBuilder.Sql(
                """
                DELETE FROM "Roles"
                WHERE "NormalizedName" = 'VENDORORGANIZATIONMANAGER'
                AND "Id" NOT IN (
                    SELECT DISTINCT "RoleId" FROM "UserRoles"
                );
                """);

            // Reverse Step 1: Rename 'HallOrganizationManager' back to 'OrganizationManager'.
            migrationBuilder.Sql(
                """
                UPDATE "Roles"
                SET "Name" = 'OrganizationManager',
                    "NormalizedName" = 'ORGANIZATIONMANAGER',
                    "ConcurrencyStamp" = gen_random_uuid()::text
                WHERE "NormalizedName" = 'HALLORGANIZATIONMANAGER';
                """);
        }
    }
}
