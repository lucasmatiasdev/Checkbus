using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Checkbus.ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStopDurationAndBoardingPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBoardingPoint",
                table: "Stops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StopDurationMinutes",
                table: "Stops",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBoardingPoint",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "StopDurationMinutes",
                table: "Stops");
        }
    }
}
