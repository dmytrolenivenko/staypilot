using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketAreaStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketAreaStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    District = table.Column<string>(type: "nvarchar(450)", nullable: false, collation: "Latin1_General_CI_AI"),
                    Municipality = table.Column<string>(type: "nvarchar(450)", nullable: false, collation: "Latin1_General_CI_AI"),
                    Town = table.Column<string>(type: "nvarchar(450)", nullable: false, collation: "Latin1_General_CI_AI"),
                    ListingCount = table.Column<int>(type: "int", nullable: false),
                    MedianPricePerM2 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketAreaStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketAreaStats_Level_District_Municipality_Town",
                table: "MarketAreaStats",
                columns: new[] { "Level", "District", "Municipality", "Town" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketAreaStats");
        }
    }
}
