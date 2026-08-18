namespace ParkEase.Api.DTOs
{
    public class CreateParkingSlotDto
    {
        public string SlotNumber { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
    }
}