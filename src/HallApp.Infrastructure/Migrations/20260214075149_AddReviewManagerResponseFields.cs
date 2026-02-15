using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewManagerResponseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "Reviews",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Reviews",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlagReason",
                table: "Reviews",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FlaggedByUserId",
                table: "Reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FlaggedDate",
                table: "Reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManagerResponderId",
                table: "Reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerResponse",
                table: "Reviews",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManagerResponseDate",
                table: "Reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_FlaggedByUserId",
                table: "Reviews",
                column: "FlaggedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_HallId_IsFlagged",
                table: "Reviews",
                columns: new[] { "HallId", "IsFlagged" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_HallId_Rating",
                table: "Reviews",
                columns: new[] { "HallId", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ManagerResponderId",
                table: "Reviews",
                column: "ManagerResponderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Users_FlaggedByUserId",
                table: "Reviews",
                column: "FlaggedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Users_ManagerResponderId",
                table: "Reviews",
                column: "ManagerResponderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Users_FlaggedByUserId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Users_ManagerResponderId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_FlaggedByUserId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_HallId_IsFlagged",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_HallId_Rating",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ManagerResponderId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "FlagReason",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "FlaggedByUserId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "FlaggedDate",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ManagerResponderId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ManagerResponse",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ManagerResponseDate",
                table: "Reviews");

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "Reviews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Reviews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
