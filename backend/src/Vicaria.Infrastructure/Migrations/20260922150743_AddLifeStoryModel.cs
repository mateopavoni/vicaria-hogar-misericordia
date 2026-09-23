using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLifeStoryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "historia_vida",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    antes_hogar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    antes_hogar_usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    antes_hogar_fecha_edicion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    en_hogar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    en_hogar_usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    en_hogar_fecha_edicion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    despues_hogar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    despues_hogar_usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    despues_hogar_fecha_edicion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historia_vida", x => x.id);
                    table.ForeignKey(
                        name: "FK_historia_vida_persona_persona_id",
                        column: x => x.persona_id,
                        principalTable: "persona",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_historia_vida_usuario_antes_hogar_usuario_id",
                        column: x => x.antes_hogar_usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_historia_vida_usuario_despues_hogar_usuario_id",
                        column: x => x.despues_hogar_usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_historia_vida_usuario_en_hogar_usuario_id",
                        column: x => x.en_hogar_usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_historia_vida_antes_hogar_usuario_id",
                table: "historia_vida",
                column: "antes_hogar_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_historia_vida_despues_hogar_usuario_id",
                table: "historia_vida",
                column: "despues_hogar_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_historia_vida_en_hogar_usuario_id",
                table: "historia_vida",
                column: "en_hogar_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_historia_vida_persona_id",
                table: "historia_vida",
                column: "persona_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historia_vida");
        }
    }
}
