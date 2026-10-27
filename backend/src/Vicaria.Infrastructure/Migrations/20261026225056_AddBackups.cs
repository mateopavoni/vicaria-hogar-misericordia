using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBackups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "backups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ExternalKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_backups_CreatedAt",
                table: "backups",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "backups");
        }
    }
}
