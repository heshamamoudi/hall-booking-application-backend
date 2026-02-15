using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <summary>
    /// Data migration: Links orphaned halls and vendors to their respective organizations.
    ///
    /// Background:
    /// - Halls and vendors created before the organization feature was introduced have
    ///   OrganizationId = NULL, making them invisible on organization pages.
    /// - These resources typically have an assigned manager (AssignedToHallManagerId or
    ///   AssignedToVendorManagerId) who belongs to an organization via OrganizationMembers.
    ///
    /// Strategy:
    /// - Pre-validates for ambiguous cases where a manager belongs to multiple organizations.
    /// - Managers with multiple organization memberships are SKIPPED and logged via RAISE WARNING
    ///   so they can be resolved manually, preventing non-deterministic assignments.
    /// - For unambiguous managers (single org membership), links halls/vendors to the organization.
    /// - Uses DISTINCT ON with ORDER BY to guarantee deterministic results even if future
    ///   schema changes introduce duplicates.
    /// - Resources with no assigned manager are left as NULL (system resources, admin assigns later).
    ///
    /// This migration is reversible: Down sets the OrganizationId back to NULL for any
    /// resources that were linked by this migration, identified by the same join condition.
    /// </summary>
    public partial class LinkOrphanedResourcesToOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // STEP 1: Pre-validate for HallManagers with multiple organization memberships.
            // These are skipped to prevent non-deterministic linking.
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    ambiguous_count integer;
                BEGIN
                    SELECT COUNT(*) INTO ambiguous_count
                    FROM (
                        SELECT hm."Id", COUNT(DISTINCT om."OrganizationId") AS org_count
                        FROM "HallManagers" hm
                        INNER JOIN "OrganizationMembers" om ON hm."AppUserId" = om."AppUserId"
                        WHERE hm."AppUserId" IN (
                            SELECT DISTINCT h."AssignedToHallManagerId"
                            FROM "Halls" h
                            WHERE h."OrganizationId" IS NULL
                              AND h."AssignedToHallManagerId" IS NOT NULL
                        )
                        GROUP BY hm."Id"
                        HAVING COUNT(DISTINCT om."OrganizationId") > 1
                    ) ambiguous;

                    IF ambiguous_count > 0 THEN
                        RAISE WARNING 'Found % HallManagers with multiple organization memberships. These will be skipped to prevent incorrect linking.', ambiguous_count;
                    END IF;
                END $$;
                """);

            // STEP 2: Link orphaned halls to organizations via their assigned hall manager.
            // Uses DISTINCT ON to guarantee deterministic results (picks first OrganizationId ASC).
            // Excludes managers belonging to multiple organizations to prevent wrong assignments.
            migrationBuilder.Sql(
                """
                UPDATE "Halls" h
                SET "OrganizationId" = subquery."OrganizationId"
                FROM (
                    SELECT DISTINCT ON (hm."Id")
                        hm."Id" AS "ManagerId",
                        om."OrganizationId"
                    FROM "HallManagers" hm
                    INNER JOIN "OrganizationMembers" om ON hm."AppUserId" = om."AppUserId"
                    WHERE hm."Id" NOT IN (
                        SELECT hm2."Id"
                        FROM "HallManagers" hm2
                        INNER JOIN "OrganizationMembers" om2 ON hm2."AppUserId" = om2."AppUserId"
                        GROUP BY hm2."Id"
                        HAVING COUNT(DISTINCT om2."OrganizationId") > 1
                    )
                    ORDER BY hm."Id", om."OrganizationId" ASC
                ) subquery
                WHERE h."OrganizationId" IS NULL
                  AND h."AssignedToHallManagerId" = subquery."ManagerId";
                """);

            // STEP 3: Pre-validate for VendorManagers with multiple organization memberships.
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    ambiguous_count integer;
                BEGIN
                    SELECT COUNT(*) INTO ambiguous_count
                    FROM (
                        SELECT vm."Id", COUNT(DISTINCT om."OrganizationId") AS org_count
                        FROM "VendorManagers" vm
                        INNER JOIN "OrganizationMembers" om ON vm."AppUserId" = om."AppUserId"
                        WHERE vm."Id" IN (
                            SELECT DISTINCT v."AssignedToVendorManagerId"
                            FROM "Vendors" v
                            WHERE v."OrganizationId" IS NULL
                              AND v."AssignedToVendorManagerId" IS NOT NULL
                        )
                        GROUP BY vm."Id"
                        HAVING COUNT(DISTINCT om."OrganizationId") > 1
                    ) ambiguous;

                    IF ambiguous_count > 0 THEN
                        RAISE WARNING 'Found % VendorManagers with multiple organization memberships. These will be skipped.', ambiguous_count;
                    END IF;
                END $$;
                """);

            // STEP 4: Link orphaned vendors to organizations via their assigned vendor manager.
            // Same deterministic approach: DISTINCT ON + exclusion of ambiguous managers.
            migrationBuilder.Sql(
                """
                UPDATE "Vendors" v
                SET "OrganizationId" = subquery."OrganizationId"
                FROM (
                    SELECT DISTINCT ON (vm."Id")
                        vm."Id" AS "ManagerId",
                        om."OrganizationId"
                    FROM "VendorManagers" vm
                    INNER JOIN "OrganizationMembers" om ON vm."AppUserId" = om."AppUserId"
                    WHERE vm."Id" NOT IN (
                        SELECT vm2."Id"
                        FROM "VendorManagers" vm2
                        INNER JOIN "OrganizationMembers" om2 ON vm2."AppUserId" = om2."AppUserId"
                        GROUP BY vm2."Id"
                        HAVING COUNT(DISTINCT om2."OrganizationId") > 1
                    )
                    ORDER BY vm."Id", om."OrganizationId" ASC
                ) subquery
                WHERE v."OrganizationId" IS NULL
                  AND v."AssignedToVendorManagerId" = subquery."ManagerId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Set OrganizationId back to NULL for halls that were linked
            // by this migration (i.e., those whose OrganizationId matches the join result).
            migrationBuilder.Sql(
                """
                UPDATE "Halls" h
                SET "OrganizationId" = NULL
                FROM (
                    SELECT hm."Id" AS "HallManagerId", om."OrganizationId"
                    FROM "HallManagers" hm
                    INNER JOIN "OrganizationMembers" om ON hm."AppUserId" = om."AppUserId"
                ) sub
                WHERE h."AssignedToHallManagerId" = sub."HallManagerId"
                  AND h."OrganizationId" = sub."OrganizationId";
                """);

            // Rollback: Set OrganizationId back to NULL for vendors that were linked.
            migrationBuilder.Sql(
                """
                UPDATE "Vendors" v
                SET "OrganizationId" = NULL
                FROM (
                    SELECT vm."Id" AS "VendorManagerId", om."OrganizationId"
                    FROM "VendorManagers" vm
                    INNER JOIN "OrganizationMembers" om ON vm."AppUserId" = om."AppUserId"
                ) sub
                WHERE v."AssignedToVendorManagerId" = sub."VendorManagerId"
                  AND v."OrganizationId" = sub."OrganizationId";
                """);
        }
    }
}
