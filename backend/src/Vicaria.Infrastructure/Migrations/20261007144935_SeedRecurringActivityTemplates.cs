using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRecurringActivityTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "general_calendar_events",
                columns: new[] { "id", "author_user_id", "created_at", "date", "description", "end_time", "recurrence_days", "start_time", "title" },
                values: new object[,]
                {
                    { new Guid("88888888-8888-8888-8888-888888888801"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, new TimeSpan(0, 11, 0, 0, 0), 31, new TimeSpan(0, 9, 30, 0, 0), "Desayuno" },
                    { new Guid("88888888-8888-8888-8888-888888888802"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, new TimeSpan(0, 14, 30, 0, 0), 31, new TimeSpan(0, 13, 30, 0, 0), "Almuerzo" },
                    { new Guid("88888888-8888-8888-8888-888888888803"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, new TimeSpan(0, 18, 0, 0, 0), 2, new TimeSpan(0, 16, 0, 0, 0), "Merendero" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "general_calendar_events",
                keyColumn: "id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888801"));

            migrationBuilder.DeleteData(
                table: "general_calendar_events",
                keyColumn: "id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888802"));

            migrationBuilder.DeleteData(
                table: "general_calendar_events",
                keyColumn: "id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888803"));
        }
    }
}
