using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationSchemeReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "medication_schemes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "medication_schemes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_medication_schemes_ReviewedByUserId",
                table: "medication_schemes",
                column: "ReviewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_medication_schemes_users_ReviewedByUserId",
                table: "medication_schemes",
                column: "ReviewedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_medication_schemes_users_ReviewedByUserId",
                table: "medication_schemes");

            migrationBuilder.DropIndex(
                name: "IX_medication_schemes_ReviewedByUserId",
                table: "medication_schemes");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "medication_schemes");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "medication_schemes");
        }
    }
}
