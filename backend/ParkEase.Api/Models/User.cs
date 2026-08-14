namespace ParkEase.Api.Models
{
    public enum UserRole
    {
        Driver,
        Operator,
        Admin
    }

    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ParkingLot> ManagedLots { get; set; } = new List<ParkingLot>();
        public ICollection<VehicleBooking> Bookings { get; set; } = new List<VehicleBooking>();
    }
}