using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TypologyProjectAndMoveInMedians : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MoveInCount",
                table: "MarketAreaTypologyStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MoveInMedianPricePerM2",
                table: "MarketAreaTypologyStats",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectCount",
                table: "MarketAreaTypologyStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ProjectMedianPricePerM2",
                table: "MarketAreaTypologyStats",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MoveInCount",
                table: "MarketAreaTypologyStats");

            migrationBuilder.DropColumn(
                name: "MoveInMedianPricePerM2",
                table: "MarketAreaTypologyStats");

            migrationBuilder.DropColumn(
                name: "ProjectCount",
                table: "MarketAreaTypologyStats");

            migrationBuilder.DropColumn(
                name: "ProjectMedianPricePerM2",
                table: "MarketAreaTypologyStats");
        }
    }
}
