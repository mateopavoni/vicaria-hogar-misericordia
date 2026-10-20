using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPsychiatricEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "psychiatric_evaluation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Professional = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Diagnosis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    RegisteredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_psychiatric_evaluation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_psychiatric_evaluation_persona_PersonId",
                        column: x => x.PersonId,
                        principalTable: "persona",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_psychiatric_evaluation_usuario_RegisteredByUserId",
                        column: x => x.RegisteredByUserId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_psychiatric_evaluation_PersonId",
                table: "psychiatric_evaluation",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_psychiatric_evaluation_RegisteredByUserId",
                table: "psychiatric_evaluation",
                column: "RegisteredByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "psychiatric_evaluation");
        }
    }
}
