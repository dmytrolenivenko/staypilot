using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnedPropertyValuation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OwnedPropertyValuations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnedPropertyId = table.Column<int>(type: "int", nullable: false),
                    District = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Municipality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Town = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocatedAreaName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocatedByCoordinates = table.Column<bool>(type: "bit", nullable: false),
                    MidPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PricePerM2 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ConfidenceLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfidenceNote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AskSpreadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DemandJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ForecastJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnedPropertyValuations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnedPropertyValuations_OwnedProperties_OwnedPropertyId",
                        column: x => x.OwnedPropertyId,
                        principalTable: "OwnedProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwnedPropertyValuations_OwnedPropertyId",
                table: "OwnedPropertyValuations",
                column: "OwnedPropertyId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwnedPropertyValuations");
        }
    }
}
