using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMisfiledLisboaZones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 182,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Lisboa", "Lisboa" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 183,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Lisboa", "Lisboa" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 184,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Lisboa", "Lisboa" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 185,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Lisboa", "Lisboa" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 186,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Lisboa", "Lisboa" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 2169,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Lisboa", "Lisboa" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 2170,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Lisboa", "Lisboa" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 2171,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Lisboa", "Lisboa" });

            migrationBuilder.InsertData(
                table: "MarketAreas",
                columns: new[] { "Id", "Country", "District", "Municipality", "Notes", "Town", "Zone" },
                values: new object[,]
                {
                    { 4473, "Portugal", "Aveiro", "Vagos", null, "Santo António", null },
                    { 4474, "Portugal", "Leiria", "Pombal", null, "Carnide", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4473);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4474);

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 182,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Aveiro", "Vagos" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 183,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Aveiro", "Vagos" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 184,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Aveiro", "Vagos" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 185,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Aveiro", "Vagos" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 186,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Aveiro", "Vagos" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 2169,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Leiria", "Pombal" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 2170,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Leiria", "Pombal" });

            migrationBuilder.UpdateData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 2171,
                columns: new[] { "District", "Municipality" },
                values: new object[] { "Leiria", "Pombal" });
        }
    }
}
