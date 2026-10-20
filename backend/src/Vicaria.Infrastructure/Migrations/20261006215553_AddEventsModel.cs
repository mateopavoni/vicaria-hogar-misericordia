using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fecha_inicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    es_recurrente = table.Column<bool>(type: "bit", nullable: false),
                    patron_recurrencia = table.Column<int>(type: "int", nullable: false),
                    dias_recurrencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    recurrencia_hasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evento", x => x.id);
                    table.ForeignKey(
                        name: "FK_evento_users_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evento_usuario_id",
                table: "evento",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evento");
        }
    }
}
