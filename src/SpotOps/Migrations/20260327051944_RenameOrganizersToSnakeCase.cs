using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotOps.Migrations
{
    /// <inheritdoc />
    public partial class RenameOrganizersToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_events_organizers_organizer_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizers_users_UserId",
                table: "Organizers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Organizers",
                table: "Organizers");

            migrationBuilder.RenameTable(
                name: "Organizers",
                newName: "organizers");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "organizers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "organizers",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "IsVerified",
                table: "organizers",
                newName: "is_verified");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "organizers",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CompanyName",
                table: "organizers",
                newName: "company_name");

            migrationBuilder.RenameColumn(
                name: "BusinessNumber",
                table: "organizers",
                newName: "business_number");

            migrationBuilder.RenameIndex(
                name: "IX_Organizers_UserId",
                table: "organizers",
                newName: "ix_organizers_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_Organizers_BusinessNumber",
                table: "organizers",
                newName: "ix_organizers_business_number");

            migrationBuilder.AddPrimaryKey(
                name: "PK_organizers",
                table: "organizers",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_organizers_users_user_id",
                table: "organizers",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_events_organizers_organizer_id",
                table: "events",
                column: "organizer_id",
                principalTable: "organizers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_events_organizers_organizer_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_organizers_users_user_id",
                table: "organizers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_organizers",
                table: "organizers");

            migrationBuilder.RenameTable(
                name: "organizers",
                newName: "Organizers");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Organizers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "Organizers",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "is_verified",
                table: "Organizers",
                newName: "IsVerified");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Organizers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "company_name",
                table: "Organizers",
                newName: "CompanyName");

            migrationBuilder.RenameColumn(
                name: "business_number",
                table: "Organizers",
                newName: "BusinessNumber");

            migrationBuilder.RenameIndex(
                name: "ix_organizers_user_id",
                table: "Organizers",
                newName: "IX_Organizers_UserId");

            migrationBuilder.RenameIndex(
                name: "ix_organizers_business_number",
                table: "Organizers",
                newName: "IX_Organizers_BusinessNumber");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Organizers",
                table: "Organizers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizers_users_UserId",
                table: "Organizers",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_events_organizers_organizer_id",
                table: "events",
                column: "organizer_id",
                principalTable: "Organizers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
