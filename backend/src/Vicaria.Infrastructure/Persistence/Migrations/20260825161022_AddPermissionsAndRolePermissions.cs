using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Vicaria.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionsAndRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rol_permission",
                columns: table => new
                {
                    RolId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol_permission", x => new { x.RolId, x.PermissionId });
                });

            migrationBuilder.InsertData(
                table: "permission",
                columns: new[] { "Id", "Code" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), "VerFichasResidentesCasaConvivencia" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "CargarObservacionesResidentes" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "VerAgendaMedicamentos" }
                });

            migrationBuilder.InsertData(
                table: "rol_permission",
                columns: new[] { "PermissionId", "RolId" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("55555555-5555-5555-5555-555555555555"), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("66666666-6666-6666-6666-666666666666"), new Guid("22222222-2222-2222-2222-222222222222") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_permission_Code",
                table: "permission",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "permission");

            migrationBuilder.DropTable(
                name: "rol_permission");
        }
    }
}
