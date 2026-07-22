using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollationForMarketArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Zone",
                table: "MarketAreas",
                type: "nvarchar(max)",
                nullable: true,
                collation: "Latin1_General_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Town",
                table: "MarketAreas",
                type: "nvarchar(max)",
                nullable: false,
                collation: "Latin1_General_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Municipality",
                table: "MarketAreas",
                type: "nvarchar(max)",
                nullable: false,
                collation: "Latin1_General_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "District",
                table: "MarketAreas",
                type: "nvarchar(max)",
                nullable: false,
                collation: "Latin1_General_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Zone",
                table: "MarketAreas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldCollation: "Latin1_General_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "Town",
                table: "MarketAreas",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldCollation: "Latin1_General_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "Municipality",
                table: "MarketAreas",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldCollation: "Latin1_General_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "District",
                table: "MarketAreas",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldCollation: "Latin1_General_CI_AI");
        }
    }
}
