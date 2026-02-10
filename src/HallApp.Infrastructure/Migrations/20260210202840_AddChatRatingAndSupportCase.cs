using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatRatingAndSupportCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CaseId",
                table: "ChatConversations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClosedByUserId",
                table: "ChatConversations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatRatings_ChatConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatRatings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    BookingId = table.Column<int>(type: "int", nullable: true),
                    HallId = table.Column<int>(type: "int", nullable: true),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    ConversationId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportCases_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportCases_ChatConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportCases_Halls_HallId",
                        column: x => x.HallId,
                        principalTable: "Halls",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportCases_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportCases_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_ClosedByUserId",
                table: "ChatConversations",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRatings_ConversationId",
                table: "ChatRatings",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRatings_ConversationId_UserId",
                table: "ChatRatings",
                columns: new[] { "ConversationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatRatings_UserId",
                table: "ChatRatings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportCases_BookingId",
                table: "SupportCases",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportCases_CaseNumber",
                table: "SupportCases",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportCases_ConversationId",
                table: "SupportCases",
                column: "ConversationId",
                unique: true,
                filter: "[ConversationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupportCases_CreatedAt",
                table: "SupportCases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportCases_CreatedByUserId",
                table: "SupportCases",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportCases_HallId",
                table: "SupportCases",
                column: "HallId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportCases_Status",
                table: "SupportCases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupportCases_VendorId",
                table: "SupportCases",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatConversations_Users_ClosedByUserId",
                table: "ChatConversations",
                column: "ClosedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatConversations_Users_ClosedByUserId",
                table: "ChatConversations");

            migrationBuilder.DropTable(
                name: "ChatRatings");

            migrationBuilder.DropTable(
                name: "SupportCases");

            migrationBuilder.DropIndex(
                name: "IX_ChatConversations_ClosedByUserId",
                table: "ChatConversations");

            migrationBuilder.DropColumn(
                name: "CaseId",
                table: "ChatConversations");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "ChatConversations");
        }
    }
}
