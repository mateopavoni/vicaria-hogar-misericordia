using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCasonaVisit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "casona_visits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    person_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    visitor_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    estimated_duration_minutes = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    cancellation_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_casona_visits", x => x.id);
                    table.ForeignKey(
                        name: "FK_casona_visits_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_casona_visits_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_casona_visits_created_by_user_id",
                table: "casona_visits",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_casona_visits_person_id",
                table: "casona_visits",
                column: "person_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "casona_visits");
        }
    }
}
