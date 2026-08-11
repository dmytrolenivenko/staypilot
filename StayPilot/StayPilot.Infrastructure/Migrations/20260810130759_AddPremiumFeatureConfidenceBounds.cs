using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumFeatureConfidenceBounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LowerBoundPercent",
                table: "PremiumFeatures",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SampleSize",
                table: "PremiumFeatures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UpperBoundPercent",
                table: "PremiumFeatures",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LowerBoundPercent",
                table: "PremiumFeatures");

            migrationBuilder.DropColumn(
                name: "SampleSize",
                table: "PremiumFeatures");

            migrationBuilder.DropColumn(
                name: "UpperBoundPercent",
                table: "PremiumFeatures");
        }
    }
}
