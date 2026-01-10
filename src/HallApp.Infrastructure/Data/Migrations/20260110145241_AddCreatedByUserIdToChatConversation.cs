using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByUserIdToChatConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "ChatConversations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "ChatConversations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "ChatConversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_CreatedById",
                table: "ChatConversations",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatConversations_Users_CreatedById",
                table: "ChatConversations",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatConversations_Users_CreatedById",
                table: "ChatConversations");

            migrationBuilder.DropIndex(
                name: "IX_ChatConversations_CreatedById",
                table: "ChatConversations");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "ChatConversations");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ChatConversations");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "ChatConversations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
