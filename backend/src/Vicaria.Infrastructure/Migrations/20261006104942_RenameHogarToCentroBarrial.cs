using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameHogarToCentroBarrial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_life_stories_users_after_hogar_updated_by_user_id",
                table: "life_stories");

            migrationBuilder.DropForeignKey(
                name: "FK_life_stories_users_before_hogar_updated_by_user_id",
                table: "life_stories");

            migrationBuilder.DropForeignKey(
                name: "FK_life_stories_users_in_hogar_updated_by_user_id",
                table: "life_stories");

            migrationBuilder.RenameColumn(
                name: "in_hogar_updated_by_user_id",
                table: "life_stories",
                newName: "in_centro_barrial_updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "in_hogar_updated_at",
                table: "life_stories",
                newName: "in_centro_barrial_updated_at");

            migrationBuilder.RenameColumn(
                name: "in_hogar",
                table: "life_stories",
                newName: "in_centro_barrial");

            migrationBuilder.RenameColumn(
                name: "before_hogar_updated_by_user_id",
                table: "life_stories",
                newName: "before_centro_barrial_updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "before_hogar_updated_at",
                table: "life_stories",
                newName: "before_centro_barrial_updated_at");

            migrationBuilder.RenameColumn(
                name: "before_hogar",
                table: "life_stories",
                newName: "before_centro_barrial");

            migrationBuilder.RenameColumn(
                name: "after_hogar_updated_by_user_id",
                table: "life_stories",
                newName: "after_centro_barrial_updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "after_hogar_updated_at",
                table: "life_stories",
                newName: "after_centro_barrial_updated_at");

            migrationBuilder.RenameColumn(
                name: "after_hogar",
                table: "life_stories",
                newName: "after_centro_barrial");

            migrationBuilder.RenameIndex(
                name: "IX_life_stories_in_hogar_updated_by_user_id",
                table: "life_stories",
                newName: "IX_life_stories_in_centro_barrial_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_life_stories_before_hogar_updated_by_user_id",
                table: "life_stories",
                newName: "IX_life_stories_before_centro_barrial_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_life_stories_after_hogar_updated_by_user_id",
                table: "life_stories",
                newName: "IX_life_stories_after_centro_barrial_updated_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_life_stories_users_after_centro_barrial_updated_by_user_id",
                table: "life_stories",
                column: "after_centro_barrial_updated_by_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_life_stories_users_before_centro_barrial_updated_by_user_id",
                table: "life_stories",
                column: "before_centro_barrial_updated_by_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_life_stories_users_in_centro_barrial_updated_by_user_id",
                table: "life_stories",
                column: "in_centro_barrial_updated_by_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_life_stories_users_after_centro_barrial_updated_by_user_id",
                table: "life_stories");

            migrationBuilder.DropForeignKey(
                name: "FK_life_stories_users_before_centro_barrial_updated_by_user_id",
                table: "life_stories");

            migrationBuilder.DropForeignKey(
                name: "FK_life_stories_users_in_centro_barrial_updated_by_user_id",
                table: "life_stories");

            migrationBuilder.RenameColumn(
                name: "in_centro_barrial_updated_by_user_id",
                table: "life_stories",
                newName: "in_hogar_updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "in_centro_barrial_updated_at",
                table: "life_stories",
                newName: "in_hogar_updated_at");

            migrationBuilder.RenameColumn(
                name: "in_centro_barrial",
                table: "life_stories",
                newName: "in_hogar");

            migrationBuilder.RenameColumn(
                name: "before_centro_barrial_updated_by_user_id",
                table: "life_stories",
                newName: "before_hogar_updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "before_centro_barrial_updated_at",
                table: "life_stories",
                newName: "before_hogar_updated_at");

            migrationBuilder.RenameColumn(
                name: "before_centro_barrial",
                table: "life_stories",
                newName: "before_hogar");

            migrationBuilder.RenameColumn(
                name: "after_centro_barrial_updated_by_user_id",
                table: "life_stories",
                newName: "after_hogar_updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "after_centro_barrial_updated_at",
                table: "life_stories",
                newName: "after_hogar_updated_at");

            migrationBuilder.RenameColumn(
                name: "after_centro_barrial",
                table: "life_stories",
                newName: "after_hogar");

            migrationBuilder.RenameIndex(
                name: "IX_life_stories_in_centro_barrial_updated_by_user_id",
                table: "life_stories",
                newName: "IX_life_stories_in_hogar_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_life_stories_before_centro_barrial_updated_by_user_id",
                table: "life_stories",
                newName: "IX_life_stories_before_hogar_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_life_stories_after_centro_barrial_updated_by_user_id",
                table: "life_stories",
                newName: "IX_life_stories_after_hogar_updated_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_life_stories_users_after_hogar_updated_by_user_id",
                table: "life_stories",
                column: "after_hogar_updated_by_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_life_stories_users_before_hogar_updated_by_user_id",
                table: "life_stories",
                column: "before_hogar_updated_by_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_life_stories_users_in_hogar_updated_by_user_id",
                table: "life_stories",
                column: "in_hogar_updated_by_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
