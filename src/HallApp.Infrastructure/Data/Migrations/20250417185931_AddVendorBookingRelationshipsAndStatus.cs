using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorBookingRelationshipsAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorBooking_Vendors_VendorId",
                table: "VendorBooking");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VendorBooking",
                table: "VendorBooking");

            migrationBuilder.RenameTable(
                name: "VendorBooking",
                newName: "VendorBookings");

            migrationBuilder.RenameIndex(
                name: "IX_VendorBooking_VendorId",
                table: "VendorBookings",
                newName: "IX_VendorBookings_VendorId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "VendorBookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookingId",
                table: "VendorBookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "VendorBookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_VendorBookings",
                table: "VendorBookings",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBookings_BookingId",
                table: "VendorBookings",
                column: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorBookings_Bookings_BookingId",
                table: "VendorBookings",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VendorBookings_Vendors_VendorId",
                table: "VendorBookings",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorBookings_Bookings_BookingId",
                table: "VendorBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_VendorBookings_Vendors_VendorId",
                table: "VendorBookings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VendorBookings",
                table: "VendorBookings");

            migrationBuilder.DropIndex(
                name: "IX_VendorBookings_BookingId",
                table: "VendorBookings");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "VendorBookings");

            migrationBuilder.DropColumn(
                name: "BookingId",
                table: "VendorBookings");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "VendorBookings");

            migrationBuilder.RenameTable(
                name: "VendorBookings",
                newName: "VendorBooking");

            migrationBuilder.RenameIndex(
                name: "IX_VendorBookings_VendorId",
                table: "VendorBooking",
                newName: "IX_VendorBooking_VendorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VendorBooking",
                table: "VendorBooking",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorBooking_Vendors_VendorId",
                table: "VendorBooking",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
