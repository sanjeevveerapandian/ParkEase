using ParkEase.Api.DTOs;
using ParkEase.Api.Services;
using ParkEase.Api.Tests.Support;

namespace ParkEase.Api.Tests
{
    public class VehicleServiceTests
    {
        private readonly TestWorld _w = new();

        [Fact]
        public async Task Adding_a_vehicle_normalizes_the_plate()
        {
            var result = await _w.Vehicles().AddAsync(new CreateVehicleDto { VehicleNumber = "tn 10-cd 5678", Type = "bike" }, _w.Driver.Id);

            Assert.True(result.Succeeded);
            Assert.Equal("TN10CD5678", result.Value!.VehicleNumber);
            Assert.Equal("Bike", result.Value.Type);
        }

        [Fact]
        public async Task An_invalid_plate_is_rejected()
        {
            var result = await _w.Vehicles().AddAsync(new CreateVehicleDto { VehicleNumber = "hello", Type = "Car" }, _w.Driver.Id);

            Assert.Equal(ServiceErrorCode.Validation, result.Error!.Code);
        }

        [Fact]
        public async Task An_unknown_type_is_rejected()
        {
            var result = await _w.Vehicles().AddAsync(new CreateVehicleDto { VehicleNumber = "TN10CD5678", Type = "Truck" }, _w.Driver.Id);

            Assert.Equal(ServiceErrorCode.Validation, result.Error!.Code);
        }

        [Fact]
        public async Task The_same_plate_cannot_be_added_twice_by_one_driver()
        {
            var result = await _w.Vehicles().AddAsync(new CreateVehicleDto { VehicleNumber = "tn09ab1234", Type = "Car" }, _w.Driver.Id);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
        }

        [Fact]
        public async Task Two_drivers_may_each_hold_the_same_plate_record()
        {
            var result = await _w.Vehicles().AddAsync(new CreateVehicleDto { VehicleNumber = "TN09AB1234", Type = "Car" }, _w.OtherDriver.Id);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task My_vehicles_lists_only_my_active_vehicles_sorted_by_plate()
        {
            _w.AddVehicleFor(_w.OtherDriver, "KA01AA0001");

            var mine = await _w.Vehicles().GetMineAsync(_w.Driver.Id);

            Assert.Equal(new[] { "TN09AB1234", "TN09XY4321" }, mine.Select(v => v.VehicleNumber));
        }

        [Fact]
        public async Task A_vehicle_without_history_is_deleted_outright()
        {
            var result = await _w.Vehicles().RemoveAsync(_w.Bike.Id, _w.Driver.Id);

            Assert.True(result.Succeeded);
            Assert.False(_w.Db.Vehicles.Any(v => v.Id == _w.Bike.Id));
        }

        [Fact]
        public async Task A_vehicle_with_booking_history_is_archived_not_deleted()
        {
            var booking = await _w.ReserveAndCheckInAsync();
            await _w.Bookings().ExitAsync(booking.Id, _w.DriverActor);

            var result = await _w.Vehicles().RemoveAsync(_w.Car.Id, _w.Driver.Id);

            Assert.True(result.Succeeded);
            Assert.False(_w.Db.Vehicles.First(v => v.Id == _w.Car.Id).IsActive);
            Assert.DoesNotContain(await _w.Vehicles().GetMineAsync(_w.Driver.Id), v => v.Id == _w.Car.Id);
        }

        [Fact]
        public async Task An_archived_vehicle_is_restored_when_added_again()
        {
            var booking = await _w.ReserveAndCheckInAsync();
            await _w.Bookings().ExitAsync(booking.Id, _w.DriverActor);
            await _w.Vehicles().RemoveAsync(_w.Car.Id, _w.Driver.Id);

            var result = await _w.Vehicles().AddAsync(new CreateVehicleDto { VehicleNumber = "TN09AB1234", Type = "Car" }, _w.Driver.Id);

            Assert.True(result.Succeeded);
            Assert.Equal(_w.Car.Id, result.Value!.Id);   // same row, so the history stays attached
            Assert.True(_w.Db.Vehicles.First(v => v.Id == _w.Car.Id).IsActive);
        }

        [Fact]
        public async Task A_vehicle_with_a_live_booking_cannot_be_removed()
        {
            await _w.ReserveAsync();

            var result = await _w.Vehicles().RemoveAsync(_w.Car.Id, _w.Driver.Id);

            Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
            Assert.True(_w.Db.Vehicles.First(v => v.Id == _w.Car.Id).IsActive);
        }

        [Fact]
        public async Task Another_drivers_vehicle_cannot_be_removed()
        {
            var result = await _w.Vehicles().RemoveAsync(_w.Car.Id, _w.OtherDriver.Id);

            Assert.Equal(ServiceErrorCode.NotFound, result.Error!.Code);
        }
    }
}
