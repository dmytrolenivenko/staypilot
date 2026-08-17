using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMisfiledBragaSaoVicente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 395,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Lisboa", "Lisboa" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 396,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Lisboa", "Lisboa" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 397,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Lisboa", "Lisboa" });

            migrationBuilder.InsertData(
                table: "MarketAreas",
                columns: new[] { "Id", "Country", "District", "Municipality", "Notes", "Town", "Zone" },
                values: new object[] { 4475, "Portugal", "Braga", "Braga", null, "São Vicente", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4475);

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 395,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Braga", "Braga" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 396,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Braga", "Braga" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 397,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Braga", "Braga" });
        }
    }
}
