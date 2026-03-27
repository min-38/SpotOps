using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotOps.Migrations
{
    /// <inheritdoc />
    public partial class RenameRemainingTablesToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PasswordResetTokens_users_UserId",
                table: "PasswordResetTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Reservations_ReservationId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_events_EventId",
                table: "QueueEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_users_UserId",
                table: "QueueEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Seats_SeatId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_events_EventId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_users_UserId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Seats_events_EventId",
                table: "Seats");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Reservations_ReservationId",
                table: "Tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Seats",
                table: "Seats");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reservations",
                table: "Reservations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payments",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QueueEntries",
                table: "QueueEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PasswordResetTokens",
                table: "PasswordResetTokens");

            migrationBuilder.RenameTable(
                name: "Tickets",
                newName: "tickets");

            migrationBuilder.RenameTable(
                name: "Seats",
                newName: "seats");

            migrationBuilder.RenameTable(
                name: "Reservations",
                newName: "reservations");

            migrationBuilder.RenameTable(
                name: "Payments",
                newName: "payments");

            migrationBuilder.RenameTable(
                name: "QueueEntries",
                newName: "queue_entries");

            migrationBuilder.RenameTable(
                name: "PasswordResetTokens",
                newName: "password_reset_tokens");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "tickets",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UsedAt",
                table: "tickets",
                newName: "used_at");

            migrationBuilder.RenameColumn(
                name: "ReservationId",
                table: "tickets",
                newName: "reservation_id");

            migrationBuilder.RenameColumn(
                name: "QrToken",
                table: "tickets",
                newName: "qr_token");

            migrationBuilder.RenameColumn(
                name: "IssuedAt",
                table: "tickets",
                newName: "issued_at");

            migrationBuilder.RenameColumn(
                name: "IsUsed",
                table: "tickets",
                newName: "is_used");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_ReservationId",
                table: "tickets",
                newName: "ix_tickets_reservation_id");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_QrToken",
                table: "tickets",
                newName: "ix_tickets_qr_token");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "seats",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Section",
                table: "seats",
                newName: "section");

            migrationBuilder.RenameColumn(
                name: "Row",
                table: "seats",
                newName: "row");

            migrationBuilder.RenameColumn(
                name: "Number",
                table: "seats",
                newName: "number");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "seats",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "seats",
                newName: "row_version");

            migrationBuilder.RenameColumn(
                name: "EventId",
                table: "seats",
                newName: "event_id");

            migrationBuilder.RenameIndex(
                name: "IX_Seats_EventId",
                table: "seats",
                newName: "ix_seats_event_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "reservations",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "reservations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "reservations",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "SeatId",
                table: "reservations",
                newName: "seat_id");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "reservations",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "EventId",
                table: "reservations",
                newName: "event_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "reservations",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_UserId",
                table: "reservations",
                newName: "ix_reservations_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_SeatId",
                table: "reservations",
                newName: "ix_reservations_seat_id");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_EventId",
                table: "reservations",
                newName: "ix_reservations_event_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "payments",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "payments",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "payments",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ReservationId",
                table: "payments",
                newName: "reservation_id");

            migrationBuilder.RenameColumn(
                name: "PortOnePaymentId",
                table: "payments",
                newName: "port_one_payment_id");

            migrationBuilder.RenameColumn(
                name: "PgTransactionId",
                table: "payments",
                newName: "pg_transaction_id");

            migrationBuilder.RenameColumn(
                name: "PaidAt",
                table: "payments",
                newName: "paid_at");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_ReservationId",
                table: "payments",
                newName: "ix_payments_reservation_id");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_PortOnePaymentId",
                table: "payments",
                newName: "ix_payments_port_one_payment_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "queue_entries",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Position",
                table: "queue_entries",
                newName: "position");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "queue_entries",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "queue_entries",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "EventId",
                table: "queue_entries",
                newName: "event_id");

            migrationBuilder.RenameColumn(
                name: "EnteredAt",
                table: "queue_entries",
                newName: "entered_at");

            migrationBuilder.RenameIndex(
                name: "IX_QueueEntries_UserId",
                table: "queue_entries",
                newName: "ix_queue_entries_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_QueueEntries_EventId_UserId",
                table: "queue_entries",
                newName: "ix_queue_entries_event_id_user_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "password_reset_tokens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "password_reset_tokens",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UsedAt",
                table: "password_reset_tokens",
                newName: "used_at");

            migrationBuilder.RenameColumn(
                name: "TokenHash",
                table: "password_reset_tokens",
                newName: "token_hash");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "password_reset_tokens",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "password_reset_tokens",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "password_reset_tokens",
                newName: "ix_password_reset_tokens_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "password_reset_tokens",
                newName: "ix_password_reset_tokens_token_hash");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tickets",
                table: "tickets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seats",
                table: "seats",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_reservations",
                table: "reservations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payments",
                table: "payments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_queue_entries",
                table: "queue_entries",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_password_reset_tokens",
                table: "password_reset_tokens",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_password_reset_tokens_users_user_id",
                table: "password_reset_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_payments_reservations_reservation_id",
                table: "payments",
                column: "reservation_id",
                principalTable: "reservations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_queue_entries_events_event_id",
                table: "queue_entries",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_queue_entries_users_user_id",
                table: "queue_entries",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_reservations_events_event_id",
                table: "reservations",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_reservations_seats_seat_id",
                table: "reservations",
                column: "seat_id",
                principalTable: "seats",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_reservations_users_user_id",
                table: "reservations",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_seats_events_event_id",
                table: "seats",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_reservations_reservation_id",
                table: "tickets",
                column: "reservation_id",
                principalTable: "reservations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_password_reset_tokens_users_user_id",
                table: "password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_payments_reservations_reservation_id",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "fk_queue_entries_events_event_id",
                table: "queue_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_queue_entries_users_user_id",
                table: "queue_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_reservations_events_event_id",
                table: "reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_reservations_seats_seat_id",
                table: "reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_reservations_users_user_id",
                table: "reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_seats_events_event_id",
                table: "seats");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_reservations_reservation_id",
                table: "tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tickets",
                table: "tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seats",
                table: "seats");

            migrationBuilder.DropPrimaryKey(
                name: "PK_reservations",
                table: "reservations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payments",
                table: "payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_queue_entries",
                table: "queue_entries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_password_reset_tokens",
                table: "password_reset_tokens");

            migrationBuilder.RenameTable(
                name: "tickets",
                newName: "Tickets");

            migrationBuilder.RenameTable(
                name: "seats",
                newName: "Seats");

            migrationBuilder.RenameTable(
                name: "reservations",
                newName: "Reservations");

            migrationBuilder.RenameTable(
                name: "payments",
                newName: "Payments");

            migrationBuilder.RenameTable(
                name: "queue_entries",
                newName: "QueueEntries");

            migrationBuilder.RenameTable(
                name: "password_reset_tokens",
                newName: "PasswordResetTokens");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Tickets",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "used_at",
                table: "Tickets",
                newName: "UsedAt");

            migrationBuilder.RenameColumn(
                name: "reservation_id",
                table: "Tickets",
                newName: "ReservationId");

            migrationBuilder.RenameColumn(
                name: "qr_token",
                table: "Tickets",
                newName: "QrToken");

            migrationBuilder.RenameColumn(
                name: "issued_at",
                table: "Tickets",
                newName: "IssuedAt");

            migrationBuilder.RenameColumn(
                name: "is_used",
                table: "Tickets",
                newName: "IsUsed");

            migrationBuilder.RenameIndex(
                name: "ix_tickets_reservation_id",
                table: "Tickets",
                newName: "IX_Tickets_ReservationId");

            migrationBuilder.RenameIndex(
                name: "ix_tickets_qr_token",
                table: "Tickets",
                newName: "IX_Tickets_QrToken");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Seats",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "section",
                table: "Seats",
                newName: "Section");

            migrationBuilder.RenameColumn(
                name: "row",
                table: "Seats",
                newName: "Row");

            migrationBuilder.RenameColumn(
                name: "number",
                table: "Seats",
                newName: "Number");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Seats",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "row_version",
                table: "Seats",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "event_id",
                table: "Seats",
                newName: "EventId");

            migrationBuilder.RenameIndex(
                name: "ix_seats_event_id",
                table: "Seats",
                newName: "IX_Seats_EventId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Reservations",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Reservations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "Reservations",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "seat_id",
                table: "Reservations",
                newName: "SeatId");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                table: "Reservations",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "event_id",
                table: "Reservations",
                newName: "EventId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Reservations",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_reservations_user_id",
                table: "Reservations",
                newName: "IX_Reservations_UserId");

            migrationBuilder.RenameIndex(
                name: "ix_reservations_seat_id",
                table: "Reservations",
                newName: "IX_Reservations_SeatId");

            migrationBuilder.RenameIndex(
                name: "ix_reservations_event_id",
                table: "Reservations",
                newName: "IX_Reservations_EventId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Payments",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "Payments",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Payments",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "reservation_id",
                table: "Payments",
                newName: "ReservationId");

            migrationBuilder.RenameColumn(
                name: "port_one_payment_id",
                table: "Payments",
                newName: "PortOnePaymentId");

            migrationBuilder.RenameColumn(
                name: "pg_transaction_id",
                table: "Payments",
                newName: "PgTransactionId");

            migrationBuilder.RenameColumn(
                name: "paid_at",
                table: "Payments",
                newName: "PaidAt");

            migrationBuilder.RenameIndex(
                name: "ix_payments_reservation_id",
                table: "Payments",
                newName: "IX_Payments_ReservationId");

            migrationBuilder.RenameIndex(
                name: "ix_payments_port_one_payment_id",
                table: "Payments",
                newName: "IX_Payments_PortOnePaymentId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "QueueEntries",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "position",
                table: "QueueEntries",
                newName: "Position");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "QueueEntries",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "QueueEntries",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "event_id",
                table: "QueueEntries",
                newName: "EventId");

            migrationBuilder.RenameColumn(
                name: "entered_at",
                table: "QueueEntries",
                newName: "EnteredAt");

            migrationBuilder.RenameIndex(
                name: "ix_queue_entries_user_id",
                table: "QueueEntries",
                newName: "IX_QueueEntries_UserId");

            migrationBuilder.RenameIndex(
                name: "ix_queue_entries_event_id_user_id",
                table: "QueueEntries",
                newName: "IX_QueueEntries_EventId_UserId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "PasswordResetTokens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "PasswordResetTokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "used_at",
                table: "PasswordResetTokens",
                newName: "UsedAt");

            migrationBuilder.RenameColumn(
                name: "token_hash",
                table: "PasswordResetTokens",
                newName: "TokenHash");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                table: "PasswordResetTokens",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "PasswordResetTokens",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_password_reset_tokens_user_id",
                table: "PasswordResetTokens",
                newName: "IX_PasswordResetTokens_UserId");

            migrationBuilder.RenameIndex(
                name: "ix_password_reset_tokens_token_hash",
                table: "PasswordResetTokens",
                newName: "IX_PasswordResetTokens_TokenHash");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Seats",
                table: "Seats",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reservations",
                table: "Reservations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payments",
                table: "Payments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QueueEntries",
                table: "QueueEntries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PasswordResetTokens",
                table: "PasswordResetTokens",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordResetTokens_users_UserId",
                table: "PasswordResetTokens",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Reservations_ReservationId",
                table: "Payments",
                column: "ReservationId",
                principalTable: "Reservations",
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
                name: "FK_QueueEntries_users_UserId",
                table: "QueueEntries",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Seats_SeatId",
                table: "Reservations",
                column: "SeatId",
                principalTable: "Seats",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_events_EventId",
                table: "Reservations",
                column: "EventId",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_users_UserId",
                table: "Reservations",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Seats_events_EventId",
                table: "Seats",
                column: "EventId",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Reservations_ReservationId",
                table: "Tickets",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
