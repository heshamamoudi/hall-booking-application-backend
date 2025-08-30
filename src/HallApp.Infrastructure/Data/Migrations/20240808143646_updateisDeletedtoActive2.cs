using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class updateisDeletedtoActive2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Users",
                newName: "Active");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "HallManager",
                newName: "Active");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Hall",
                newName: "Active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Active",
                table: "Users",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "Active",
                table: "HallManager",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "Active",
                table: "Hall",
                newName: "IsDeleted");
        }
    }
}
