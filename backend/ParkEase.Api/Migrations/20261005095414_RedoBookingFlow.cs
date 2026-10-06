using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkEase.Api.Migrations
{
    /// <inheritdoc />
    // Scaffolded, then reordered by hand so existing rows survive: every new NOT NULL column is
    // added nullable, backfilled from the data already there, and only then tightened and indexed.
    public partial class RedoBookingFlow : Migration
    {
        // "tn 09-ab 1234" -> "TN09AB1234", same rule as VehicleNumber.Normalize in code.
        private const string NormalizedPlate = "UPPER(REPLACE(REPLACE({0}, ' ', ''), '-', ''))";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------------------------------------------------------- Notifications ----
            // New columns first, backfill from IsSent, and only then drop IsSent.
            migrationBuilder.AddColumn<string>(name: "Error", table: "Notifications", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Message", table: "Notifications", type: "nvarchar(2000)", maxLength: 2000, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Provider", table: "Notifications", type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<int>(name: "Status", table: "Notifications", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<string>(name: "Subject", table: "Notifications", type: "nvarchar(200)", maxLength: 200, nullable: true);

            // Status: Sent = 1, Failed = 2. Every pre-existing notification was an email sent through Brevo.
            migrationBuilder.Sql(@"
                UPDATE Notifications SET
                    Status   = CASE WHEN IsSent = 1 THEN 1 ELSE 2 END,
                    Provider = CASE WHEN Channel = 0 THEN 'Brevo' ELSE 'Log' END,
                    Message  = CASE Type WHEN 0 THEN 'Booking confirmation' WHEN 1 THEN 'Exit receipt' ELSE 'Notification' END
                               + ' (sent before message history was recorded)',
                    Error    = CASE WHEN IsSent = 1 THEN NULL ELSE 'Delivery failed; details were not recorded at the time.' END");

            migrationBuilder.DropColumn(name: "IsSent", table: "Notifications");
            migrationBuilder.RenameColumn(name: "SentAt", table: "Notifications", newName: "CreatedAt");
            migrationBuilder.AlterColumn<string>(
                name: "Recipient", table: "Notifications", type: "nvarchar(256)", maxLength: 256, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(max)");

            migrationBuilder.DropIndex(name: "IX_Notifications_VehicleBookingId", table: "Notifications");
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_VehicleBookingId_Type", table: "Notifications",
                columns: new[] { "VehicleBookingId", "Type" });

            // ------------------------------------------------------------- Vehicles ----
            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_Users_OwnerId", column: x => x.OwnerId,
                        principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_OwnerId_VehicleNumber", table: "Vehicles",
                columns: new[] { "OwnerId", "VehicleNumber" }, unique: true);

            // ---------------------------------------------------------- ParkingSlots ----
            migrationBuilder.AlterColumn<string>(
                name: "SlotNumber", table: "ParkingSlots", type: "nvarchar(20)", maxLength: 20, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(max)");
            // Existing slots default to 0 = Car.
            migrationBuilder.AddColumn<int>(name: "VehicleType", table: "ParkingSlots", type: "int", nullable: false, defaultValue: 0);

            migrationBuilder.DropIndex(name: "IX_ParkingSlots_ParkingLotId", table: "ParkingSlots");
            migrationBuilder.CreateIndex(
                name: "IX_ParkingSlots_ParkingLotId_SlotNumber", table: "ParkingSlots",
                columns: new[] { "ParkingLotId", "SlotNumber" }, unique: true);

            // -------------------------------------------------------- VehicleBookings ----
            migrationBuilder.AddColumn<DateTime>(name: "CancelledAt", table: "VehicleBookings", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "ReservationExpiresAt", table: "VehicleBookings", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "ReservedAt", table: "VehicleBookings", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<int>(name: "VehicleId", table: "VehicleBookings", type: "int", nullable: true);

            // One Vehicle per distinct (driver, plate) found in the booking history...
            migrationBuilder.Sql($@"
                INSERT INTO Vehicles (OwnerId, VehicleNumber, Type, IsActive, CreatedAt)
                SELECT UserId, {string.Format(NormalizedPlate, "VehicleNumber")}, 0, 1, MIN(EntryTime)
                FROM VehicleBookings
                GROUP BY UserId, {string.Format(NormalizedPlate, "VehicleNumber")}");

            // ...then point each booking at its vehicle. Old bookings were reserved and parked in
            // one step, so their reservation time is their entry time.
            migrationBuilder.Sql($@"
                UPDATE b SET
                    b.VehicleNumber = v.VehicleNumber,
                    b.VehicleId     = v.Id,
                    b.ReservedAt    = b.EntryTime
                FROM VehicleBookings b
                JOIN Vehicles v ON v.OwnerId = b.UserId
                               AND v.VehicleNumber = {string.Format(NormalizedPlate, "b.VehicleNumber")}");

            migrationBuilder.AlterColumn<int>(
                name: "VehicleId", table: "VehicleBookings", type: "int", nullable: false,
                oldClrType: typeof(int), oldType: "int", oldNullable: true);
            migrationBuilder.AlterColumn<DateTime>(
                name: "ReservedAt", table: "VehicleBookings", type: "datetime2", nullable: false,
                oldClrType: typeof(DateTime), oldType: "datetime2", oldNullable: true);
            migrationBuilder.AlterColumn<DateTime>(
                name: "EntryTime", table: "VehicleBookings", type: "datetime2", nullable: true,
                oldClrType: typeof(DateTime), oldType: "datetime2");
            migrationBuilder.AlterColumn<string>(
                name: "VehicleNumber", table: "VehicleBookings", type: "nvarchar(15)", maxLength: 15, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(max)");

            // "Live" = Active (0) or Reserved (4): at most one live booking per slot and per vehicle.
            migrationBuilder.DropIndex(name: "IX_VehicleBookings_ParkingSlotId", table: "VehicleBookings");
            migrationBuilder.CreateIndex(
                name: "UX_VehicleBookings_OneLiveBookingPerSlot", table: "VehicleBookings",
                column: "ParkingSlotId", unique: true, filter: "[Status] IN (0, 4)");
            migrationBuilder.CreateIndex(
                name: "UX_VehicleBookings_OneLiveBookingPerVehicle", table: "VehicleBookings",
                column: "VehicleId", unique: true, filter: "[Status] IN (0, 4)");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleBookings_Vehicles_VehicleId", table: "VehicleBookings", column: "VehicleId",
                principalTable: "Vehicles", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_VehicleBookings_Vehicles_VehicleId", table: "VehicleBookings");
            migrationBuilder.DropIndex(name: "UX_VehicleBookings_OneLiveBookingPerSlot", table: "VehicleBookings");
            migrationBuilder.DropIndex(name: "UX_VehicleBookings_OneLiveBookingPerVehicle", table: "VehicleBookings");
            migrationBuilder.DropIndex(name: "IX_ParkingSlots_ParkingLotId_SlotNumber", table: "ParkingSlots");
            migrationBuilder.DropIndex(name: "IX_Notifications_VehicleBookingId_Type", table: "Notifications");

            // Bookings that were never checked in have no entry time in the old schema; use the reservation time.
            migrationBuilder.Sql("UPDATE VehicleBookings SET EntryTime = ReservedAt WHERE EntryTime IS NULL");
            migrationBuilder.AlterColumn<DateTime>(
                name: "EntryTime", table: "VehicleBookings", type: "datetime2", nullable: false,
                oldClrType: typeof(DateTime), oldType: "datetime2", oldNullable: true);
            migrationBuilder.AlterColumn<string>(
                name: "VehicleNumber", table: "VehicleBookings", type: "nvarchar(max)", nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(15)", oldMaxLength: 15);
            migrationBuilder.DropColumn(name: "CancelledAt", table: "VehicleBookings");
            migrationBuilder.DropColumn(name: "ReservationExpiresAt", table: "VehicleBookings");
            migrationBuilder.DropColumn(name: "ReservedAt", table: "VehicleBookings");
            migrationBuilder.DropColumn(name: "VehicleId", table: "VehicleBookings");
            migrationBuilder.CreateIndex(name: "IX_VehicleBookings_ParkingSlotId", table: "VehicleBookings", column: "ParkingSlotId");

            migrationBuilder.DropTable(name: "Vehicles");

            migrationBuilder.DropColumn(name: "VehicleType", table: "ParkingSlots");
            migrationBuilder.AlterColumn<string>(
                name: "SlotNumber", table: "ParkingSlots", type: "nvarchar(max)", nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(20)", oldMaxLength: 20);
            migrationBuilder.CreateIndex(name: "IX_ParkingSlots_ParkingLotId", table: "ParkingSlots", column: "ParkingLotId");

            migrationBuilder.AddColumn<bool>(name: "IsSent", table: "Notifications", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.Sql("UPDATE Notifications SET IsSent = CASE WHEN Status = 1 THEN 1 ELSE 0 END");
            migrationBuilder.RenameColumn(name: "CreatedAt", table: "Notifications", newName: "SentAt");
            migrationBuilder.AlterColumn<string>(
                name: "Recipient", table: "Notifications", type: "nvarchar(max)", nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(256)", oldMaxLength: 256);
            migrationBuilder.DropColumn(name: "Error", table: "Notifications");
            migrationBuilder.DropColumn(name: "Message", table: "Notifications");
            migrationBuilder.DropColumn(name: "Provider", table: "Notifications");
            migrationBuilder.DropColumn(name: "Status", table: "Notifications");
            migrationBuilder.DropColumn(name: "Subject", table: "Notifications");
            migrationBuilder.CreateIndex(name: "IX_Notifications_VehicleBookingId", table: "Notifications", column: "VehicleBookingId");
        }
    }
}
