using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanySignage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScreenDisplayMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayMode",
                table: "Screens",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Fill");

            migrationBuilder.AddColumn<string>(
                name: "Orientation",
                table: "Screens",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Landscape");

            migrationBuilder.AddColumn<int>(
                name: "ScreenHeight",
                table: "Screens",
                type: "int",
                nullable: false,
                defaultValue: 1080);

            migrationBuilder.AddColumn<int>(
                name: "ScreenWidth",
                table: "Screens",
                type: "int",
                nullable: false,
                defaultValue: 1920);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayMode",
                table: "Screens");

            migrationBuilder.DropColumn(
                name: "Orientation",
                table: "Screens");

            migrationBuilder.DropColumn(
                name: "ScreenHeight",
                table: "Screens");

            migrationBuilder.DropColumn(
                name: "ScreenWidth",
                table: "Screens");
        }
    }
}
