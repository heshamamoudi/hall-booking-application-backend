using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationAndTeamManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedToVendorManagerId",
                table: "Vendors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Vendors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvitationToken",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvitationTokenExpiry",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedToHallManagerId",
                table: "Halls",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Halls",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organizations_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    AppUserId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CanManageTeam = table.Column<bool>(type: "boolean", nullable: false),
                    CanCreateResources = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InvitedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Users_InvitedBy",
                        column: x => x.InvitedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_AssignedToVendorManagerId",
                table: "Vendors",
                column: "AssignedToVendorManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_OrganizationId",
                table: "Vendors",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Halls_AssignedToHallManagerId",
                table: "Halls",
                column: "AssignedToHallManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Halls_OrganizationId",
                table: "Halls",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_AppUserId",
                table: "OrganizationMembers",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_InvitedBy",
                table: "OrganizationMembers",
                column: "InvitedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_OrganizationId_AppUserId",
                table: "OrganizationMembers",
                columns: new[] { "OrganizationId", "AppUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OwnerId",
                table: "Organizations",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Type",
                table: "Organizations",
                column: "Type");

            migrationBuilder.AddForeignKey(
                name: "FK_Halls_HallManagers_AssignedToHallManagerId",
                table: "Halls",
                column: "AssignedToHallManagerId",
                principalTable: "HallManagers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Halls_Organizations_OrganizationId",
                table: "Halls",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendors_Organizations_OrganizationId",
                table: "Vendors",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendors_VendorManagers_AssignedToVendorManagerId",
                table: "Vendors",
                column: "AssignedToVendorManagerId",
                principalTable: "VendorManagers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Halls_HallManagers_AssignedToHallManagerId",
                table: "Halls");

            migrationBuilder.DropForeignKey(
                name: "FK_Halls_Organizations_OrganizationId",
                table: "Halls");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendors_Organizations_OrganizationId",
                table: "Vendors");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendors_VendorManagers_AssignedToVendorManagerId",
                table: "Vendors");

            migrationBuilder.DropTable(
                name: "OrganizationMembers");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_AssignedToVendorManagerId",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_OrganizationId",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Halls_AssignedToHallManagerId",
                table: "Halls");

            migrationBuilder.DropIndex(
                name: "IX_Halls_OrganizationId",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "AssignedToVendorManagerId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "InvitationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InvitationTokenExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedToHallManagerId",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Halls");
        }
    }
}
