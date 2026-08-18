namespace ParkEase.Api.DTOs
{
    public class BookingDto
    {
        public int Id { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public int ParkingSlotId { get; set; }
        public string SlotNumber { get; set; } = string.Empty;
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public decimal? TotalFee { get; set; }
        public decimal? PenaltyFee { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}