using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumFeatureListingsWithFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ListingsWithFeature",
                table: "PremiumFeatures",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ListingsWithFeature",
                table: "PremiumFeatures");
        }
    }
}
