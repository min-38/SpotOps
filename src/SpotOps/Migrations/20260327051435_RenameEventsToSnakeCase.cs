using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotOps.Migrations
{
    /// <inheritdoc />
    public partial class RenameEventsToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Organizers_OrganizerId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_Events_EventId",
                table: "QueueEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Events_EventId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Seats_Events_EventId",
                table: "Seats");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Events",
                table: "Events");

            migrationBuilder.RenameTable(
                name: "Events",
                newName: "events");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "events",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "events",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "events",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "events",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VenueName",
                table: "events",
                newName: "venue_name");

            migrationBuilder.RenameColumn(
                name: "TotalCapacity",
                table: "events",
                newName: "total_capacity");

            migrationBuilder.RenameColumn(
                name: "TicketType",
                table: "events",
                newName: "ticket_type");

            migrationBuilder.RenameColumn(
                name: "SaleStartAt",
                table: "events",
                newName: "sale_start_at");

            migrationBuilder.RenameColumn(
                name: "SaleEndAt",
                table: "events",
                newName: "sale_end_at");

            migrationBuilder.RenameColumn(
                name: "OrganizerId",
                table: "events",
                newName: "organizer_id");

            migrationBuilder.RenameColumn(
                name: "EventAt",
                table: "events",
                newName: "event_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "events",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Events_OrganizerId",
                table: "events",
                newName: "ix_events_organizer_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_events",
                table: "events",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_events_organizers_organizer_id",
                table: "events",
                column: "organizer_id",
                principalTable: "Organizers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_events_EventId",
                table: "QueueEntries",
                column: "EventId",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_events_EventId",
                table: "Reservations",
                column: "EventId",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Seats_events_EventId",
                table: "Seats",
                column: "EventId",
                principalTable: "events",
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
                name: "FK_QueueEntries_events_EventId",
                table: "QueueEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_events_EventId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Seats_events_EventId",
                table: "Seats");

            migrationBuilder.DropPrimaryKey(
                name: "PK_events",
                table: "events");

            migrationBuilder.RenameTable(
                name: "events",
                newName: "Events");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Events",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "Events",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Events",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Events",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "venue_name",
                table: "Events",
                newName: "VenueName");

            migrationBuilder.RenameColumn(
                name: "total_capacity",
                table: "Events",
                newName: "TotalCapacity");

            migrationBuilder.RenameColumn(
                name: "ticket_type",
                table: "Events",
                newName: "TicketType");

            migrationBuilder.RenameColumn(
                name: "sale_start_at",
                table: "Events",
                newName: "SaleStartAt");

            migrationBuilder.RenameColumn(
                name: "sale_end_at",
                table: "Events",
                newName: "SaleEndAt");

            migrationBuilder.RenameColumn(
                name: "organizer_id",
                table: "Events",
                newName: "OrganizerId");

            migrationBuilder.RenameColumn(
                name: "event_at",
                table: "Events",
                newName: "EventAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Events",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_events_organizer_id",
                table: "Events",
                newName: "IX_Events_OrganizerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Events",
                table: "Events",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Organizers_OrganizerId",
                table: "Events",
                column: "OrganizerId",
                principalTable: "Organizers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_Events_EventId",
                table: "QueueEntries",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Events_EventId",
                table: "Reservations",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Seats_Events_EventId",
                table: "Seats",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
