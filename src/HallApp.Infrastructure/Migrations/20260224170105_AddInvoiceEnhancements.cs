using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRegenerated",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "Invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundDate",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundMethod",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReason",
                table: "Invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefundedBy",
                table: "Invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegeneratedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegenerationCount",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRegenerated",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RefundDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RefundMethod",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RefundReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RefundedBy",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RegeneratedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RegenerationCount",
                table: "Invoices");
        }
    }
}
