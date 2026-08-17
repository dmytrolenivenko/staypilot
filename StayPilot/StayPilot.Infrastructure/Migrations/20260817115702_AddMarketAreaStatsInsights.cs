using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketAreaStatsInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BelowEstimateCount",
                table: "MarketAreaStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CentroidLatitude",
                table: "MarketAreaStats",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CentroidLongitude",
                table: "MarketAreaStats",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MedianAreaM2",
                table: "MarketAreaStats",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MoveInCount",
                table: "MarketAreaStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MoveInMedianPricePerM2",
                table: "MarketAreaStats",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectCount",
                table: "MarketAreaStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ProjectMedianPricePerM2",
                table: "MarketAreaStats",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MarketAreaTypologyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketAreaStatsId = table.Column<int>(type: "int", nullable: false),
                    Typology = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ListingCount = table.Column<int>(type: "int", nullable: false),
                    MedianPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MedianAreaM2 = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MedianPricePerM2 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketAreaTypologyStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketAreaTypologyStats_MarketAreaStats_MarketAreaStatsId",
                        column: x => x.MarketAreaStatsId,
                        principalTable: "MarketAreaStats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketAreaTypologyStats_MarketAreaStatsId_Typology",
                table: "MarketAreaTypologyStats",
                columns: new[] { "MarketAreaStatsId", "Typology" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketAreaTypologyStats");

            migrationBuilder.DropColumn(
                name: "BelowEstimateCount",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "CentroidLatitude",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "CentroidLongitude",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "MedianAreaM2",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "MoveInCount",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "MoveInMedianPricePerM2",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "ProjectCount",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "ProjectMedianPricePerM2",
                table: "MarketAreaStats");
        }
    }
}
