using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotOps.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentPortOnePaymentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PortOnePaymentId",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """UPDATE "Payments" SET "PortOnePaymentId" = 'migrated-' || "Id"::text WHERE "PortOnePaymentId" IS NULL;""");

            migrationBuilder.AlterColumn<string>(
                name: "PortOnePaymentId",
                table: "Payments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PortOnePaymentId",
                table: "Payments",
                column: "PortOnePaymentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_PortOnePaymentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PortOnePaymentId",
                table: "Payments");
        }
    }
}
