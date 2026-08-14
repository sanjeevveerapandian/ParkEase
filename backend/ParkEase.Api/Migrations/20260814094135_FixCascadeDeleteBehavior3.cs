using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkEase.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeDeleteBehavior3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingLots_Users_OperatorId",
                table: "ParkingLots");

            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSlots_ParkingLots_ParkingLotId",
                table: "ParkingSlots");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingLots_Users_OperatorId",
                table: "ParkingLots",
                column: "OperatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSlots_ParkingLots_ParkingLotId",
                table: "ParkingSlots",
                column: "ParkingLotId",
                principalTable: "ParkingLots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingLots_Users_OperatorId",
                table: "ParkingLots");

            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSlots_ParkingLots_ParkingLotId",
                table: "ParkingSlots");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingLots_Users_OperatorId",
                table: "ParkingLots",
                column: "OperatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSlots_ParkingLots_ParkingLotId",
                table: "ParkingSlots",
                column: "ParkingLotId",
                principalTable: "ParkingLots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
