using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixHallAndVendorSchemaIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Vendors_VendorId1",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_VendorLocations_Vendors_VendorId1",
                table: "VendorLocations");

            migrationBuilder.DropIndex(
                name: "IX_VendorLocations_VendorId1",
                table: "VendorLocations");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_VendorId1",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "VendorId1",
                table: "VendorLocations");

            migrationBuilder.DropColumn(
                name: "VendorId1",
                table: "Reviews");

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "Hall",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                table: "Hall");

            migrationBuilder.AddColumn<int>(
                name: "VendorId1",
                table: "VendorLocations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VendorId1",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorLocations_VendorId1",
                table: "VendorLocations",
                column: "VendorId1");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_VendorId1",
                table: "Reviews",
                column: "VendorId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Vendors_VendorId1",
                table: "Reviews",
                column: "VendorId1",
                principalTable: "Vendors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorLocations_Vendors_VendorId1",
                table: "VendorLocations",
                column: "VendorId1",
                principalTable: "Vendors",
                principalColumn: "Id");
        }
    }
}
