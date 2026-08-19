using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Vicaria.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRolYRolIdUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RolId",
                table: "usuario",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "rol",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "rol",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Referente" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "DirectoraDeCasona" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Escucha" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_usuario_RolId",
                table: "usuario",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_rol_Nombre",
                table: "rol",
                column: "Nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_usuario_rol_RolId",
                table: "usuario",
                column: "RolId",
                principalTable: "rol",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_usuario_rol_RolId",
                table: "usuario");

            migrationBuilder.DropTable(
                name: "rol");

            migrationBuilder.DropIndex(
                name: "IX_usuario_RolId",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "RolId",
                table: "usuario");
        }
    }
}
