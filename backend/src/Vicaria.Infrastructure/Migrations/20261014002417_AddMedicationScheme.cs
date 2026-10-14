using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationScheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "medication_catalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_catalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "medication_schemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dose = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Time = table.Column<TimeSpan>(type: "time", nullable: false),
                    LoadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LoadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PendingStock = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_schemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_medication_schemes_medication_catalogs_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "medication_catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_medication_schemes_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_medication_schemes_users_LoadedByUserId",
                        column: x => x.LoadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_medication_catalogs_Name",
                table: "medication_catalogs",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_medication_schemes_LoadedByUserId",
                table: "medication_schemes",
                column: "LoadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_medication_schemes_MedicationId",
                table: "medication_schemes",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_medication_schemes_PersonId",
                table: "medication_schemes",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "medication_schemes");

            migrationBuilder.DropTable(
                name: "medication_catalogs");
        }
    }
}
