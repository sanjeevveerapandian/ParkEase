using ParkEase.Api.DTOs;
using ParkEase.Api.Models;
using ParkEase.Api.Services;
using ParkEase.Api.Tests.Support;

namespace ParkEase.Api.Tests
{
    // Step 1 of the flow: reserving a slot for a vehicle, and the vehicle-slot mapping rules.
    public class BookingReservationTests
    {
        private readonly TestWorld _w = new();

        private Task<ServiceResult<BookingDto>> TryReserve(Vehicle vehicle, ParkingSlot slot, User? driver = null) =>
            _w.Bookings().ReserveAsync(new CreateBookingDto { VehicleId = vehicle.Id, ParkingSlotId = slot.Id }, (driver ?? _w.Driver).Id);

        [Fact]
        public async Task Reserving_maps_the_vehicle_to_the_slot_and_holds_it()
        {
            var booking = await _w.ReserveAsync();

            Assert.Equal("Reserved", booking.Status);
            Assert.Equal(_w.Car.Id, booking.VehicleId);
            Assert.Equal("TN09AB1234", booking.VehicleNumber);
            Assert.Equal("Car", booking.VehicleType);
            Assert.Equal(_w.CarSlot.Id, booking.ParkingSlotId);
            Assert.Equal("A1", booking.SlotNumber);
            Assert.Equal("Test Lot", booking.LotName);
            Assert.Equal("Dana Driver", booking.DriverName);
            Assert.Equal(50m, booking.HourlyRate);

            Assert.Equal(SlotStatus.Reserved, _w.Reload(_w.CarSlot).Status);
            Assert.Equal(_w.Clock.GetUtcNow().UtcDateTime, booking.ReservedAt);
            Assert.Equal(booking.ReservedAt.AddMinutes(15), booking.ReservationExpiresAt);
        }

        [Fact]
        public async Task Billing_has_not_started_at_reservation()
        {
            var booking = await _w.ReserveAsync();

            Assert.Null(booking.EntryTime);
            Assert.Null(booking.ExitTime);
            Assert.Null(booking.TotalFee);
        }

        [Fact]
        public async Task The_hold_length_comes_from_configuration()
        {
            var result = await _w.Bookings(holdMinutes: 30).ReserveAsync(
                new CreateBookingDto { VehicleId = _w.Car.Id, ParkingSlotId = _w.CarSlot.Id }, _w.Driver.Id);

            Assert.Equal(result.Value!.ReservedAt.AddMinutes(30), result.Value.ReservationExpiresAt);
        }

        [Fact]
        public async Task A_confirmation_is_sent_for_the_new_booking()
        {
            var booking = await _w.ReserveAsync();

            Assert.Equal((NotificationType.BookingConfirmation, booking.Id), Assert.Single(_w.Notifications.Calls));
        }

        [Fact]
        public async Task A_bike_cannot_be_mapped_to_a_car_slot()
        {
            var result = await TryReserve(_w.Bike, _w.CarSlot);

            Assert.False(result.Succeeded);
            Assert.Equal(ServiceErrorCode.Validation, result.Error!.Code);
            Assert.Contains("Car slot", result.Error.Message);
            Assert.Contains("Bike", result.Error.Message);
            Assert.Equal(SlotStatus.Available, _w.Reload(_w.CarSlot).Status);
        }

        [Fact]
        public async Task A_car_cannot_be_mapped_to_a_bike_slot()
        {
            var result = await TryReserve(_w.Car, _w.BikeSlot);

            Assert.Equal(ServiceErrorCode.Validation, result.Error!.Code);
            Assert.Contains("Bike slot", result.Error.Message);
        }

        [Fact]
        public async Task A_bike_fits_a_bike_slot()
        {
            var result = await TryReserve(_w.Bike, _w.BikeSlot);

            Assert.True(result.Succeeded);
            Assert.Equal("Bike", result.Value!.VehicleType);
        }

        [Fact]
        public async Task A_slot_can_only_be_reserved_once()
        {
            await _w.ReserveAsync();
            var second = _w.AddVehicleFor(_w.OtherDriver, "TN10CD5678");

            var result = await TryReserve(second, _w.CarSlot, _w.OtherDriver);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
            Assert.Contains("already reserved", result.Error.Message);
        }

        [Fact]
        public async Task An_occupied_slot_cannot_be_reserved()
        {
            await _w.ReserveAndCheckInAsync();
            var other = _w.AddVehicleFor(_w.OtherDriver, "TN10CD5678");

            var result = await TryReserve(other, _w.CarSlot, _w.OtherDriver);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
            Assert.Contains("occupied", result.Error.Message);
        }

        [Fact]
        public async Task A_slot_under_maintenance_cannot_be_reserved()
        {
            var slot = _w.Db.ParkingSlots.First(s => s.Id == _w.CarSlot2.Id);
            slot.Status = SlotStatus.Maintenance;
            _w.Db.SaveChanges();

            var result = await TryReserve(_w.Car, _w.CarSlot2);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
            Assert.Contains("maintenance", result.Error.Message);
        }

        [Fact]
        public async Task A_vehicle_cannot_hold_two_slots_at_once()
        {
            await _w.ReserveAsync(_w.Car, _w.CarSlot);

            var result = await TryReserve(_w.Car, _w.CarSlot2);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
            Assert.Contains("TN09AB1234", result.Error.Message);
            Assert.Contains("A1", result.Error.Message);
            Assert.Equal(SlotStatus.Available, _w.Reload(_w.CarSlot2).Status);
        }

        [Fact]
        public async Task The_same_plate_registered_under_two_accounts_still_cannot_hold_two_slots()
        {
            await _w.ReserveAsync(_w.Car, _w.CarSlot);
            var duplicatePlate = _w.AddVehicleFor(_w.OtherDriver, "TN09AB1234");

            var result = await TryReserve(duplicatePlate, _w.CarSlot2, _w.OtherDriver);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
        }

        [Fact]
        public async Task A_vehicle_can_be_booked_again_once_its_booking_is_over()
        {
            var first = await _w.ReserveAndCheckInAsync();
            Assert.True((await _w.Bookings().ExitAsync(first.Id, _w.DriverActor)).Succeeded);

            var again = await TryReserve(_w.Car, _w.CarSlot);

            Assert.True(again.Succeeded);
        }

        [Fact]
        public async Task A_driver_cannot_book_with_someone_elses_vehicle()
        {
            var result = await TryReserve(_w.Car, _w.CarSlot, _w.OtherDriver);

            Assert.Equal(ServiceErrorCode.NotFound, result.Error!.Code);
            Assert.Equal(SlotStatus.Available, _w.Reload(_w.CarSlot).Status);
        }

        [Fact]
        public async Task A_removed_vehicle_cannot_be_booked()
        {
            var vehicle = _w.Db.Vehicles.First(v => v.Id == _w.Car.Id);
            vehicle.IsActive = false;
            _w.Db.SaveChanges();

            var result = await TryReserve(_w.Car, _w.CarSlot);

            Assert.Equal(ServiceErrorCode.NotFound, result.Error!.Code);
        }

        [Fact]
        public async Task An_unknown_slot_is_not_found()
        {
            var result = await _w.Bookings().ReserveAsync(
                new CreateBookingDto { VehicleId = _w.Car.Id, ParkingSlotId = 9999 }, _w.Driver.Id);

            Assert.Equal(ServiceErrorCode.NotFound, result.Error!.Code);
        }

        [Fact]
        public async Task A_failed_reservation_sends_no_notification()
        {
            await TryReserve(_w.Bike, _w.CarSlot);

            Assert.Empty(_w.Notifications.Calls);
        }
    }
}
