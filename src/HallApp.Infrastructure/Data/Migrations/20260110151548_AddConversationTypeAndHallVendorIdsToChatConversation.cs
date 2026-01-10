using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationTypeAndHallVendorIdsToChatConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConversationType",
                table: "ChatConversations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HallId",
                table: "ChatConversations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VendorId",
                table: "ChatConversations",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConversationType",
                table: "ChatConversations");

            migrationBuilder.DropColumn(
                name: "HallId",
                table: "ChatConversations");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "ChatConversations");
        }
    }
}
