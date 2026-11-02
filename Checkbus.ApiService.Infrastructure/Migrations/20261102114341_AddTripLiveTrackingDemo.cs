using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Checkbus.ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTripLiveTrackingDemo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CurrentLatitude",
                table: "Trips",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLongitude",
                table: "Trips",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LiveTrackingEnabled",
                table: "Trips",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LocationUpdatedAt",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLatitude",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "CurrentLongitude",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "LiveTrackingEnabled",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "LocationUpdatedAt",
                table: "Trips");
        }
    }
}
