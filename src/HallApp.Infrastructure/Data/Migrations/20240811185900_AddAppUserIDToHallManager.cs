using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUserIDToHallManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                table: "HallManager");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "HallManager");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "HallManager");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "HallManager");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "HallManager");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "HallManager");

            migrationBuilder.AddColumn<int>(
                name: "AppUserID",
                table: "HallManager",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_HallManager_AppUserID",
                table: "HallManager",
                column: "AppUserID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HallManager_Users_AppUserID",
                table: "HallManager",
                column: "AppUserID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HallManager_Users_AppUserID",
                table: "HallManager");

            migrationBuilder.DropIndex(
                name: "IX_HallManager_AppUserID",
                table: "HallManager");

            migrationBuilder.DropColumn(
                name: "AppUserID",
                table: "HallManager");

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "HallManager",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "HallManager",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "HallManager",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "HallManager",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "HallManager",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "HallManager",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
