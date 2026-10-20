using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeDniDigitsOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // bug reportado 2026-09-23: DNIs guardados con puntos/espacios/guiones
            // ("38.123.456") no matcheaban contra una búsqueda solo de dígitos
            // ("38123456"). El código nuevo ya normaliza a solo dígitos al guardar;
            // esto limpia los datos que ya existían antes del fix.
            migrationBuilder.Sql(@"
                UPDATE persona
                SET Dni = REPLACE(REPLACE(REPLACE(REPLACE(Dni, '.', ''), ' ', ''), '-', ''), CHAR(9), '')
                WHERE Dni IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // no reversible: no se puede reconstruir el formato original (puntos/espacios)
            // a partir del valor ya normalizado.
        }
    }
}
