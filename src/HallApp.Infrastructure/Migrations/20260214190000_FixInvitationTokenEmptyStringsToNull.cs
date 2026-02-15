using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <summary>
    /// Data migration: Updates existing empty-string InvitationToken values to NULL.
    ///
    /// Background:
    /// - The InvitationToken property in AppUser defaulted to string.Empty ("") in the C# entity.
    /// - The database column is already nullable (text, nullable: true).
    /// - Any users created with the 'set' password option would have an empty string instead of NULL.
    /// - This creates a mismatch: code checking for null would miss these records.
    ///
    /// Fix:
    /// - The C# property has been changed to string? with a null default.
    /// - This migration cleans up any existing empty strings in the database.
    ///
    /// This migration is fully reversible (Down restores empty strings).
    /// </summary>
    public partial class FixInvitationTokenEmptyStringsToNull : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update any empty-string InvitationToken values to NULL
            migrationBuilder.Sql(
                """
                UPDATE "Users"
                SET "InvitationToken" = NULL
                WHERE "InvitationToken" = '';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore NULL InvitationToken values to empty string
            // (only where InvitationTokenExpiry is also NULL, indicating they were never invited)
            migrationBuilder.Sql(
                """
                UPDATE "Users"
                SET "InvitationToken" = ''
                WHERE "InvitationToken" IS NULL
                  AND "InvitationTokenExpiry" IS NULL;
                """);
        }
    }
}
