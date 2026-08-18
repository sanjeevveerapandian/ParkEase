namespace ParkEase.Api.DTOs
{
    public class ParkingSlotDto
    {
        public int Id { get; set; }
        public string SlotNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // enum as readable string, not raw int
        public decimal HourlyRate { get; set; }
        public int ParkingLotId { get; set; }
    }
}