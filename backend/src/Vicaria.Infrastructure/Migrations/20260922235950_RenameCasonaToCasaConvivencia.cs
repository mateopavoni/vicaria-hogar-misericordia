using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameCasonaToCasaConvivencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // rename real (no drop+create): preserva los datos y las FKs existentes
            migrationBuilder.RenameTable(
                name: "estadia_casona",
                newName: "estadia_casa_convivencia");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "estadia_casa_convivencia",
                newName: "estadia_casona");
        }
    }
}
