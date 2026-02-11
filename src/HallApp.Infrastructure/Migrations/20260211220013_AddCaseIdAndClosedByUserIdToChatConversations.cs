using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseIdAndClosedByUserIdToChatConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CaseId",
                table: "ChatConversations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClosedByUserId",
                table: "ChatConversations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_CaseId",
                table: "ChatConversations",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_ClosedByUserId",
                table: "ChatConversations",
                column: "ClosedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatConversations_SupportCases_CaseId",
                table: "ChatConversations",
                column: "CaseId",
                principalTable: "SupportCases",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatConversations_Users_ClosedByUserId",
                table: "ChatConversations",
                column: "ClosedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatConversations_SupportCases_CaseId",
                table: "ChatConversations");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatConversations_Users_ClosedByUserId",
                table: "ChatConversations");

            migrationBuilder.DropIndex(
                name: "IX_ChatConversations_CaseId",
                table: "ChatConversations");

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
