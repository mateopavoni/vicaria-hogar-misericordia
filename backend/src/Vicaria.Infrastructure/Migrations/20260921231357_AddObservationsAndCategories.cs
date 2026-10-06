using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddObservationsAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categoria_observacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categoria_observacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "observacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_observacion_categoria_observacion_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categoria_observacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_observacion_persona_persona_id",
                        column: x => x.persona_id,
                        principalTable: "persona",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_observacion_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_observacion_categoria_id",
                table: "observacion",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_observacion_persona_id",
                table: "observacion",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "IX_observacion_usuario_id",
                table: "observacion",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "observacion");

            migrationBuilder.DropTable(
                name: "categoria_observacion");
        }
    }
}
