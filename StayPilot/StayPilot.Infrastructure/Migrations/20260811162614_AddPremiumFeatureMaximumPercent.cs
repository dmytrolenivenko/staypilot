using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumFeatureMaximumPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaximumBasis",
                table: "PremiumFeatures",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumPercent",
                table: "PremiumFeatures",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaximumBasis",
                table: "PremiumFeatures");

            migrationBuilder.DropColumn(
                name: "MaximumPercent",
                table: "PremiumFeatures");
        }
    }
}
