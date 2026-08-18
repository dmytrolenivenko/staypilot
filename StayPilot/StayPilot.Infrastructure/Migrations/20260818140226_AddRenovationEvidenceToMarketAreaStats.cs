using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRenovationEvidenceToMarketAreaStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MoveInMedianAreaM2",
                table: "MarketAreaStats",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MoveInP25PricePerM2",
                table: "MarketAreaStats",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MoveInP75PricePerM2",
                table: "MarketAreaStats",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectByConditionCount",
                table: "MarketAreaStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProjectByEnergyCount",
                table: "MarketAreaStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ProjectMedianAreaM2",
                table: "MarketAreaStats",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProjectP25PricePerM2",
                table: "MarketAreaStats",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProjectP75PricePerM2",
                table: "MarketAreaStats",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnclassifiedCount",
                table: "MarketAreaStats",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MoveInMedianAreaM2",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "MoveInP25PricePerM2",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "MoveInP75PricePerM2",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "ProjectByConditionCount",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "ProjectByEnergyCount",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "ProjectMedianAreaM2",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "ProjectP25PricePerM2",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "ProjectP75PricePerM2",
                table: "MarketAreaStats");

            migrationBuilder.DropColumn(
                name: "UnclassifiedCount",
                table: "MarketAreaStats");
        }
    }
}
