using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyManagerEntities_MoveBusinessPropertiesToParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VendorManagers_CommercialRegistrationNumber",
                table: "VendorManagers");

            migrationBuilder.DropIndex(
                name: "IX_VendorManagers_VatNumber",
                table: "VendorManagers");

            migrationBuilder.DropIndex(
                name: "IX_HallManagers_CommercialRegistrationNumber",
                table: "HallManagers");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "VendorManagers");

            migrationBuilder.DropColumn(
                name: "CommercialRegistrationNumber",
                table: "VendorManagers");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "VendorManagers");

            migrationBuilder.DropColumn(
                name: "VatNumber",
                table: "VendorManagers");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "HallManagers");

            migrationBuilder.DropColumn(
                name: "CommercialRegistrationNumber",
                table: "HallManagers");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "HallManagers");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "HallManagers");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Vendors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommercialRegistrationNumber",
                table: "Vendors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VatNumber",
                table: "Vendors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Halls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Halls",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "CommercialRegistrationNumber",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "VatNumber",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Halls");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "VendorManagers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommercialRegistrationNumber",
                table: "VendorManagers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "VendorManagers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VatNumber",
                table: "VendorManagers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "HallManagers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommercialRegistrationNumber",
                table: "HallManagers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "HallManagers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "HallManagers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_VendorManagers_CommercialRegistrationNumber",
                table: "VendorManagers",
                column: "CommercialRegistrationNumber",
                unique: true,
                filter: "[CommercialRegistrationNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendorManagers_VatNumber",
                table: "VendorManagers",
                column: "VatNumber",
                unique: true,
                filter: "[VatNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HallManagers_CommercialRegistrationNumber",
                table: "HallManagers",
                column: "CommercialRegistrationNumber",
                unique: true,
                filter: "[CommercialRegistrationNumber] IS NOT NULL");
        }
    }
}
