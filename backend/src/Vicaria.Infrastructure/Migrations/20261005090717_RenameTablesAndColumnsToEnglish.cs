using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vicaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesAndColumnsToEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_asistencia_persona_PersonId",
                table: "asistencia");

            migrationBuilder.DropForeignKey(
                name: "FK_contacto_ficha_social_SocialRecordId",
                table: "contacto");

            migrationBuilder.DropForeignKey(
                name: "FK_estadia_casona_persona_PersonId",
                table: "estadia_casa_convivencia");

            migrationBuilder.DropForeignKey(
                name: "FK_ficha_social_persona_PersonId",
                table: "ficha_social");

            migrationBuilder.DropForeignKey(
                name: "FK_historia_vida_persona_persona_id",
                table: "historia_vida");

            migrationBuilder.DropForeignKey(
                name: "FK_historia_vida_usuario_antes_hogar_usuario_id",
                table: "historia_vida");

            migrationBuilder.DropForeignKey(
                name: "FK_historia_vida_usuario_despues_hogar_usuario_id",
                table: "historia_vida");

            migrationBuilder.DropForeignKey(
                name: "FK_historia_vida_usuario_en_hogar_usuario_id",
                table: "historia_vida");

            migrationBuilder.DropForeignKey(
                name: "FK_historia_vida_entradas_persona_persona_id",
                table: "historia_vida_entradas");

            migrationBuilder.DropForeignKey(
                name: "FK_historia_vida_entradas_usuario_usuario_id",
                table: "historia_vida_entradas");

            migrationBuilder.DropForeignKey(
                name: "FK_historial_tipo_persona_persona_persona_id",
                table: "historial_tipo_persona");

            migrationBuilder.DropForeignKey(
                name: "FK_historial_tipo_persona_usuario_usuario_id",
                table: "historial_tipo_persona");

            migrationBuilder.DropForeignKey(
                name: "FK_observacion_categoria_observacion_categoria_id",
                table: "observacion");

            migrationBuilder.DropForeignKey(
                name: "FK_observacion_persona_persona_id",
                table: "observacion");

            migrationBuilder.DropForeignKey(
                name: "FK_observacion_usuario_usuario_id",
                table: "observacion");

            migrationBuilder.DropForeignKey(
                name: "FK_psychiatric_evaluation_persona_PersonId",
                table: "psychiatric_evaluation");

            migrationBuilder.DropForeignKey(
                name: "FK_psychiatric_evaluation_usuario_RegisteredByUserId",
                table: "psychiatric_evaluation");

            migrationBuilder.DropForeignKey(
                name: "FK_usuario_rol_RoleId",
                table: "usuario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_usuario",
                table: "usuario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_rol_permission",
                table: "rol_permission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_rol",
                table: "rol");

            migrationBuilder.DropPrimaryKey(
                name: "PK_psychiatric_evaluation",
                table: "psychiatric_evaluation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_persona",
                table: "persona");

            migrationBuilder.DropPrimaryKey(
                name: "PK_permission",
                table: "permission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_observacion",
                table: "observacion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_historial_tipo_persona",
                table: "historial_tipo_persona");

            migrationBuilder.DropPrimaryKey(
                name: "PK_historia_vida_entradas",
                table: "historia_vida_entradas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_historia_vida",
                table: "historia_vida");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ficha_social",
                table: "ficha_social");

            migrationBuilder.DropPrimaryKey(
                name: "PK_estadia_casona",
                table: "estadia_casa_convivencia");

            migrationBuilder.DropPrimaryKey(
                name: "PK_contacto",
                table: "contacto");

            migrationBuilder.DropPrimaryKey(
                name: "PK_categoria_observacion",
                table: "categoria_observacion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_asistencia",
                table: "asistencia");

            migrationBuilder.RenameTable(
                name: "usuario",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "rol_permission",
                newName: "role_permissions");

            migrationBuilder.RenameTable(
                name: "rol",
                newName: "roles");

            migrationBuilder.RenameTable(
                name: "psychiatric_evaluation",
                newName: "psychiatric_evaluations");

            migrationBuilder.RenameTable(
                name: "persona",
                newName: "people");

            migrationBuilder.RenameTable(
                name: "permission",
                newName: "permissions");

            migrationBuilder.RenameTable(
                name: "observacion",
                newName: "observations");

            migrationBuilder.RenameTable(
                name: "historial_tipo_persona",
                newName: "person_type_changes");

            migrationBuilder.RenameTable(
                name: "historia_vida_entradas",
                newName: "life_story_entries");

            migrationBuilder.RenameTable(
                name: "historia_vida",
                newName: "life_stories");

            migrationBuilder.RenameTable(
                name: "ficha_social",
                newName: "social_records");

            migrationBuilder.RenameTable(
                name: "estadia_casa_convivencia",
                newName: "casa_convivencia_stays");

            migrationBuilder.RenameTable(
                name: "contacto",
                newName: "contacts");

            migrationBuilder.RenameTable(
                name: "categoria_observacion",
                newName: "observation_categories");

            migrationBuilder.RenameTable(
                name: "asistencia",
                newName: "attendances");

            migrationBuilder.RenameIndex(
                name: "IX_usuario_RoleId",
                table: "users",
                newName: "IX_users_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_usuario_Email",
                table: "users",
                newName: "IX_users_Email");

            migrationBuilder.RenameIndex(
                name: "IX_rol_Name",
                table: "roles",
                newName: "IX_roles_Name");

            migrationBuilder.RenameIndex(
                name: "IX_psychiatric_evaluation_RegisteredByUserId",
                table: "psychiatric_evaluations",
                newName: "IX_psychiatric_evaluations_RegisteredByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_psychiatric_evaluation_PersonId",
                table: "psychiatric_evaluations",
                newName: "IX_psychiatric_evaluations_PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_permission_Code",
                table: "permissions",
                newName: "IX_permissions_Code");

            migrationBuilder.RenameColumn(
                name: "usuario_id",
                table: "observations",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "persona_id",
                table: "observations",
                newName: "person_id");

            migrationBuilder.RenameColumn(
                name: "fecha_creacion",
                table: "observations",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "contenido",
                table: "observations",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "categoria_id",
                table: "observations",
                newName: "category_id");

            migrationBuilder.RenameIndex(
                name: "IX_observacion_usuario_id",
                table: "observations",
                newName: "IX_observations_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_observacion_persona_id",
                table: "observations",
                newName: "IX_observations_person_id");

            migrationBuilder.RenameIndex(
                name: "IX_observacion_categoria_id",
                table: "observations",
                newName: "IX_observations_category_id");

            migrationBuilder.RenameColumn(
                name: "usuario_id",
                table: "person_type_changes",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "tipo_nuevo",
                table: "person_type_changes",
                newName: "new_type");

            migrationBuilder.RenameColumn(
                name: "tipo_anterior",
                table: "person_type_changes",
                newName: "previous_type");

            migrationBuilder.RenameColumn(
                name: "persona_id",
                table: "person_type_changes",
                newName: "person_id");

            migrationBuilder.RenameColumn(
                name: "fecha_cambio",
                table: "person_type_changes",
                newName: "changed_at");

            migrationBuilder.RenameIndex(
                name: "IX_historial_tipo_persona_usuario_id",
                table: "person_type_changes",
                newName: "IX_person_type_changes_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_historial_tipo_persona_persona_id",
                table: "person_type_changes",
                newName: "IX_person_type_changes_person_id");

            migrationBuilder.RenameColumn(
                name: "usuario_id",
                table: "life_story_entries",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "persona_id",
                table: "life_story_entries",
                newName: "person_id");

            migrationBuilder.RenameColumn(
                name: "fecha_creacion",
                table: "life_story_entries",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "etapa",
                table: "life_story_entries",
                newName: "stage");

            migrationBuilder.RenameColumn(
                name: "contenido",
                table: "life_story_entries",
                newName: "content");

            migrationBuilder.RenameIndex(
                name: "IX_historia_vida_entradas_usuario_id",
                table: "life_story_entries",
                newName: "IX_life_story_entries_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_historia_vida_entradas_persona_id_etapa",
                table: "life_story_entries",
                newName: "IX_life_story_entries_person_id_stage");

            migrationBuilder.RenameColumn(
                name: "persona_id",
                table: "life_stories",
                newName: "person_id");

            migrationBuilder.RenameColumn(
                name: "en_hogar_usuario_id",
                table: "life_stories",
                newName: "in_hogar_updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "en_hogar_fecha_edicion",
                table: "life_stories",
                newName: "in_hogar_updated_at");

            migrationBuilder.RenameColumn(
                name: "en_hogar",
                table: "life_stories",
                newName: "in_hogar");

            migrationBuilder.RenameColumn(
                name: "despues_hogar_usuario_id",
                table: "life_stories",
                newName: "after_hogar_updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "despues_hogar_fecha_edicion",
                table: "life_stories",
                newName: "after_hogar_updated_at");

            migrationBuilder.RenameColumn(
                name: "despues_hogar",
                table: "life_stories",
                newName: "after_hogar");

            migrationBuilder.RenameColumn(
                name: "antes_hogar_usuario_id",
                table: "life_stories",
                newName: "before_hogar_updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "antes_hogar_fecha_edicion",
                table: "life_stories",
                newName: "before_hogar_updated_at");

            migrationBuilder.RenameColumn(
                name: "antes_hogar",
                table: "life_stories",
                newName: "before_hogar");

            migrationBuilder.RenameIndex(
                name: "IX_historia_vida_persona_id",
                table: "life_stories",
                newName: "IX_life_stories_person_id");

            migrationBuilder.RenameIndex(
                name: "IX_historia_vida_en_hogar_usuario_id",
                table: "life_stories",
                newName: "IX_life_stories_in_hogar_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_historia_vida_despues_hogar_usuario_id",
                table: "life_stories",
                newName: "IX_life_stories_after_hogar_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_historia_vida_antes_hogar_usuario_id",
                table: "life_stories",
                newName: "IX_life_stories_before_hogar_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_ficha_social_PersonId",
                table: "social_records",
                newName: "IX_social_records_PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_estadia_casona_PersonId",
                table: "casa_convivencia_stays",
                newName: "IX_casa_convivencia_stays_PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_contacto_SocialRecordId",
                table: "contacts",
                newName: "IX_contacts_SocialRecordId");

            migrationBuilder.RenameIndex(
                name: "IX_asistencia_PersonId",
                table: "attendances",
                newName: "IX_attendances_PersonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_role_permissions",
                table: "role_permissions",
                columns: new[] { "RoleId", "PermissionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_roles",
                table: "roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_psychiatric_evaluations",
                table: "psychiatric_evaluations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_people",
                table: "people",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_permissions",
                table: "permissions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_observations",
                table: "observations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_person_type_changes",
                table: "person_type_changes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_life_story_entries",
                table: "life_story_entries",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_life_stories",
                table: "life_stories",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_social_records",
                table: "social_records",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_casa_convivencia_stays",
                table: "casa_convivencia_stays",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_contacts",
                table: "contacts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_observation_categories",
                table: "observation_categories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_attendances",
                table: "attendances",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_attendances_people_PersonId",
                table: "attendances",
                column: "PersonId",
                principalTable: "people",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_casa_convivencia_stays_people_PersonId",
                table: "casa_convivencia_stays",
                column: "PersonId",
                principalTable: "people",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_contacts_social_records_SocialRecordId",
                table: "contacts",
                column: "SocialRecordId",
                principalTable: "social_records",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_life_stories_people_person_id",
                table: "life_stories",
                column: "person_id",
                principalTable: "people",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

            migrationBuilder.AddForeignKey(
                name: "FK_life_story_entries_people_person_id",
                table: "life_story_entries",
                column: "person_id",
                principalTable: "people",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_life_story_entries_users_user_id",
                table: "life_story_entries",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_observations_observation_categories_category_id",
                table: "observations",
                column: "category_id",
                principalTable: "observation_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_observations_people_person_id",
                table: "observations",
                column: "person_id",
                principalTable: "people",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_observations_users_user_id",
                table: "observations",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_person_type_changes_people_person_id",
                table: "person_type_changes",
                column: "person_id",
                principalTable: "people",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_person_type_changes_users_user_id",
                table: "person_type_changes",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_psychiatric_evaluations_people_PersonId",
                table: "psychiatric_evaluations",
                column: "PersonId",
                principalTable: "people",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_psychiatric_evaluations_users_RegisteredByUserId",
                table: "psychiatric_evaluations",
                column: "RegisteredByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_social_records_people_PersonId",
                table: "social_records",
                column: "PersonId",
                principalTable: "people",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_users_roles_RoleId",
                table: "users",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attendances_people_PersonId",
                table: "attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_casa_convivencia_stays_people_PersonId",
                table: "casa_convivencia_stays");

            migrationBuilder.DropForeignKey(
                name: "FK_contacts_social_records_SocialRecordId",
                table: "contacts");

            migrationBuilder.DropForeignKey(
                name: "FK_life_stories_people_person_id",
                table: "life_stories");

            migrationBuilder.DropForeignKey(
                name: "FK_life_stories_users_after_hogar_updated_by_user_id",
                table: "life_stories");

            migrationBuilder.DropForeignKey(
                name: "FK_life_stories_users_before_hogar_updated_by_user_id",
                table: "life_stories");

            migrationBuilder.DropForeignKey(
                name: "FK_life_stories_users_in_hogar_updated_by_user_id",
                table: "life_stories");

            migrationBuilder.DropForeignKey(
                name: "FK_life_story_entries_people_person_id",
                table: "life_story_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_life_story_entries_users_user_id",
                table: "life_story_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_observations_observation_categories_category_id",
                table: "observations");

            migrationBuilder.DropForeignKey(
                name: "FK_observations_people_person_id",
                table: "observations");

            migrationBuilder.DropForeignKey(
                name: "FK_observations_users_user_id",
                table: "observations");

            migrationBuilder.DropForeignKey(
                name: "FK_person_type_changes_people_person_id",
                table: "person_type_changes");

            migrationBuilder.DropForeignKey(
                name: "FK_person_type_changes_users_user_id",
                table: "person_type_changes");

            migrationBuilder.DropForeignKey(
                name: "FK_psychiatric_evaluations_people_PersonId",
                table: "psychiatric_evaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_psychiatric_evaluations_users_RegisteredByUserId",
                table: "psychiatric_evaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_social_records_people_PersonId",
                table: "social_records");

            migrationBuilder.DropForeignKey(
                name: "FK_users_roles_RoleId",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_social_records",
                table: "social_records");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roles",
                table: "roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_role_permissions",
                table: "role_permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_psychiatric_evaluations",
                table: "psychiatric_evaluations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_person_type_changes",
                table: "person_type_changes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_permissions",
                table: "permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_people",
                table: "people");

            migrationBuilder.DropPrimaryKey(
                name: "PK_observations",
                table: "observations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_observation_categories",
                table: "observation_categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_life_story_entries",
                table: "life_story_entries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_life_stories",
                table: "life_stories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_contacts",
                table: "contacts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_casa_convivencia_stays",
                table: "casa_convivencia_stays");

            migrationBuilder.DropPrimaryKey(
                name: "PK_attendances",
                table: "attendances");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "usuario");

            migrationBuilder.RenameTable(
                name: "social_records",
                newName: "ficha_social");

            migrationBuilder.RenameTable(
                name: "roles",
                newName: "rol");

            migrationBuilder.RenameTable(
                name: "role_permissions",
                newName: "rol_permission");

            migrationBuilder.RenameTable(
                name: "psychiatric_evaluations",
                newName: "psychiatric_evaluation");

            migrationBuilder.RenameTable(
                name: "person_type_changes",
                newName: "historial_tipo_persona");

            migrationBuilder.RenameTable(
                name: "permissions",
                newName: "permission");

            migrationBuilder.RenameTable(
                name: "people",
                newName: "persona");

            migrationBuilder.RenameTable(
                name: "observations",
                newName: "observacion");

            migrationBuilder.RenameTable(
                name: "observation_categories",
                newName: "categoria_observacion");

            migrationBuilder.RenameTable(
                name: "life_story_entries",
                newName: "historia_vida_entradas");

            migrationBuilder.RenameTable(
                name: "life_stories",
                newName: "historia_vida");

            migrationBuilder.RenameTable(
                name: "contacts",
                newName: "contacto");

            migrationBuilder.RenameTable(
                name: "casa_convivencia_stays",
                newName: "estadia_casa_convivencia");

            migrationBuilder.RenameTable(
                name: "attendances",
                newName: "asistencia");

            migrationBuilder.RenameIndex(
                name: "IX_users_RoleId",
                table: "usuario",
                newName: "IX_usuario_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_users_Email",
                table: "usuario",
                newName: "IX_usuario_Email");

            migrationBuilder.RenameIndex(
                name: "IX_social_records_PersonId",
                table: "ficha_social",
                newName: "IX_ficha_social_PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_roles_Name",
                table: "rol",
                newName: "IX_rol_Name");

            migrationBuilder.RenameIndex(
                name: "IX_psychiatric_evaluations_RegisteredByUserId",
                table: "psychiatric_evaluation",
                newName: "IX_psychiatric_evaluation_RegisteredByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_psychiatric_evaluations_PersonId",
                table: "psychiatric_evaluation",
                newName: "IX_psychiatric_evaluation_PersonId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "historial_tipo_persona",
                newName: "usuario_id");

            migrationBuilder.RenameColumn(
                name: "previous_type",
                table: "historial_tipo_persona",
                newName: "tipo_anterior");

            migrationBuilder.RenameColumn(
                name: "person_id",
                table: "historial_tipo_persona",
                newName: "persona_id");

            migrationBuilder.RenameColumn(
                name: "new_type",
                table: "historial_tipo_persona",
                newName: "tipo_nuevo");

            migrationBuilder.RenameColumn(
                name: "changed_at",
                table: "historial_tipo_persona",
                newName: "fecha_cambio");

            migrationBuilder.RenameIndex(
                name: "IX_person_type_changes_user_id",
                table: "historial_tipo_persona",
                newName: "IX_historial_tipo_persona_usuario_id");

            migrationBuilder.RenameIndex(
                name: "IX_person_type_changes_person_id",
                table: "historial_tipo_persona",
                newName: "IX_historial_tipo_persona_persona_id");

            migrationBuilder.RenameIndex(
                name: "IX_permissions_Code",
                table: "permission",
                newName: "IX_permission_Code");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "observacion",
                newName: "usuario_id");

            migrationBuilder.RenameColumn(
                name: "person_id",
                table: "observacion",
                newName: "persona_id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "observacion",
                newName: "fecha_creacion");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "observacion",
                newName: "contenido");

            migrationBuilder.RenameColumn(
                name: "category_id",
                table: "observacion",
                newName: "categoria_id");

            migrationBuilder.RenameIndex(
                name: "IX_observations_user_id",
                table: "observacion",
                newName: "IX_observacion_usuario_id");

            migrationBuilder.RenameIndex(
                name: "IX_observations_person_id",
                table: "observacion",
                newName: "IX_observacion_persona_id");

            migrationBuilder.RenameIndex(
                name: "IX_observations_category_id",
                table: "observacion",
                newName: "IX_observacion_categoria_id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "historia_vida_entradas",
                newName: "usuario_id");

            migrationBuilder.RenameColumn(
                name: "stage",
                table: "historia_vida_entradas",
                newName: "etapa");

            migrationBuilder.RenameColumn(
                name: "person_id",
                table: "historia_vida_entradas",
                newName: "persona_id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "historia_vida_entradas",
                newName: "fecha_creacion");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "historia_vida_entradas",
                newName: "contenido");

            migrationBuilder.RenameIndex(
                name: "IX_life_story_entries_user_id",
                table: "historia_vida_entradas",
                newName: "IX_historia_vida_entradas_usuario_id");

            migrationBuilder.RenameIndex(
                name: "IX_life_story_entries_person_id_stage",
                table: "historia_vida_entradas",
                newName: "IX_historia_vida_entradas_persona_id_etapa");

            migrationBuilder.RenameColumn(
                name: "person_id",
                table: "historia_vida",
                newName: "persona_id");

            migrationBuilder.RenameColumn(
                name: "in_hogar_updated_by_user_id",
                table: "historia_vida",
                newName: "en_hogar_usuario_id");

            migrationBuilder.RenameColumn(
                name: "in_hogar_updated_at",
                table: "historia_vida",
                newName: "en_hogar_fecha_edicion");

            migrationBuilder.RenameColumn(
                name: "in_hogar",
                table: "historia_vida",
                newName: "en_hogar");

            migrationBuilder.RenameColumn(
                name: "before_hogar_updated_by_user_id",
                table: "historia_vida",
                newName: "antes_hogar_usuario_id");

            migrationBuilder.RenameColumn(
                name: "before_hogar_updated_at",
                table: "historia_vida",
                newName: "antes_hogar_fecha_edicion");

            migrationBuilder.RenameColumn(
                name: "before_hogar",
                table: "historia_vida",
                newName: "antes_hogar");

            migrationBuilder.RenameColumn(
                name: "after_hogar_updated_by_user_id",
                table: "historia_vida",
                newName: "despues_hogar_usuario_id");

            migrationBuilder.RenameColumn(
                name: "after_hogar_updated_at",
                table: "historia_vida",
                newName: "despues_hogar_fecha_edicion");

            migrationBuilder.RenameColumn(
                name: "after_hogar",
                table: "historia_vida",
                newName: "despues_hogar");

            migrationBuilder.RenameIndex(
                name: "IX_life_stories_person_id",
                table: "historia_vida",
                newName: "IX_historia_vida_persona_id");

            migrationBuilder.RenameIndex(
                name: "IX_life_stories_in_hogar_updated_by_user_id",
                table: "historia_vida",
                newName: "IX_historia_vida_en_hogar_usuario_id");

            migrationBuilder.RenameIndex(
                name: "IX_life_stories_before_hogar_updated_by_user_id",
                table: "historia_vida",
                newName: "IX_historia_vida_antes_hogar_usuario_id");

            migrationBuilder.RenameIndex(
                name: "IX_life_stories_after_hogar_updated_by_user_id",
                table: "historia_vida",
                newName: "IX_historia_vida_despues_hogar_usuario_id");

            migrationBuilder.RenameIndex(
                name: "IX_contacts_SocialRecordId",
                table: "contacto",
                newName: "IX_contacto_SocialRecordId");

            migrationBuilder.RenameIndex(
                name: "IX_casa_convivencia_stays_PersonId",
                table: "estadia_casa_convivencia",
                newName: "IX_estadia_casa_convivencia_PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_attendances_PersonId",
                table: "asistencia",
                newName: "IX_asistencia_PersonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_usuario",
                table: "usuario",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ficha_social",
                table: "ficha_social",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_rol",
                table: "rol",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_rol_permission",
                table: "rol_permission",
                columns: new[] { "RoleId", "PermissionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_psychiatric_evaluation",
                table: "psychiatric_evaluation",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_historial_tipo_persona",
                table: "historial_tipo_persona",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_permission",
                table: "permission",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_persona",
                table: "persona",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_observacion",
                table: "observacion",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_categoria_observacion",
                table: "categoria_observacion",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_historia_vida_entradas",
                table: "historia_vida_entradas",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_historia_vida",
                table: "historia_vida",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_contacto",
                table: "contacto",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_estadia_casa_convivencia",
                table: "estadia_casa_convivencia",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_asistencia",
                table: "asistencia",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_asistencia_persona_PersonId",
                table: "asistencia",
                column: "PersonId",
                principalTable: "persona",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_contacto_ficha_social_SocialRecordId",
                table: "contacto",
                column: "SocialRecordId",
                principalTable: "ficha_social",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_estadia_casa_convivencia_persona_PersonId",
                table: "estadia_casa_convivencia",
                column: "PersonId",
                principalTable: "persona",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ficha_social_persona_PersonId",
                table: "ficha_social",
                column: "PersonId",
                principalTable: "persona",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_historia_vida_persona_persona_id",
                table: "historia_vida",
                column: "persona_id",
                principalTable: "persona",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_historia_vida_usuario_antes_hogar_usuario_id",
                table: "historia_vida",
                column: "antes_hogar_usuario_id",
                principalTable: "usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_historia_vida_usuario_despues_hogar_usuario_id",
                table: "historia_vida",
                column: "despues_hogar_usuario_id",
                principalTable: "usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_historia_vida_usuario_en_hogar_usuario_id",
                table: "historia_vida",
                column: "en_hogar_usuario_id",
                principalTable: "usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_historia_vida_entradas_persona_persona_id",
                table: "historia_vida_entradas",
                column: "persona_id",
                principalTable: "persona",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_historia_vida_entradas_usuario_usuario_id",
                table: "historia_vida_entradas",
                column: "usuario_id",
                principalTable: "usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_historial_tipo_persona_persona_persona_id",
                table: "historial_tipo_persona",
                column: "persona_id",
                principalTable: "persona",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_historial_tipo_persona_usuario_usuario_id",
                table: "historial_tipo_persona",
                column: "usuario_id",
                principalTable: "usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_observacion_categoria_observacion_categoria_id",
                table: "observacion",
                column: "categoria_id",
                principalTable: "categoria_observacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_observacion_persona_persona_id",
                table: "observacion",
                column: "persona_id",
                principalTable: "persona",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_observacion_usuario_usuario_id",
                table: "observacion",
                column: "usuario_id",
                principalTable: "usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_psychiatric_evaluation_persona_PersonId",
                table: "psychiatric_evaluation",
                column: "PersonId",
                principalTable: "persona",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_psychiatric_evaluation_usuario_RegisteredByUserId",
                table: "psychiatric_evaluation",
                column: "RegisteredByUserId",
                principalTable: "usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_usuario_rol_RoleId",
                table: "usuario",
                column: "RoleId",
                principalTable: "rol",
                principalColumn: "Id");
        }
    }
}
