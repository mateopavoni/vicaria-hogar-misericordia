using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarEventMonthlyRecurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "repeats_monthly",
                table: "personal_calendar_events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "repeats_monthly",
                table: "general_calendar_events",
                type: "bit",
                nullable: false,
                defaultValue: false);



        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "repeats_monthly",
                table: "personal_calendar_events");

            migrationBuilder.DropColumn(
                name: "repeats_monthly",
                table: "general_calendar_events");
        }
    }
}
