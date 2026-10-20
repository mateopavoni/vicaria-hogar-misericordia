using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoordinadorRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "rol",
                columns: new[] { "Id", "Nombre" },
                values: new object[] { new Guid("77777777-7777-7777-7777-777777777777"), "CoordinadorDeCasaConvivencia" });

            migrationBuilder.InsertData(
                table: "rol_permission",
                columns: new[] { "PermissionId", "RolId" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), new Guid("77777777-7777-7777-7777-777777777777") },
                    { new Guid("55555555-5555-5555-5555-555555555555"), new Guid("77777777-7777-7777-7777-777777777777") },
                    { new Guid("66666666-6666-6666-6666-666666666666"), new Guid("77777777-7777-7777-7777-777777777777") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rol",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DeleteData(
                table: "rol_permission",
                keyColumns: new[] { "PermissionId", "RolId" },
                keyValues: new object[] { new Guid("44444444-4444-4444-4444-444444444444"), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.DeleteData(
                table: "rol_permission",
                keyColumns: new[] { "PermissionId", "RolId" },
                keyValues: new object[] { new Guid("55555555-5555-5555-5555-555555555555"), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.DeleteData(
                table: "rol_permission",
                keyColumns: new[] { "PermissionId", "RolId" },
                keyValues: new object[] { new Guid("66666666-6666-6666-6666-666666666666"), new Guid("77777777-7777-7777-7777-777777777777") });
        }
    }
}
