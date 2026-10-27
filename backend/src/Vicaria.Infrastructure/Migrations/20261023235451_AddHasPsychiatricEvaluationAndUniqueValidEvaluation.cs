using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHasPsychiatricEvaluationAndUniqueValidEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_psychiatric_evaluations_PersonId",
                table: "psychiatric_evaluations");

            migrationBuilder.AddColumn<bool>(
                name: "HasPsychiatricEvaluation",
                table: "social_records",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_psychiatric_evaluations_PersonId",
                table: "psychiatric_evaluations",
                column: "PersonId",
                unique: true,
                filter: "[IsValid] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_psychiatric_evaluations_PersonId",
                table: "psychiatric_evaluations");

            migrationBuilder.DropColumn(
                name: "HasPsychiatricEvaluation",
                table: "social_records");

            migrationBuilder.CreateIndex(
                name: "IX_psychiatric_evaluations_PersonId",
                table: "psychiatric_evaluations",
                column: "PersonId");
        }
    }
}
