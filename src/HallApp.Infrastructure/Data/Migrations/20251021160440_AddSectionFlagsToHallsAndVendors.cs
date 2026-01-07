using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionFlagsToHallsAndVendors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasSpecialOffer",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasSpecialOffer",
                table: "Halls",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Halls",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "Halls",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasSpecialOffer",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "IsPremium",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "HasSpecialOffer",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "IsPremium",
                table: "Halls");
        }
    }
}
