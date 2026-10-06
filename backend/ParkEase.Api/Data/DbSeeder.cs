using ParkEase.Api.Models;

namespace ParkEase.Api.Data
{
    // Gives a fresh clone something to log in and click through: an Admin, and (only on an empty
    // database) a demo Operator with two lots, plus a demo Driver with a car and a bike.
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext db)
        {
            if (!db.Users.Any(u => u.Role == UserRole.Admin))
            {
                db.Users.Add(NewUser("System Admin", "admin@parkease.com", "Admin@123", "9999999999", UserRole.Admin));
                db.SaveChanges();
                Console.WriteLine("Seeded default Admin: admin@parkease.com / Admin@123");
            }

            // Guarded on "no lots yet" rather than on the specific accounts, same as the Admin seed.
            if (db.ParkingLots.Any()) return;

            var op = NewUser("Demo Operator", "operator@parkease.com", "Operator@123", "9888888888", UserRole.Operator);
            var driver = NewUser("Demo Driver", "driver@parkease.com", "Driver@123", "9777777777", UserRole.Driver);
            db.Users.AddRange(op, driver);
            db.SaveChanges(); // need the ids before they can own lots and vehicles

            var mylapore = new ParkingLot { Name = "Mylapore Temple Parking", Address = "North Mada Street, Mylapore, Chennai 600004", OperatorId = op.Id };
            var porur = new ParkingLot { Name = "Porur Junction Parking", Address = "Mount Poonamallee Road, Porur, Chennai 600116", OperatorId = op.Id };
            db.ParkingLots.AddRange(mylapore, porur);
            db.SaveChanges();

            db.ParkingSlots.AddRange(
                NewSlot(mylapore.Id, "A1", 40m, VehicleType.Car),
                NewSlot(mylapore.Id, "A2", 60m, VehicleType.Car),
                NewSlot(mylapore.Id, "B1", 20m, VehicleType.Bike),
                NewSlot(porur.Id, "A1", 30m, VehicleType.Car),
                NewSlot(porur.Id, "A2", 80m, VehicleType.Car));

            db.Vehicles.AddRange(
                new Vehicle { OwnerId = driver.Id, VehicleNumber = "TN09AB1234", Type = VehicleType.Car },
                new Vehicle { OwnerId = driver.Id, VehicleNumber = "TN09XY4321", Type = VehicleType.Bike });
            db.SaveChanges();

            Console.WriteLine("Seeded demo Operator (operator@parkease.com / Operator@123) with 2 lots and 5 slots, " +
                              "and demo Driver (driver@parkease.com / Driver@123) with a car and a bike.");
        }

        private static User NewUser(string name, string email, string password, string phone, UserRole role) => new()
        {
            FullName = name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            PhoneNumber = phone,
            Role = role
        };

        private static ParkingSlot NewSlot(int lotId, string number, decimal rate, VehicleType type) => new()
        {
            ParkingLotId = lotId,
            SlotNumber = number,
            HourlyRate = rate,
            VehicleType = type,
            Status = SlotStatus.Available
        };
    }
}
