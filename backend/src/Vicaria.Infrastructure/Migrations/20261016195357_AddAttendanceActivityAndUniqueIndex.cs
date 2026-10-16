using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceActivityAndUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attendances_PersonId",
                table: "attendances");

            migrationBuilder.AddColumn<string>(
                name: "Activity",
                table: "attendances",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AnonymousCount",
                table: "attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_attendances_PersonId_Activity_Date",
                table: "attendances",
                columns: new[] { "PersonId", "Activity", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attendances_PersonId_Activity_Date",
                table: "attendances");

            migrationBuilder.DropColumn(
                name: "Activity",
                table: "attendances");

            migrationBuilder.DropColumn(
                name: "AnonymousCount",
                table: "attendances");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_PersonId",
                table: "attendances",
                column: "PersonId");
        }
    }
}
