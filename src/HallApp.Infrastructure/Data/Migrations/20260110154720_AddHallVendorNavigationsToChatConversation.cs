using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHallVendorNavigationsToChatConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_HallId",
                table: "ChatConversations",
                column: "HallId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_VendorId",
                table: "ChatConversations",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatConversations_Halls_HallId",
                table: "ChatConversations",
                column: "HallId",
                principalTable: "Halls",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatConversations_Vendors_VendorId",
                table: "ChatConversations",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatConversations_Halls_HallId",
                table: "ChatConversations");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatConversations_Vendors_VendorId",
                table: "ChatConversations");

            migrationBuilder.DropIndex(
                name: "IX_ChatConversations_HallId",
                table: "ChatConversations");

            migrationBuilder.DropIndex(
                name: "IX_ChatConversations_VendorId",
                table: "ChatConversations");
        }
    }
}
