namespace ParkEase.Api.DTOs
{
    // One booking = one vehicle mapped to one slot, with its full lifecycle timestamps.
    public class BookingDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty; // Reserved | Active | Completed | Overstayed | Cancelled | Expired

        public int VehicleId { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;

        public int ParkingSlotId { get; set; }
        public string SlotNumber { get; set; } = string.Empty;
        public int ParkingLotId { get; set; }
        public string LotName { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }

        public DateTime ReservedAt { get; set; }
        public DateTime? ReservationExpiresAt { get; set; }
        public DateTime? EntryTime { get; set; }   // check-in; billing starts here
        public DateTime? ExitTime { get; set; }
        public DateTime? CancelledAt { get; set; } // cancelled or expired reservations

        public decimal? TotalFee { get; set; }
        public decimal? PenaltyFee { get; set; }
    }
}
