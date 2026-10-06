namespace ParkEase.Api.Models
{
    // Booking lifecycle (see docs/FLOW.md for the diagram):
    //
    //   Reserved --check-in--> Active --exit--> Completed | Overstayed
    //      |
    //      +--cancel--> Cancelled
    //      +--hold runs out--> Expired
    //
    // The numeric values of the original four states are unchanged so existing rows
    // keep their meaning; Reserved and Expired were added.
    public enum BookingStatus
    {
        Active = 0,
        Completed = 1,
        Overstayed = 2,
        Cancelled = 3,
        Reserved = 4,
        Expired = 5
    }

    public class VehicleBooking
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        // The vehicle-to-slot mapping.
        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }
        public int ParkingSlotId { get; set; }
        public ParkingSlot? ParkingSlot { get; set; }

        // Snapshot of the plate at booking time, so history reads correctly even if the
        // vehicle is later archived.
        public string VehicleNumber { get; set; } = string.Empty;

        public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
        // Set while Reserved; the slot is released if the driver hasn't checked in by then.
        public DateTime? ReservationExpiresAt { get; set; }

        // Check-in time. Billing starts here, not at reservation. Null until checked in.
        public DateTime? EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        // When a Reserved booking was cancelled or expired.
        public DateTime? CancelledAt { get; set; }

        public decimal? TotalFee { get; set; }
        public decimal? PenaltyFee { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Reserved;
    }
}
