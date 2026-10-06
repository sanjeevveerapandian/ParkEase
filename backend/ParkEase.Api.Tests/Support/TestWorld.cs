using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ParkEase.Api.Data;
using ParkEase.Api.DTOs;
using ParkEase.Api.Models;
using ParkEase.Api.Services;
using ParkEase.Api.Services.Notifications;

namespace ParkEase.Api.Tests.Support
{
    public sealed class FakeClock : TimeProvider
    {
        private DateTimeOffset _now;

        public FakeClock(DateTimeOffset start) => _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    // Records every notification the booking service asks for, and writes a row for it like the
    // real dispatcher would, so "already warned" style checks behave realistically.
    public sealed class RecordingNotifications : INotificationService
    {
        private readonly ApplicationDbContext _db;

        public List<(NotificationType Type, int BookingId)> Calls { get; } = new();

        public RecordingNotifications(ApplicationDbContext db) => _db = db;

        public async Task NotifyAsync(NotificationType type, int bookingId, CancellationToken ct = default)
        {
            Calls.Add((type, bookingId));
            _db.Notifications.Add(new Notification
            {
                VehicleBookingId = bookingId,
                Type = type,
                Channel = NotificationChannel.Sms,
                Recipient = "test",
                Message = "test",
                Status = NotificationStatus.Simulated,
                Provider = "Test",
                CreatedAt = DateTime.MinValue   // keep these out of the way of fake-clock ordering
            });
            await _db.SaveChangesAsync(ct);
        }
    }

    // A small, fully-populated parking world: one operator with one lot holding two car slots and
    // one bike slot, a driver with a car and a bike, plus a second driver/operator/admin to
    // exercise the "not yours" paths.
    public sealed class TestWorld
    {
        public ApplicationDbContext Db { get; }
        public FakeClock Clock { get; }
        public RecordingNotifications Notifications { get; }

        public User Driver { get; }
        public User OtherDriver { get; }
        public User Operator { get; }
        public User OtherOperator { get; }
        public User Admin { get; }

        public ParkingLot Lot { get; }
        public ParkingSlot CarSlot { get; }   // A1, Rs.50/hr
        public ParkingSlot CarSlot2 { get; }  // A2, Rs.60/hr
        public ParkingSlot BikeSlot { get; }  // B1, Rs.20/hr

        public Vehicle Car { get; }
        public Vehicle Bike { get; }

        public TestWorld()
        {
            Db = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);
            Clock = new FakeClock(new DateTimeOffset(2026, 10, 5, 8, 0, 0, TimeSpan.Zero));
            Notifications = new RecordingNotifications(Db);

            Driver = AddUser("Dana Driver", "dana@test.com", "+919876500001", UserRole.Driver);
            OtherDriver = AddUser("Omar Driver", "omar@test.com", "+919876500002", UserRole.Driver);
            Operator = AddUser("Olive Operator", "olive@test.com", "+919876500003", UserRole.Operator);
            OtherOperator = AddUser("Otto Operator", "otto@test.com", "+919876500004", UserRole.Operator);
            Admin = AddUser("Ada Admin", "ada@test.com", "+919876500005", UserRole.Admin);
            Db.SaveChanges();

            Lot = new ParkingLot { Name = "Test Lot", Address = "1 Test Street", OperatorId = Operator.Id };
            Db.ParkingLots.Add(Lot);
            Db.SaveChanges();

            CarSlot = AddSlot("A1", 50m, VehicleType.Car);
            CarSlot2 = AddSlot("A2", 60m, VehicleType.Car);
            BikeSlot = AddSlot("B1", 20m, VehicleType.Bike);

            Car = AddVehicle(Driver, "TN09AB1234", VehicleType.Car);
            Bike = AddVehicle(Driver, "TN09XY4321", VehicleType.Bike);
            Db.SaveChanges();
        }

        public Actor DriverActor => new(Driver.Id, UserRole.Driver);
        public Actor OtherDriverActor => new(OtherDriver.Id, UserRole.Driver);
        public Actor OperatorActor => new(Operator.Id, UserRole.Operator);
        public Actor OtherOperatorActor => new(OtherOperator.Id, UserRole.Operator);
        public Actor AdminActor => new(Admin.Id, UserRole.Admin);

        public BookingService Bookings(int holdMinutes = 15) =>
            new(Db, Notifications, Options.Create(new BookingOptions { ReservationHoldMinutes = holdMinutes }), Clock);

        public ParkingLotService Lots() => new(Db);

        public VehicleService Vehicles() => new(Db);

        public async Task<BookingDto> ReserveAsync(Vehicle? vehicle = null, ParkingSlot? slot = null)
        {
            var result = await Bookings().ReserveAsync(
                new CreateBookingDto { VehicleId = (vehicle ?? Car).Id, ParkingSlotId = (slot ?? CarSlot).Id },
                Driver.Id);
            Assert.True(result.Succeeded, result.Error?.Message);
            return result.Value!;
        }

        public async Task<BookingDto> ReserveAndCheckInAsync(Vehicle? vehicle = null, ParkingSlot? slot = null)
        {
            var booking = await ReserveAsync(vehicle, slot);
            var result = await Bookings().CheckInAsync(booking.Id, DriverActor);
            Assert.True(result.Succeeded, result.Error?.Message);
            return result.Value!;
        }

        public ParkingSlot Reload(ParkingSlot slot) => Db.ParkingSlots.AsNoTracking().First(s => s.Id == slot.Id);

        public VehicleBooking ReloadBooking(int id) => Db.VehicleBookings.AsNoTracking().First(b => b.Id == id);

        private User AddUser(string name, string email, string phone, UserRole role)
        {
            var user = new User { FullName = name, Email = email, PhoneNumber = phone, PasswordHash = "x", Role = role };
            Db.Users.Add(user);
            return user;
        }

        private ParkingSlot AddSlot(string number, decimal rate, VehicleType type)
        {
            var slot = new ParkingSlot { ParkingLotId = Lot.Id, SlotNumber = number, HourlyRate = rate, VehicleType = type };
            Db.ParkingSlots.Add(slot);
            Db.SaveChanges();
            return slot;
        }

        private Vehicle AddVehicle(User owner, string plate, VehicleType type)
        {
            var vehicle = new Vehicle { OwnerId = owner.Id, VehicleNumber = plate, Type = type };
            Db.Vehicles.Add(vehicle);
            return vehicle;
        }

        public Vehicle AddVehicleFor(User owner, string plate, VehicleType type = VehicleType.Car)
        {
            var vehicle = AddVehicle(owner, plate, type);
            Db.SaveChanges();
            return vehicle;
        }
    }
}
