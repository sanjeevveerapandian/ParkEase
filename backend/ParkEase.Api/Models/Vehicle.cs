namespace ParkEase.Api.Models
{
    public enum VehicleType
    {
        Car = 0,
        Bike = 1
    }

    // A driver's registered vehicle. This is the "vehicle" half of vehicle-slot mapping:
    // a booking maps exactly one Vehicle to exactly one ParkingSlot, and the two must
    // have the same VehicleType (a bike can't take a car bay and vice versa).
    public class Vehicle
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public User? Owner { get; set; }

        // Normalized: upper-case, no spaces or hyphens (TN 09-AB 1234 -> TN09AB1234).
        public string VehicleNumber { get; set; } = string.Empty;
        public VehicleType Type { get; set; } = VehicleType.Car;

        // Vehicles with booking history can't be hard-deleted (billing records point at
        // them), so "removing" one archives it instead.
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<VehicleBooking> Bookings { get; set; } = new List<VehicleBooking>();
    }
}
