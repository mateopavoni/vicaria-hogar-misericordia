using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyMedicationScheduleAndStockMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinimumStockThreshold",
                table: "medication_catalogs",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFromCalendar = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "daily_medication_schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicationSchemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Time = table.Column<TimeSpan>(type: "time", nullable: false),
                    Dose = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Observation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdministeredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdministeredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_medication_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_medication_schedules_medication_catalogs_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "medication_catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_daily_medication_schedules_medication_schemes_MedicationSchemeId",
                        column: x => x.MedicationSchemeId,
                        principalTable: "medication_schemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_daily_medication_schedules_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_daily_medication_schedules_users_AdministeredByUserId",
                        column: x => x.AdministeredByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "medication_stock_movements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Supplier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_stock_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_medication_stock_movements_medication_catalogs_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "medication_catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_medication_stock_movements_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_medication_schedules_AdministeredByUserId",
                table: "daily_medication_schedules",
                column: "AdministeredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_medication_schedules_Date",
                table: "daily_medication_schedules",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_daily_medication_schedules_MedicationId",
                table: "daily_medication_schedules",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_medication_schedules_MedicationSchemeId",
                table: "daily_medication_schedules",
                column: "MedicationSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_medication_schedules_PersonId",
                table: "daily_medication_schedules",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_medication_schedules_PersonId_Date",
                table: "daily_medication_schedules",
                columns: new[] { "PersonId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_medication_stock_movements_CreatedByUserId",
                table: "medication_stock_movements",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_medication_stock_movements_Date",
                table: "medication_stock_movements",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_medication_stock_movements_MedicationId",
                table: "medication_stock_movements",
                column: "MedicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "daily_medication_schedules");

            migrationBuilder.DropTable(
                name: "medication_stock_movements");

            migrationBuilder.DropColumn(
                name: "MinimumStockThreshold",
                table: "medication_catalogs");
        }
    }
}
