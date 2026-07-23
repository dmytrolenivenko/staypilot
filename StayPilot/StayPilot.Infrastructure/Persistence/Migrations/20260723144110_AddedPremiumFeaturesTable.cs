using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedPremiumFeaturesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAirConditioning",
                table: "OwnedProperties",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NearestBeachMarkerId",
                table: "OwnedProperties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NearestBeachName",
                table: "OwnedProperties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PremiumFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Feature = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PremiumPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PremiumFeatures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwnedProperties_NearestBeachMarkerId",
                table: "OwnedProperties",
                column: "NearestBeachMarkerId");

            migrationBuilder.AddForeignKey(
                name: "FK_OwnedProperties_BeachMarkers_NearestBeachMarkerId",
                table: "OwnedProperties",
                column: "NearestBeachMarkerId",
                principalTable: "BeachMarkers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OwnedProperties_BeachMarkers_NearestBeachMarkerId",
                table: "OwnedProperties");

            migrationBuilder.DropTable(
                name: "PremiumFeatures");

            migrationBuilder.DropIndex(
                name: "IX_OwnedProperties_NearestBeachMarkerId",
                table: "OwnedProperties");

            migrationBuilder.DropColumn(
                name: "HasAirConditioning",
                table: "OwnedProperties");

            migrationBuilder.DropColumn(
                name: "NearestBeachMarkerId",
                table: "OwnedProperties");

            migrationBuilder.DropColumn(
                name: "NearestBeachName",
                table: "OwnedProperties");
        }
    }
}
