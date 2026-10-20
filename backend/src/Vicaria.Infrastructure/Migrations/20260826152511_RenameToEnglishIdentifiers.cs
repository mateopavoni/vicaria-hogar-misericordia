using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameToEnglishIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_usuario_rol_RolId",
                table: "usuario");

            migrationBuilder.RenameColumn(
                name: "RolId",
                table: "usuario",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "Nombre",
                table: "usuario",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "Estado",
                table: "usuario",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "Apellido",
                table: "usuario",
                newName: "LastName");

            migrationBuilder.RenameIndex(
                name: "IX_usuario_RolId",
                table: "usuario",
                newName: "IX_usuario_RoleId");

            migrationBuilder.RenameColumn(
                name: "RolId",
                table: "rol_permission",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "Nombre",
                table: "rol",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_rol_Nombre",
                table: "rol",
                newName: "IX_rol_Name");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "audit_logs",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Fecha",
                table: "audit_logs",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "EntidadAfectada",
                table: "audit_logs",
                newName: "AffectedEntity");

            migrationBuilder.RenameColumn(
                name: "Accion",
                table: "audit_logs",
                newName: "Action");

            migrationBuilder.AddForeignKey(
                name: "FK_usuario_rol_RoleId",
                table: "usuario",
                column: "RoleId",
                principalTable: "rol",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_usuario_rol_RoleId",
                table: "usuario");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "usuario",
                newName: "Estado");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "usuario",
                newName: "RolId");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "usuario",
                newName: "Nombre");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "usuario",
                newName: "Apellido");

            migrationBuilder.RenameIndex(
                name: "IX_usuario_RoleId",
                table: "usuario",
                newName: "IX_usuario_RolId");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "rol_permission",
                newName: "RolId");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "rol",
                newName: "Nombre");

            migrationBuilder.RenameIndex(
                name: "IX_rol_Name",
                table: "rol",
                newName: "IX_rol_Nombre");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "audit_logs",
                newName: "UsuarioId");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "audit_logs",
                newName: "Fecha");

            migrationBuilder.RenameColumn(
                name: "AffectedEntity",
                table: "audit_logs",
                newName: "EntidadAfectada");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "audit_logs",
                newName: "Accion");

            migrationBuilder.AddForeignKey(
                name: "FK_usuario_rol_RolId",
                table: "usuario",
                column: "RolId",
                principalTable: "rol",
                principalColumn: "Id");
        }
    }
}
