using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameDirectorRoleToCasaConvivencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "Name",
                value: "DirectoraDeCasaConvivencia");

            // el rol viaja dentro del JWT: subir TokenVersion invalida las sesiones viejas y fuerza re-login
            migrationBuilder.Sql("UPDATE users SET TokenVersion = TokenVersion + 1 WHERE RoleId = '22222222-2222-2222-2222-222222222222'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "Name",
                value: "DirectoraDeCasona");

            // el rol viaja dentro del JWT: subir TokenVersion invalida las sesiones viejas y fuerza re-login
            migrationBuilder.Sql("UPDATE users SET TokenVersion = TokenVersion + 1 WHERE RoleId = '22222222-2222-2222-2222-222222222222'");
        }
    }
}
