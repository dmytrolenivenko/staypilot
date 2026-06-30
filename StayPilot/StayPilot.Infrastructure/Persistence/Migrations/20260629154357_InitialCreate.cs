using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    District = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Municipality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Town = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketAreas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OwnedProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MarketAreaId = table.Column<int>(type: "int", nullable: false),
                    PropertyType = table.Column<int>(type: "int", nullable: false),
                    Typology = table.Column<int>(type: "int", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RenovationInvestment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AreaM2 = table.Column<int>(type: "int", nullable: false),
                    Bathrooms = table.Column<int>(type: "int", nullable: true),
                    Floor = table.Column<int>(type: "int", nullable: true),
                    TotalFloors = table.Column<int>(type: "int", nullable: true),
                    HasElevator = table.Column<bool>(type: "bit", nullable: true),
                    ConstructionYear = table.Column<int>(type: "int", nullable: true),
                    RenovationYear = table.Column<int>(type: "int", nullable: true),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    BalconyCount = table.Column<int>(type: "int", nullable: false),
                    HasTerrace = table.Column<bool>(type: "bit", nullable: false),
                    HasGarage = table.Column<bool>(type: "bit", nullable: false),
                    HasParking = table.Column<bool>(type: "bit", nullable: false),
                    HasSwimmingPool = table.Column<bool>(type: "bit", nullable: false),
                    IsFurnished = table.Column<bool>(type: "bit", nullable: false),
                    HasSeaView = table.Column<bool>(type: "bit", nullable: false),
                    HasCityView = table.Column<bool>(type: "bit", nullable: false),
                    DistanceToBeachMeters = table.Column<int>(type: "int", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    EnergyCertificate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnedProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnedProperties_MarketAreas_MarketAreaId",
                        column: x => x.MarketAreaId,
                        principalTable: "MarketAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyListings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketAreaId = table.Column<int>(type: "int", nullable: false),
                    PropertyType = table.Column<int>(type: "int", nullable: false),
                    Typology = table.Column<int>(type: "int", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AreaM2 = table.Column<int>(type: "int", nullable: false),
                    Bathrooms = table.Column<int>(type: "int", nullable: false),
                    Floor = table.Column<int>(type: "int", nullable: true),
                    TotalFloors = table.Column<int>(type: "int", nullable: true),
                    HasElevator = table.Column<bool>(type: "bit", nullable: true),
                    HasAirConditioning = table.Column<bool>(type: "bit", nullable: true),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    ConstructionYear = table.Column<int>(type: "int", nullable: true),
                    RenovationYear = table.Column<int>(type: "int", nullable: true),
                    BalconyCount = table.Column<int>(type: "int", nullable: false),
                    HasTerrace = table.Column<bool>(type: "bit", nullable: false),
                    HasGarage = table.Column<bool>(type: "bit", nullable: false),
                    HasParking = table.Column<bool>(type: "bit", nullable: false),
                    HasSwimmingPool = table.Column<bool>(type: "bit", nullable: false),
                    IsFurnished = table.Column<bool>(type: "bit", nullable: false),
                    HasSeaView = table.Column<bool>(type: "bit", nullable: false),
                    HasCityView = table.Column<bool>(type: "bit", nullable: false),
                    DistanceToBeachMeters = table.Column<int>(type: "int", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    EnergyCertificate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyListings_MarketAreas_MarketAreaId",
                        column: x => x.MarketAreaId,
                        principalTable: "MarketAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListingSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyListingId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SnapshotDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingSnapshots_PropertyListings_PropertyListingId",
                        column: x => x.PropertyListingId,
                        principalTable: "PropertyListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingSnapshots_PropertyListingId",
                table: "ListingSnapshots",
                column: "PropertyListingId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnedProperties_MarketAreaId",
                table: "OwnedProperties",
                column: "MarketAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListings_MarketAreaId",
                table: "PropertyListings",
                column: "MarketAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListings_SourceUrl",
                table: "PropertyListings",
                column: "SourceUrl",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingSnapshots");

            migrationBuilder.DropTable(
                name: "OwnedProperties");

            migrationBuilder.DropTable(
                name: "PropertyListings");

            migrationBuilder.DropTable(
                name: "MarketAreas");
        }
    }
}
