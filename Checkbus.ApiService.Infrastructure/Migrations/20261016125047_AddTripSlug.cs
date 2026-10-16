using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Checkbus.ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTripSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trips_OrganizationId",
                table: "Trips");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Trips",
                type: "text",
                nullable: true);

            // Backfill existing rows with a unique value per row (the Id itself) before
            // enforcing NOT NULL + the unique index below — a single shared default would
            // collide across trips in the same organization.
            migrationBuilder.Sql(@"UPDATE ""Trips"" SET ""Slug"" = ""Id""::text WHERE ""Slug"" IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Trips",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_OrganizationId_Slug",
                table: "Trips",
                columns: new[] { "OrganizationId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trips_OrganizationId_Slug",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Trips");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_OrganizationId",
                table: "Trips",
                column: "OrganizationId");
        }
    }
}
