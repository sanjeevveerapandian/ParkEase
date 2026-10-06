using ParkEase.Api.DTOs;
using ParkEase.Api.Models;
using ParkEase.Api.Services;
using ParkEase.Api.Tests.Support;

namespace ParkEase.Api.Tests
{
    public class ParkingLotServiceTests
    {
        private readonly TestWorld _w = new();

        private static CreateParkingSlotDto NewSlot(string number, string type = "Car", decimal rate = 30m) =>
            new() { SlotNumber = number, HourlyRate = rate, VehicleType = type };

        // ------------------------------------------------------------------ lots ----

        [Fact]
        public async Task Operators_see_only_their_own_lots_and_admins_see_all()
        {
            await _w.Lots().CreateLotAsync(new CreateParkingLotDto { Name = "Other Lot", Address = "2 Elsewhere" }, _w.OtherOperator.Id);

            var asOperator = await _w.Lots().GetLotsAsync(_w.OperatorActor);
            var asOther = await _w.Lots().GetLotsAsync(_w.OtherOperatorActor);
            var asAdmin = await _w.Lots().GetLotsAsync(_w.AdminActor);

            Assert.Equal("Test Lot", Assert.Single(asOperator).Name);
            Assert.Equal("Other Lot", Assert.Single(asOther).Name);
            Assert.Equal(2, asAdmin.Count);
        }

        [Fact]
        public async Task A_new_lot_is_owned_by_the_creator_and_trimmed()
        {
            var lot = await _w.Lots().CreateLotAsync(new CreateParkingLotDto { Name = "  Beach Road  ", Address = " Marina " }, _w.Admin.Id);

            Assert.Equal(_w.Admin.Id, lot.OperatorId);
            Assert.Equal("Beach Road", lot.Name);
            Assert.Equal("Marina", lot.Address);
            Assert.Equal(0, lot.TotalSlots);
        }

        [Fact]
        public async Task Lot_counts_reflect_available_slots()
        {
            await _w.ReserveAsync();

            var lot = await _w.Lots().GetLotByIdAsync(_w.Lot.Id, _w.OperatorActor);

            Assert.Equal(3, lot!.TotalSlots);
            Assert.Equal(2, lot.AvailableSlots);
        }

        [Fact]
        public async Task A_lot_you_do_not_own_reads_as_not_found()
        {
            Assert.Null(await _w.Lots().GetLotByIdAsync(_w.Lot.Id, _w.OtherOperatorActor));
            Assert.NotNull(await _w.Lots().GetLotByIdAsync(_w.Lot.Id, _w.AdminActor));
        }

        [Fact]
        public async Task Browsing_lists_every_lot_for_any_driver()
        {
            await _w.Lots().CreateLotAsync(new CreateParkingLotDto { Name = "Other Lot", Address = "x" }, _w.OtherOperator.Id);

            Assert.Equal(2, (await _w.Lots().GetAllLotsForBrowsingAsync()).Count);
        }

        [Fact]
        public async Task A_lot_that_still_has_slots_cannot_be_deleted()
        {
            var result = await _w.Lots().DeleteLotAsync(_w.Lot.Id, _w.OperatorActor);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
            Assert.Contains("3 slot", result.Error.Message);
        }

        [Fact]
        public async Task An_empty_lot_can_be_deleted_by_its_owner_but_not_by_a_stranger()
        {
            var lot = await _w.Lots().CreateLotAsync(new CreateParkingLotDto { Name = "Empty", Address = "x" }, _w.Operator.Id);

            var stranger = await _w.Lots().DeleteLotAsync(lot.Id, _w.OtherOperatorActor);
            var owner = await _w.Lots().DeleteLotAsync(lot.Id, _w.OperatorActor);

            Assert.Equal(ServiceErrorCode.NotFound, stranger.Error!.Code);
            Assert.True(owner.Succeeded);
        }

        // ----------------------------------------------------------------- slots ----

        [Fact]
        public async Task A_slot_is_created_available_with_its_vehicle_type()
        {
            var result = await _w.Lots().CreateSlotAsync(_w.Lot.Id, NewSlot("c7", "Bike", 15m), _w.OperatorActor);

            Assert.True(result.Succeeded);
            Assert.Equal("C7", result.Value!.SlotNumber);   // upper-cased
            Assert.Equal("Bike", result.Value.VehicleType);
            Assert.Equal("Available", result.Value.Status);
            Assert.Equal(15m, result.Value.HourlyRate);
        }

        [Fact]
        public async Task Slot_numbers_are_unique_within_a_lot_ignoring_case()
        {
            var result = await _w.Lots().CreateSlotAsync(_w.Lot.Id, NewSlot("a1"), _w.OperatorActor);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
        }

        [Fact]
        public async Task The_same_slot_number_may_exist_in_different_lots()
        {
            var other = await _w.Lots().CreateLotAsync(new CreateParkingLotDto { Name = "Other", Address = "x" }, _w.Operator.Id);

            var result = await _w.Lots().CreateSlotAsync(other.Id, NewSlot("A1"), _w.OperatorActor);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task Only_the_owner_or_an_admin_can_add_slots()
        {
            var stranger = await _w.Lots().CreateSlotAsync(_w.Lot.Id, NewSlot("Z1"), _w.OtherOperatorActor);
            var admin = await _w.Lots().CreateSlotAsync(_w.Lot.Id, NewSlot("Z2"), _w.AdminActor);

            Assert.Equal(ServiceErrorCode.Forbidden, stranger.Error!.Code);
            Assert.True(admin.Succeeded);
        }

        [Fact]
        public async Task Adding_a_slot_to_a_missing_lot_is_not_found()
        {
            var result = await _w.Lots().CreateSlotAsync(9999, NewSlot("A1"), _w.AdminActor);

            Assert.Equal(ServiceErrorCode.NotFound, result.Error!.Code);
        }

        [Fact]
        public async Task Slots_of_a_lot_are_listed_in_order_with_type_and_status()
        {
            var slots = await _w.Lots().GetSlotsForLotAsync(_w.Lot.Id);

            Assert.Equal(new[] { "A1", "A2", "B1" }, slots.Select(s => s.SlotNumber));
            Assert.Equal("Bike", slots.Last().VehicleType);
        }

        // --------------------------------------------------------- slot status ----

        [Fact]
        public async Task A_free_slot_can_go_to_maintenance_and_back()
        {
            var down = await _w.Lots().UpdateSlotStatusAsync(_w.Lot.Id, _w.CarSlot.Id, "Maintenance", _w.OperatorActor);
            var up = await _w.Lots().UpdateSlotStatusAsync(_w.Lot.Id, _w.CarSlot.Id, "available", _w.OperatorActor);

            Assert.Equal("Maintenance", down.Value!.Status);
            Assert.Equal("Available", up.Value!.Status);
        }

        [Fact]
        public async Task A_reserved_or_occupied_slot_cannot_be_put_in_maintenance()
        {
            await _w.ReserveAsync();
            var reserved = await _w.Lots().UpdateSlotStatusAsync(_w.Lot.Id, _w.CarSlot.Id, "Maintenance", _w.OperatorActor);

            Assert.Equal(ServiceErrorCode.Conflict, reserved.Error!.Code);
            Assert.Equal(SlotStatus.Reserved, _w.Reload(_w.CarSlot).Status);
        }

        [Fact]
        public async Task Setting_a_slot_to_the_status_it_already_has_is_a_conflict()
        {
            var result = await _w.Lots().UpdateSlotStatusAsync(_w.Lot.Id, _w.CarSlot.Id, "Available", _w.OperatorActor);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
            Assert.Contains("already", result.Error.Message);
        }

        [Fact]
        public async Task Operators_cannot_change_status_in_lots_they_do_not_own()
        {
            var result = await _w.Lots().UpdateSlotStatusAsync(_w.Lot.Id, _w.CarSlot.Id, "Maintenance", _w.OtherOperatorActor);

            Assert.Equal(ServiceErrorCode.Forbidden, result.Error!.Code);
        }

        [Fact]
        public async Task A_slot_id_from_a_different_lot_is_not_found()
        {
            var other = await _w.Lots().CreateLotAsync(new CreateParkingLotDto { Name = "Other", Address = "x" }, _w.Operator.Id);

            var result = await _w.Lots().UpdateSlotStatusAsync(other.Id, _w.CarSlot.Id, "Maintenance", _w.OperatorActor);

            Assert.Equal(ServiceErrorCode.NotFound, result.Error!.Code);
        }

        // --------------------------------------------------------- slot deletion ----

        [Fact]
        public async Task An_unused_slot_can_be_deleted()
        {
            var result = await _w.Lots().DeleteSlotAsync(_w.Lot.Id, _w.CarSlot2.Id, _w.OperatorActor);

            Assert.True(result.Succeeded);
            Assert.False(_w.Db.ParkingSlots.Any(s => s.Id == _w.CarSlot2.Id));
        }

        [Fact]
        public async Task A_held_slot_cannot_be_deleted()
        {
            await _w.ReserveAsync();

            var result = await _w.Lots().DeleteSlotAsync(_w.Lot.Id, _w.CarSlot.Id, _w.OperatorActor);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
        }

        [Fact]
        public async Task A_slot_with_booking_history_cannot_be_deleted()
        {
            var booking = await _w.ReserveAndCheckInAsync();
            await _w.Bookings().ExitAsync(booking.Id, _w.DriverActor);

            var result = await _w.Lots().DeleteSlotAsync(_w.Lot.Id, _w.CarSlot.Id, _w.OperatorActor);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
            Assert.Contains("history", result.Error.Message);
        }

        [Fact]
        public async Task Operators_cannot_delete_slots_in_lots_they_do_not_own()
        {
            var result = await _w.Lots().DeleteSlotAsync(_w.Lot.Id, _w.CarSlot2.Id, _w.OtherOperatorActor);

            Assert.Equal(ServiceErrorCode.Forbidden, result.Error!.Code);
        }
    }
}
