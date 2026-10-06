using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ParkEase.Api.Data;
using ParkEase.Api.DTOs;
using ParkEase.Api.Models;
using ParkEase.Api.Services;

namespace ParkEase.Api.Tests
{
    public class AuthServiceTests
    {
        private static ApplicationDbContext NewDb() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static AuthService NewService(ApplicationDbContext db)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "unit-test-signing-key-that-is-long-enough-for-hmac-sha256",
                ["Jwt:Issuer"] = "ParkEase.Tests",
                ["Jwt:Audience"] = "ParkEase.Tests.Clients",
                ["Jwt:ExpiryMinutes"] = "60"
            }).Build();
            return new AuthService(db, config);
        }

        private static RegisterDto Registration(string email = "dana@test.com", string role = "Driver", string phone = "98765 43210") => new()
        {
            FullName = "  Dana Driver ",
            Email = email,
            Password = "Passw0rd!",
            PhoneNumber = phone,
            Role = role
        };

        [Fact]
        public async Task Registering_stores_a_hashed_password_and_an_E164_phone_number()
        {
            using var db = NewDb();

            var result = await NewService(db).RegisterAsync(Registration());

            Assert.True(result.Succeeded);
            var user = db.Users.Single();
            Assert.Equal("Dana Driver", user.FullName);
            Assert.Equal("+919876543210", user.PhoneNumber);
            Assert.NotEqual("Passw0rd!", user.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("Passw0rd!", user.PasswordHash));
            Assert.Equal(UserRole.Driver, user.Role);
        }

        [Fact]
        public async Task The_issued_token_carries_the_user_id_role_and_expiry()
        {
            using var db = NewDb();

            var result = await NewService(db).RegisterAsync(Registration(role: "operator"));

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Value!.Token);
            Assert.Equal("Operator", result.Value.Role);
            Assert.Equal(db.Users.Single().Id.ToString(), jwt.Subject);
            Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Operator");
            Assert.Equal("ParkEase.Tests", jwt.Issuer);
            Assert.InRange(result.Value.ExpiresAt, DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
        }

        [Fact]
        public async Task A_duplicate_email_is_a_conflict()
        {
            using var db = NewDb();
            var service = NewService(db);
            await service.RegisterAsync(Registration());

            var again = await service.RegisterAsync(Registration());

            Assert.Equal(ServiceErrorCode.Conflict, again.Error!.Code);
            Assert.Equal(1, db.Users.Count());
        }

        [Fact]
        public async Task An_invalid_phone_number_is_rejected_at_registration()
        {
            using var db = NewDb();

            var result = await NewService(db).RegisterAsync(Registration(phone: "12345"));

            Assert.Equal(ServiceErrorCode.Validation, result.Error!.Code);
            Assert.Empty(db.Users);
        }

        [Fact]
        public async Task An_unknown_role_is_rejected()
        {
            using var db = NewDb();

            var result = await NewService(db).RegisterAsync(Registration(role: "Superuser"));

            Assert.Equal(ServiceErrorCode.Validation, result.Error!.Code);
        }

        [Fact]
        public async Task Login_succeeds_with_the_right_password_only()
        {
            using var db = NewDb();
            var service = NewService(db);
            await service.RegisterAsync(Registration());

            Assert.NotNull(await service.LoginAsync(new LoginDto { Email = "dana@test.com", Password = "Passw0rd!" }));
            Assert.Null(await service.LoginAsync(new LoginDto { Email = "dana@test.com", Password = "wrong" }));
            Assert.Null(await service.LoginAsync(new LoginDto { Email = "nobody@test.com", Password = "Passw0rd!" }));
        }
    }

    public class DbSeederTests
    {
        private static ApplicationDbContext NewDb() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        [Fact]
        public void An_empty_database_gets_an_admin_an_operator_a_driver_lots_slots_and_vehicles()
        {
            using var db = NewDb();

            DbSeeder.Seed(db);

            Assert.Equal(1, db.Users.Count(u => u.Role == UserRole.Admin));
            Assert.Equal(1, db.Users.Count(u => u.Role == UserRole.Operator));
            Assert.Equal(1, db.Users.Count(u => u.Role == UserRole.Driver));
            Assert.Equal(2, db.ParkingLots.Count());
            Assert.Equal(5, db.ParkingSlots.Count());
            Assert.Equal(2, db.Vehicles.Count());
        }

        [Fact]
        public void The_seeded_slots_cover_both_vehicle_types_and_the_seeded_vehicles_fit_them()
        {
            using var db = NewDb();
            DbSeeder.Seed(db);

            var slotTypes = db.ParkingSlots.Select(s => s.VehicleType).Distinct().ToList();
            var vehicleTypes = db.Vehicles.Select(v => v.Type).Distinct().ToList();

            Assert.Contains(VehicleType.Car, slotTypes);
            Assert.Contains(VehicleType.Bike, slotTypes);
            Assert.All(vehicleTypes, t => Assert.Contains(t, slotTypes));
        }

        [Fact]
        public void Seeded_phone_numbers_are_valid_for_SMS()
        {
            using var db = NewDb();
            DbSeeder.Seed(db);

            Assert.All(db.Users.ToList(), u =>
                Assert.True(ParkEase.Api.Services.Notifications.PhoneNumber.TryNormalize(u.PhoneNumber, out _), u.Email));
        }

        [Fact]
        public void Seeding_twice_changes_nothing()
        {
            using var db = NewDb();
            DbSeeder.Seed(db);
            var (users, lots, slots, vehicles) = (db.Users.Count(), db.ParkingLots.Count(), db.ParkingSlots.Count(), db.Vehicles.Count());

            DbSeeder.Seed(db);

            Assert.Equal((users, lots, slots, vehicles), (db.Users.Count(), db.ParkingLots.Count(), db.ParkingSlots.Count(), db.Vehicles.Count()));
        }

        [Fact]
        public void A_database_that_already_has_lots_gets_only_the_missing_admin()
        {
            using var db = NewDb();
            var op = new User { FullName = "Real Op", Email = "op@real.com", PasswordHash = "x", PhoneNumber = "+919000000001", Role = UserRole.Operator };
            db.Users.Add(op);
            db.SaveChanges();
            db.ParkingLots.Add(new ParkingLot { Name = "Existing", Address = "x", OperatorId = op.Id });
            db.SaveChanges();

            DbSeeder.Seed(db);

            Assert.Equal(1, db.ParkingLots.Count());           // no demo lots layered on real data
            Assert.Equal(2, db.Users.Count());                 // existing operator + admin
            Assert.Equal(1, db.Users.Count(u => u.Role == UserRole.Admin));
        }
    }
}
