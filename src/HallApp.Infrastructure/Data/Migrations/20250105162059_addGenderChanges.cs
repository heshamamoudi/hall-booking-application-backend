using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class addGenderChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WeekEnds",
                table: "Hall",
                newName: "MaleWeekEnds");

            migrationBuilder.RenameColumn(
                name: "WeekDays",
                table: "Hall",
                newName: "MaleWeekDays");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Hall",
                newName: "MalePrice");

            migrationBuilder.RenameColumn(
                name: "Min",
                table: "Hall",
                newName: "MaleMin");

            migrationBuilder.RenameColumn(
                name: "Max",
                table: "Hall",
                newName: "MaleMax");

            migrationBuilder.RenameColumn(
                name: "Active",
                table: "Hall",
                newName: "MaleActive");

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "MediaFile",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "Hall",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FemaleActive",
                table: "Hall",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FemaleMax",
                table: "Hall",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FemaleMin",
                table: "Hall",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "FemalePrice",
                table: "Hall",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FemaleWeekDays",
                table: "Hall",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FemaleWeekEnds",
                table: "Hall",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "MediaFile");

            migrationBuilder.DropColumn(
                name: "FemaleActive",
                table: "Hall");

            migrationBuilder.DropColumn(
                name: "FemaleMax",
                table: "Hall");

            migrationBuilder.DropColumn(
                name: "FemaleMin",
                table: "Hall");

            migrationBuilder.DropColumn(
                name: "FemalePrice",
                table: "Hall");

            migrationBuilder.DropColumn(
                name: "FemaleWeekDays",
                table: "Hall");

            migrationBuilder.DropColumn(
                name: "FemaleWeekEnds",
                table: "Hall");

            migrationBuilder.RenameColumn(
                name: "MaleWeekEnds",
                table: "Hall",
                newName: "WeekEnds");

            migrationBuilder.RenameColumn(
                name: "MaleWeekDays",
                table: "Hall",
                newName: "WeekDays");

            migrationBuilder.RenameColumn(
                name: "MalePrice",
                table: "Hall",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "MaleMin",
                table: "Hall",
                newName: "Min");

            migrationBuilder.RenameColumn(
                name: "MaleMax",
                table: "Hall",
                newName: "Max");

            migrationBuilder.RenameColumn(
                name: "MaleActive",
                table: "Hall",
                newName: "Active");

            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "Hall",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
