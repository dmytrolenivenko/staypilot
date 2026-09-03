using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndOwnedPropertyOwnerEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CleanUp the OwnedProperty table
            migrationBuilder.Sql("Delete FROM OwnedProperties;");

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "OwnedProperties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PreferredLocale = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwnedProperties_OwnerUserId",
                table: "OwnedProperties",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalId",
                table: "Users",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserEmail",
                table: "Users",
                column: "UserEmail",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OwnedProperties_Users_OwnerUserId",
                table: "OwnedProperties",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OwnedProperties_Users_OwnerUserId",
                table: "OwnedProperties");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_OwnedProperties_OwnerUserId",
                table: "OwnedProperties");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "OwnedProperties");
        }
    }
}
