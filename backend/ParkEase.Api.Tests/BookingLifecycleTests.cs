using ParkEase.Api.Models;
using ParkEase.Api.Services;
using ParkEase.Api.Tests.Support;

namespace ParkEase.Api.Tests
{
    // Steps 2 and 3 (check-in, exit), the Cancel side-exit, and who may do what.
    public class BookingLifecycleTests
    {
        private readonly TestWorld _w = new();

        // ------------------------------------------------------------------ check-in ----

        [Fact]
        public async Task Check_in_starts_billing_and_occupies_the_slot()
        {
            var reserved = await _w.ReserveAsync();
            _w.Clock.Advance(TimeSpan.FromMinutes(5));

            var result = await _w.Bookings().CheckInAsync(reserved.Id, _w.DriverActor);

            Assert.True(result.Succeeded);
            Assert.Equal("Active", result.Value!.Status);
            Assert.Equal(_w.Clock.GetUtcNow().UtcDateTime, result.Value.EntryTime);
            Assert.Null(result.Value.ReservationExpiresAt);
            Assert.Equal(SlotStatus.Occupied, _w.Reload(_w.CarSlot).Status);
            Assert.Contains((NotificationType.CheckIn, reserved.Id), _w.Notifications.Calls);
        }

        [Fact]
        public async Task The_operator_who_owns_the_lot_can_check_a_vehicle_in()
        {
            var reserved = await _w.ReserveAsync();

            var result = await _w.Bookings().CheckInAsync(reserved.Id, _w.OperatorActor);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task An_admin_can_check_a_vehicle_in()
        {
            var reserved = await _w.ReserveAsync();

            Assert.True((await _w.Bookings().CheckInAsync(reserved.Id, _w.AdminActor)).Succeeded);
        }

        [Fact]
        public async Task Another_driver_cannot_check_in_someone_elses_booking()
        {
            var reserved = await _w.ReserveAsync();

            var result = await _w.Bookings().CheckInAsync(reserved.Id, _w.OtherDriverActor);

            Assert.Equal(ServiceErrorCode.NotFound, result.Error!.Code);
            Assert.Equal(BookingStatus.Reserved, _w.ReloadBooking(reserved.Id).Status);
        }

        [Fact]
        public async Task An_operator_of_a_different_lot_cannot_check_in_the_booking()
        {
            var reserved = await _w.ReserveAsync();

            var result = await _w.Bookings().CheckInAsync(reserved.Id, _w.OtherOperatorActor);

            Assert.Equal(ServiceErrorCode.NotFound, result.Error!.Code);
        }

        [Fact]
        public async Task A_booking_cannot_be_checked_in_twice()
        {
            var active = await _w.ReserveAndCheckInAsync();

            var result = await _w.Bookings().CheckInAsync(active.Id, _w.DriverActor);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
            Assert.Contains("Active", result.Error.Message);
        }

        [Fact]
        public async Task Checking_in_after_the_hold_has_run_out_releases_the_slot_instead()
        {
            var reserved = await _w.ReserveAsync();
            _w.Clock.Advance(TimeSpan.FromMinutes(16));

            var result = await _w.Bookings().CheckInAsync(reserved.Id, _w.DriverActor);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
            Assert.Contains("expired", result.Error.Message);
            Assert.Equal(BookingStatus.Expired, _w.ReloadBooking(reserved.Id).Status);
            Assert.Equal(SlotStatus.Available, _w.Reload(_w.CarSlot).Status);
            Assert.Contains((NotificationType.ReservationExpired, reserved.Id), _w.Notifications.Calls);
        }

        [Fact]
        public async Task An_unknown_booking_is_not_found()
        {
            var result = await _w.Bookings().CheckInAsync(9999, _w.DriverActor);

            Assert.Equal(ServiceErrorCode.NotFound, result.Error!.Code);
        }

        // ---------------------------------------------------------------------- exit ----

        [Fact]
        public async Task Exit_charges_the_hourly_rate_rounded_up_and_frees_the_slot()
        {
            var active = await _w.ReserveAndCheckInAsync();
            _w.Clock.Advance(TimeSpan.FromMinutes(61));

            var result = await _w.Bookings().ExitAsync(active.Id, _w.DriverActor);

            Assert.True(result.Succeeded);
            Assert.Equal("Completed", result.Value!.Status);
            Assert.Equal(100m, result.Value.TotalFee);   // 61 min -> 2h * Rs.50
            Assert.Equal(0m, result.Value.PenaltyFee);
            Assert.Equal(_w.Clock.GetUtcNow().UtcDateTime, result.Value.ExitTime);
            Assert.Equal(SlotStatus.Available, _w.Reload(_w.CarSlot).Status);
            Assert.Contains((NotificationType.ExitReceipt, active.Id), _w.Notifications.Calls);
        }

        [Fact]
        public async Task Time_spent_only_reserved_is_not_billed()
        {
            var reserved = await _w.ReserveAsync();
            _w.Clock.Advance(TimeSpan.FromMinutes(10));           // 10 min of reservation hold
            await _w.Bookings().CheckInAsync(reserved.Id, _w.DriverActor);
            _w.Clock.Advance(TimeSpan.FromMinutes(30));           // 30 min actually parked

            var result = await _w.Bookings().ExitAsync(reserved.Id, _w.DriverActor);

            Assert.Equal(50m, result.Value!.TotalFee);            // 1 hour, not 40 minutes' worth of 2
        }

        [Fact]
        public async Task Parking_past_24_hours_is_billed_as_an_overstay()
        {
            var active = await _w.ReserveAndCheckInAsync();
            _w.Clock.Advance(TimeSpan.FromHours(26));

            var result = await _w.Bookings().ExitAsync(active.Id, _w.DriverActor);

            Assert.Equal("Overstayed", result.Value!.Status);
            Assert.Equal(1200m, result.Value.TotalFee);
            Assert.Equal(150m, result.Value.PenaltyFee);
        }

        [Fact]
        public async Task A_reserved_booking_cannot_be_exited_before_check_in()
        {
            var reserved = await _w.ReserveAsync();

            var result = await _w.Bookings().ExitAsync(reserved.Id, _w.DriverActor);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
            Assert.Contains("check", result.Error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(SlotStatus.Reserved, _w.Reload(_w.CarSlot).Status);
        }

        [Fact]
        public async Task A_finished_booking_cannot_be_exited_again()
        {
            var active = await _w.ReserveAndCheckInAsync();
            await _w.Bookings().ExitAsync(active.Id, _w.DriverActor);

            var again = await _w.Bookings().ExitAsync(active.Id, _w.DriverActor);

            Assert.Equal(ServiceErrorCode.Conflict, again.Error!.Code);
            Assert.Contains("Completed", again.Error.Message);
        }

        [Fact]
        public async Task The_owning_operator_can_close_out_a_booking_at_the_barrier()
        {
            var active = await _w.ReserveAndCheckInAsync();

            Assert.True((await _w.Bookings().ExitAsync(active.Id, _w.OperatorActor)).Succeeded);
        }

        [Fact]
        public async Task An_operator_cannot_exit_bookings_in_someone_elses_lot()
        {
            var active = await _w.ReserveAndCheckInAsync();

            var result = await _w.Bookings().ExitAsync(active.Id, _w.OtherOperatorActor);

            Assert.Equal(ServiceErrorCode.NotFound, result.Error!.Code);
            Assert.Equal(BookingStatus.Active, _w.ReloadBooking(active.Id).Status);
        }

        [Fact]
        public async Task Another_driver_cannot_exit_my_booking()
        {
            var active = await _w.ReserveAndCheckInAsync();

            var result = await _w.Bookings().ExitAsync(active.Id, _w.OtherDriverActor);

            Assert.Equal(ServiceErrorCode.NotFound, result.Error!.Code);
        }

        // -------------------------------------------------------------------- cancel ----

        [Fact]
        public async Task Cancelling_a_reservation_frees_the_slot_free_of_charge()
        {
            var reserved = await _w.ReserveAsync();

            var result = await _w.Bookings().CancelAsync(reserved.Id, _w.DriverActor);

            Assert.True(result.Succeeded);
            Assert.Equal("Cancelled", result.Value!.Status);
            Assert.NotNull(result.Value.CancelledAt);
            Assert.Null(result.Value.TotalFee);
            Assert.Equal(SlotStatus.Available, _w.Reload(_w.CarSlot).Status);
            Assert.Contains((NotificationType.ReservationCancelled, reserved.Id), _w.Notifications.Calls);
        }

        [Fact]
        public async Task A_cancelled_slot_can_be_reserved_again()
        {
            var reserved = await _w.ReserveAsync();
            await _w.Bookings().CancelAsync(reserved.Id, _w.DriverActor);

            var again = await _w.Bookings().ReserveAsync(
                new ParkEase.Api.DTOs.CreateBookingDto { VehicleId = _w.Car.Id, ParkingSlotId = _w.CarSlot.Id }, _w.Driver.Id);

            Assert.True(again.Succeeded);
        }

        [Fact]
        public async Task A_parked_vehicle_cannot_be_cancelled()
        {
            var active = await _w.ReserveAndCheckInAsync();

            var result = await _w.Bookings().CancelAsync(active.Id, _w.DriverActor);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
            Assert.Contains("Exit", result.Error.Message);
            Assert.Equal(SlotStatus.Occupied, _w.Reload(_w.CarSlot).Status);
        }

        [Fact]
        public async Task Another_driver_cannot_cancel_my_reservation()
        {
            var reserved = await _w.ReserveAsync();

            var result = await _w.Bookings().CancelAsync(reserved.Id, _w.OtherDriverActor);

            Assert.Equal(ServiceErrorCode.NotFound, result.Error!.Code);
        }

        // --------------------------------------------------------------- listings ----

        [Fact]
        public async Task My_bookings_lists_only_mine_newest_first()
        {
            var first = await _w.ReserveAsync(_w.Car, _w.CarSlot);
            _w.Clock.Advance(TimeSpan.FromMinutes(1));
            var second = await _w.ReserveAsync(_w.Bike, _w.BikeSlot);
            var otherVehicle = _w.AddVehicleFor(_w.OtherDriver, "TN10CD5678");
            await _w.Bookings().ReserveAsync(
                new ParkEase.Api.DTOs.CreateBookingDto { VehicleId = otherVehicle.Id, ParkingSlotId = _w.CarSlot2.Id }, _w.OtherDriver.Id);

            var mine = await _w.Bookings().GetMyBookingsAsync(_w.Driver.Id);

            Assert.Equal(new[] { second.Id, first.Id }, mine.Select(b => b.Id));
        }

        [Fact]
        public async Task A_lot_shows_who_is_mapped_to_which_slot_to_its_operator_and_admins_only()
        {
            await _w.ReserveAsync();

            var asOperator = await _w.Bookings().GetBookingsForLotAsync(_w.Lot.Id, _w.OperatorActor);
            var asAdmin = await _w.Bookings().GetBookingsForLotAsync(_w.Lot.Id, _w.AdminActor);
            var asStranger = await _w.Bookings().GetBookingsForLotAsync(_w.Lot.Id, _w.OtherOperatorActor);

            var row = Assert.Single(asOperator);
            Assert.Equal("TN09AB1234", row.VehicleNumber);
            Assert.Equal("A1", row.SlotNumber);
            Assert.Equal("Dana Driver", row.DriverName);
            Assert.Single(asAdmin);
            Assert.Empty(asStranger);
        }
    }
}
