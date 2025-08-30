using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class serviceGen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MalePrice",
                table: "Hall",
                newName: "BothWeekEnds");

            migrationBuilder.RenameColumn(
                name: "FemalePrice",
                table: "Hall",
                newName: "BothWeekDays");

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Service",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Service");

            migrationBuilder.RenameColumn(
                name: "BothWeekEnds",
                table: "Hall",
                newName: "MalePrice");

            migrationBuilder.RenameColumn(
                name: "BothWeekDays",
                table: "Hall",
                newName: "FemalePrice");
        }
    }
}
