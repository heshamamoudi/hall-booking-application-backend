using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHallNavigationProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Bookings_HallId",
                table: "Bookings",
                column: "HallId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Halls_HallId",
                table: "Bookings",
                column: "HallId",
                principalTable: "Halls",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Halls_HallId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_HallId",
                table: "Bookings");
        }
    }
}
