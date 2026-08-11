using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumFeatureConfidenceAndBasis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Basis",
                table: "PremiumFeatures",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Basis",
                table: "PremiumFeatures");
        }
    }
}
