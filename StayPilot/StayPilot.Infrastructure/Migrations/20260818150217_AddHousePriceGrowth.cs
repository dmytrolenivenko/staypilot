using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHousePriceGrowth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HousePriceGrowth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AnnualGrowthPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    VolatilityPercentagePoints = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    AsOfYear = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HousePriceGrowth", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "HousePriceGrowth",
                columns: new[] { "Id", "AnnualGrowthPercent", "AsOfYear", "District", "Source", "VolatilityPercentagePoints" },
                values: new object[,]
                {
                    { 1, 6.0m, 2026, "", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 3.0m },
                    { 2, 7.5m, 2026, "Faro", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 4.0m },
                    { 3, 7.0m, 2026, "Lisboa", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 3.5m },
                    { 4, 7.0m, 2026, "Porto", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 3.5m },
                    { 5, 6.8m, 2026, "Setúbal", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 3.5m },
                    { 6, 7.0m, 2026, "Madeira", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 4.0m },
                    { 7, 6.5m, 2026, "Braga", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 3.0m },
                    { 8, 6.0m, 2026, "Aveiro", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 3.0m },
                    { 9, 5.5m, 2026, "Leiria", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 3.0m },
                    { 10, 5.5m, 2026, "Viana do Castelo", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 3.0m },
                    { 11, 5.5m, 2026, "Açores", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 3.5m },
                    { 12, 5.0m, 2026, "Coimbra", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 2.5m },
                    { 13, 4.5m, 2026, "Santarém", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 2.5m },
                    { 14, 4.5m, 2026, "Évora", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 2.5m },
                    { 15, 4.5m, 2026, "Viseu", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 2.5m },
                    { 16, 4.0m, 2026, "Vila Real", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 2.5m },
                    { 17, 4.0m, 2026, "Beja", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 2.5m },
                    { 18, 3.5m, 2026, "Castelo Branco", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 2.0m },
                    { 19, 3.5m, 2026, "Bragança", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 2.0m },
                    { 20, 3.0m, 2026, "Guarda", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 2.0m },
                    { 21, 3.0m, 2026, "Portalegre", "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.", 2.0m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HousePriceGrowth_District",
                table: "HousePriceGrowth",
                column: "District",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HousePriceGrowth");
        }
    }
}
