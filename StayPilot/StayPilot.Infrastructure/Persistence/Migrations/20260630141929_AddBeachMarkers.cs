using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBeachMarkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DistanceToBeachMethod",
                table: "PropertyListings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NearestBeachMarkerId",
                table: "PropertyListings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NearestBeachName",
                table: "PropertyListings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerM2",
                table: "ListingSnapshots",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "BeachMarkers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OsmId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeachMarkers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListings_NearestBeachMarkerId",
                table: "PropertyListings",
                column: "NearestBeachMarkerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyListings_BeachMarkers_NearestBeachMarkerId",
                table: "PropertyListings",
                column: "NearestBeachMarkerId",
                principalTable: "BeachMarkers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PropertyListings_BeachMarkers_NearestBeachMarkerId",
                table: "PropertyListings");

            migrationBuilder.DropTable(
                name: "BeachMarkers");

            migrationBuilder.DropIndex(
                name: "IX_PropertyListings_NearestBeachMarkerId",
                table: "PropertyListings");

            migrationBuilder.DropColumn(
                name: "DistanceToBeachMethod",
                table: "PropertyListings");

            migrationBuilder.DropColumn(
                name: "NearestBeachMarkerId",
                table: "PropertyListings");

            migrationBuilder.DropColumn(
                name: "NearestBeachName",
                table: "PropertyListings");

            migrationBuilder.DropColumn(
                name: "PricePerM2",
                table: "ListingSnapshots");
        }
    }
}
