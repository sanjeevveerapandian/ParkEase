using ParkEase.Api.Models;
using ParkEase.Api.Tests.Support;

namespace ParkEase.Api.Tests
{
    // The time-driven parts the background worker runs.
    public class BookingMaintenanceTests
    {
        private readonly TestWorld _w = new();

        [Fact]
        public async Task A_reservation_nobody_checks_in_for_expires_and_frees_its_slot()
        {
            var reserved = await _w.ReserveAsync();
            _w.Clock.Advance(TimeSpan.FromMinutes(16));

            var expired = await _w.Bookings().ExpireStaleReservationsAsync();

            Assert.Equal(1, expired);
            var booking = _w.ReloadBooking(reserved.Id);
            Assert.Equal(BookingStatus.Expired, booking.Status);
            Assert.NotNull(booking.CancelledAt);
            Assert.Null(booking.ReservationExpiresAt);
            Assert.Equal(SlotStatus.Available, _w.Reload(_w.CarSlot).Status);
            Assert.Contains((NotificationType.ReservationExpired, reserved.Id), _w.Notifications.Calls);
        }

        [Fact]
        public async Task A_reservation_still_inside_its_hold_is_left_alone()
        {
            var reserved = await _w.ReserveAsync();
            _w.Clock.Advance(TimeSpan.FromMinutes(14));

            var expired = await _w.Bookings().ExpireStaleReservationsAsync();

            Assert.Equal(0, expired);
            Assert.Equal(BookingStatus.Reserved, _w.ReloadBooking(reserved.Id).Status);
            Assert.Equal(SlotStatus.Reserved, _w.Reload(_w.CarSlot).Status);
        }

        [Fact]
        public async Task A_checked_in_booking_never_expires()
        {
            var active = await _w.ReserveAndCheckInAsync();
            _w.Clock.Advance(TimeSpan.FromHours(5));

            Assert.Equal(0, await _w.Bookings().ExpireStaleReservationsAsync());
            Assert.Equal(BookingStatus.Active, _w.ReloadBooking(active.Id).Status);
        }

        [Fact]
        public async Task Expiring_twice_is_harmless()
        {
            await _w.ReserveAsync();
            _w.Clock.Advance(TimeSpan.FromMinutes(20));

            Assert.Equal(1, await _w.Bookings().ExpireStaleReservationsAsync());
            Assert.Equal(0, await _w.Bookings().ExpireStaleReservationsAsync());
        }

        [Fact]
        public async Task Only_the_stale_reservations_expire()
        {
            await _w.ReserveAsync(_w.Car, _w.CarSlot);
            _w.Clock.Advance(TimeSpan.FromMinutes(10));
            var fresh = await _w.ReserveAsync(_w.Bike, _w.BikeSlot);
            _w.Clock.Advance(TimeSpan.FromMinutes(6)); // first is 16 min old, second only 6

            var expired = await _w.Bookings().ExpireStaleReservationsAsync();

            Assert.Equal(1, expired);
            Assert.Equal(BookingStatus.Reserved, _w.ReloadBooking(fresh.Id).Status);
        }

        [Fact]
        public async Task A_driver_close_to_the_24_hour_limit_gets_one_overstay_warning()
        {
            var active = await _w.ReserveAndCheckInAsync();

            _w.Clock.Advance(TimeSpan.FromHours(22));
            Assert.Equal(0, await _w.Bookings().SendOverstayWarningsAsync());   // too early

            _w.Clock.Advance(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(1)));   // 23h01m parked
            Assert.Equal(1, await _w.Bookings().SendOverstayWarningsAsync());
            Assert.Contains((NotificationType.OverstayWarning, active.Id), _w.Notifications.Calls);

            _w.Clock.Advance(TimeSpan.FromMinutes(30));
            Assert.Equal(0, await _w.Bookings().SendOverstayWarningsAsync());   // never twice
        }

        [Fact]
        public async Task Reserved_and_finished_bookings_get_no_overstay_warning()
        {
            var reserved = await _w.ReserveAsync(_w.Bike, _w.BikeSlot);
            var done = await _w.ReserveAndCheckInAsync();
            await _w.Bookings().ExitAsync(done.Id, _w.DriverActor);
            _w.Clock.Advance(TimeSpan.FromHours(30));

            Assert.Equal(0, await _w.Bookings().SendOverstayWarningsAsync());
            Assert.DoesNotContain(_w.Notifications.Calls, c => c.Type == NotificationType.OverstayWarning && c.BookingId == reserved.Id);
        }
    }
}
