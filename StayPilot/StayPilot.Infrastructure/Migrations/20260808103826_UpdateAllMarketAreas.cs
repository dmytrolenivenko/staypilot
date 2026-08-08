using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAllMarketAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 3406,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Faro", "Loulé" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 3413,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Faro", "Loulé" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 3414,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Faro", "Loulé" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 3419,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Faro", "Loulé" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 3423,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Faro", "Loulé" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 3424,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Faro", "Loulé" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 3406,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Setúbal", "Setúbal" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 3413,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Setúbal", "Setúbal" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 3414,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Setúbal", "Setúbal" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 3419,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Setúbal", "Setúbal" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 3423,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Setúbal", "Setúbal" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 3424,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Setúbal", "Setúbal" });
        }
    }
}
