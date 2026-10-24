using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaritasCatalogValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "caritas_catalog_values",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_caritas_catalog_values", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "caritas_catalog_values",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222201"), "HousingSituation", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "En situación de calle" },
                    { new Guid("22222222-2222-2222-2222-222222222202"), "HousingSituation", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Con domicilio" },
                    { new Guid("22222222-2222-2222-2222-222222222203"), "HousingSituation", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Transitorio" },
                    { new Guid("22222222-2222-2222-2222-222222222204"), "DocumentationStatus", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Con DNI" },
                    { new Guid("22222222-2222-2222-2222-222222222205"), "DocumentationStatus", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Sin DNI" },
                    { new Guid("22222222-2222-2222-2222-222222222206"), "DocumentationStatus", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "En trámite" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_caritas_catalog_values_Category_Name",
                table: "caritas_catalog_values",
                columns: new[] { "Category", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "caritas_catalog_values");
        }
    }
}
