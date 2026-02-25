using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CriticalAuditFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Created",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Updated",
                table: "Bookings");

            migrationBuilder.CreateSequence<int>(
                name: "invoice_number_seq");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Invoices",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Bookings",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "BookingPackages",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_HallId_InvoiceDate",
                table: "Invoices",
                columns: new[] { "HallId", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_HallId_PaymentStatus",
                table: "Invoices",
                columns: new[] { "HallId", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceDate",
                table: "Invoices",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_IsCancelled",
                table: "Invoices",
                column: "IsCancelled");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PaymentStatus",
                table: "Invoices",
                column: "PaymentStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_HallId_InvoiceDate",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_HallId_PaymentStatus",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceDate",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_IsCancelled",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PaymentStatus",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Bookings");

            migrationBuilder.DropSequence(
                name: "invoice_number_seq");

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "Updated",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "BookingPackages",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}
