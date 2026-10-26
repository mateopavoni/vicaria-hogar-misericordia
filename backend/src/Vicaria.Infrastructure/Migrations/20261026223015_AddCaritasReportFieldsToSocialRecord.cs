using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaritasReportFieldsToSocialRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AttendanceReasonId",
                table: "social_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentationStatusId",
                table: "social_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GenderId",
                table: "social_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HousingSituationId",
                table: "social_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocalityId",
                table: "social_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "person_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_person_attachments_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_person_attachments_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "caritas_catalog_values",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222207"), "Gender", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Masculino" },
                    { new Guid("22222222-2222-2222-2222-222222222208"), "Gender", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Femenino" },
                    { new Guid("22222222-2222-2222-2222-222222222209"), "Gender", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Otro" },
                    { new Guid("22222222-2222-2222-2222-222222222210"), "OriginLocality", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Misma ciudad" },
                    { new Guid("22222222-2222-2222-2222-222222222211"), "OriginLocality", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Otra localidad de la provincia" },
                    { new Guid("22222222-2222-2222-2222-222222222212"), "OriginLocality", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Otra provincia" },
                    { new Guid("22222222-2222-2222-2222-222222222213"), "OriginLocality", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Otro país" },
                    { new Guid("22222222-2222-2222-2222-222222222214"), "AttendanceReason", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Consumo de alcohol" },
                    { new Guid("22222222-2222-2222-2222-222222222215"), "AttendanceReason", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Consumo de drogas" },
                    { new Guid("22222222-2222-2222-2222-222222222216"), "AttendanceReason", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Violencia" },
                    { new Guid("22222222-2222-2222-2222-222222222217"), "AttendanceReason", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Múltiple" },
                    { new Guid("22222222-2222-2222-2222-222222222218"), "AttendanceReason", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "No especificado" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_social_records_AttendanceReasonId",
                table: "social_records",
                column: "AttendanceReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_social_records_DocumentationStatusId",
                table: "social_records",
                column: "DocumentationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_social_records_GenderId",
                table: "social_records",
                column: "GenderId");

            migrationBuilder.CreateIndex(
                name: "IX_social_records_HousingSituationId",
                table: "social_records",
                column: "HousingSituationId");

            migrationBuilder.CreateIndex(
                name: "IX_social_records_OriginLocalityId",
                table: "social_records",
                column: "OriginLocalityId");

            migrationBuilder.CreateIndex(
                name: "IX_person_attachments_PersonId",
                table: "person_attachments",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_person_attachments_UploadedAt",
                table: "person_attachments",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_person_attachments_UploadedByUserId",
                table: "person_attachments",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_social_records_caritas_catalog_values_AttendanceReasonId",
                table: "social_records",
                column: "AttendanceReasonId",
                principalTable: "caritas_catalog_values",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_social_records_caritas_catalog_values_DocumentationStatusId",
                table: "social_records",
                column: "DocumentationStatusId",
                principalTable: "caritas_catalog_values",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_social_records_caritas_catalog_values_GenderId",
                table: "social_records",
                column: "GenderId",
                principalTable: "caritas_catalog_values",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_social_records_caritas_catalog_values_HousingSituationId",
                table: "social_records",
                column: "HousingSituationId",
                principalTable: "caritas_catalog_values",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_social_records_caritas_catalog_values_OriginLocalityId",
                table: "social_records",
                column: "OriginLocalityId",
                principalTable: "caritas_catalog_values",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_social_records_caritas_catalog_values_AttendanceReasonId",
                table: "social_records");

            migrationBuilder.DropForeignKey(
                name: "FK_social_records_caritas_catalog_values_DocumentationStatusId",
                table: "social_records");

            migrationBuilder.DropForeignKey(
                name: "FK_social_records_caritas_catalog_values_GenderId",
                table: "social_records");

            migrationBuilder.DropForeignKey(
                name: "FK_social_records_caritas_catalog_values_HousingSituationId",
                table: "social_records");

            migrationBuilder.DropForeignKey(
                name: "FK_social_records_caritas_catalog_values_OriginLocalityId",
                table: "social_records");

            migrationBuilder.DropTable(
                name: "person_attachments");

            migrationBuilder.DropIndex(
                name: "IX_social_records_AttendanceReasonId",
                table: "social_records");

            migrationBuilder.DropIndex(
                name: "IX_social_records_DocumentationStatusId",
                table: "social_records");

            migrationBuilder.DropIndex(
                name: "IX_social_records_GenderId",
                table: "social_records");

            migrationBuilder.DropIndex(
                name: "IX_social_records_HousingSituationId",
                table: "social_records");

            migrationBuilder.DropIndex(
                name: "IX_social_records_OriginLocalityId",
                table: "social_records");

            migrationBuilder.DeleteData(
                table: "caritas_catalog_values",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222207"));

            migrationBuilder.DeleteData(
                table: "caritas_catalog_values",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222208"));

            migrationBuilder.DeleteData(
                table: "caritas_catalog_values",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222209"));

            migrationBuilder.DeleteData(
                table: "caritas_catalog_values",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222210"));

            migrationBuilder.DeleteData(
                table: "caritas_catalog_values",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222211"));

            migrationBuilder.DeleteData(
                table: "caritas_catalog_values",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222212"));

            migrationBuilder.DeleteData(
                table: "caritas_catalog_values",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222213"));

            migrationBuilder.DeleteData(
                table: "caritas_catalog_values",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222214"));

            migrationBuilder.DeleteData(
                table: "caritas_catalog_values",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222215"));

            migrationBuilder.DeleteData(
                table: "caritas_catalog_values",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222216"));

            migrationBuilder.DeleteData(
                table: "caritas_catalog_values",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222217"));

            migrationBuilder.DeleteData(
                table: "caritas_catalog_values",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222218"));

            migrationBuilder.DropColumn(
                name: "AttendanceReasonId",
                table: "social_records");

            migrationBuilder.DropColumn(
                name: "DocumentationStatusId",
                table: "social_records");

            migrationBuilder.DropColumn(
                name: "GenderId",
                table: "social_records");

            migrationBuilder.DropColumn(
                name: "HousingSituationId",
                table: "social_records");

            migrationBuilder.DropColumn(
                name: "OriginLocalityId",
                table: "social_records");
        }
    }
}
