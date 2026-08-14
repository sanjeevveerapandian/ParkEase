namespace ParkEase.Api.Models
{
    public enum BookingStatus
    {
        Active,
        Completed,
        Overstayed,
        Cancelled
    }

    public class VehicleBooking
    {
        public int Id { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
        public User? User { get; set; }
        public int ParkingSlotId { get; set; }
        public ParkingSlot? ParkingSlot { get; set; }

        public DateTime EntryTime { get; set; } = DateTime.UtcNow;
        public DateTime? ExitTime { get; set; }
        public decimal? TotalFee { get; set; }
        public decimal? PenaltyFee { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Active;
    }
}