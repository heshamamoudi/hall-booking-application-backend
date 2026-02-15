using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActiveOrganizationPerOwnerIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add a unique filtered index to prevent more than one active organization per owner.
            // This enforces the business rule at the database level, eliminating the race condition
            // in the check-then-create pattern used by CreateOrganization.
            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OwnerId_UniqueActive",
                table: "Organizations",
                column: "OwnerId",
                unique: true,
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_OwnerId_UniqueActive",
                table: "Organizations");
        }
    }
}
