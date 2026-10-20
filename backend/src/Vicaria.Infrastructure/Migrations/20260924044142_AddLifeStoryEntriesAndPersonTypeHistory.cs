using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLifeStoryEntriesAndPersonTypeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "historia_vida_entradas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    etapa = table.Column<int>(type: "int", nullable: false),
                    contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historia_vida_entradas", x => x.id);
                    table.ForeignKey(
                        name: "FK_historia_vida_entradas_persona_persona_id",
                        column: x => x.persona_id,
                        principalTable: "persona",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_historia_vida_entradas_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "historial_tipo_persona",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tipo_anterior = table.Column<int>(type: "int", nullable: true),
                    tipo_nuevo = table.Column<int>(type: "int", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fecha_cambio = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_tipo_persona", x => x.id);
                    table.ForeignKey(
                        name: "FK_historial_tipo_persona_persona_persona_id",
                        column: x => x.persona_id,
                        principalTable: "persona",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_historial_tipo_persona_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_historia_vida_entradas_persona_id_etapa",
                table: "historia_vida_entradas",
                columns: new[] { "persona_id", "etapa" });

            migrationBuilder.CreateIndex(
                name: "IX_historia_vida_entradas_usuario_id",
                table: "historia_vida_entradas",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_historial_tipo_persona_persona_id",
                table: "historial_tipo_persona",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "IX_historial_tipo_persona_usuario_id",
                table: "historial_tipo_persona",
                column: "usuario_id");

            // backfill: cada etapa ya escrita en historia_vida (el "último valor" que
            // antes se pisaba en cada guardado) se preserva como su primera entrada en
            // el historial nuevo, en vez de arrancar el historial vacío y perder lo ya
            // escrito por el equipo hasta ahora.
            migrationBuilder.Sql(@"
                INSERT INTO historia_vida_entradas (id, persona_id, etapa, contenido, usuario_id, fecha_creacion)
                SELECT NEWID(), persona_id, 0, antes_hogar, antes_hogar_usuario_id, antes_hogar_fecha_edicion
                FROM historia_vida
                WHERE antes_hogar IS NOT NULL AND antes_hogar_usuario_id IS NOT NULL AND antes_hogar_fecha_edicion IS NOT NULL;

                INSERT INTO historia_vida_entradas (id, persona_id, etapa, contenido, usuario_id, fecha_creacion)
                SELECT NEWID(), persona_id, 1, en_hogar, en_hogar_usuario_id, en_hogar_fecha_edicion
                FROM historia_vida
                WHERE en_hogar IS NOT NULL AND en_hogar_usuario_id IS NOT NULL AND en_hogar_fecha_edicion IS NOT NULL;

                INSERT INTO historia_vida_entradas (id, persona_id, etapa, contenido, usuario_id, fecha_creacion)
                SELECT NEWID(), persona_id, 2, despues_hogar, despues_hogar_usuario_id, despues_hogar_fecha_edicion
                FROM historia_vida
                WHERE despues_hogar IS NOT NULL AND despues_hogar_usuario_id IS NOT NULL AND despues_hogar_fecha_edicion IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historia_vida_entradas");

            migrationBuilder.DropTable(
                name: "historial_tipo_persona");
        }
    }
}
