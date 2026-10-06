namespace ParkEase.Api.DTOs
{
    public class ParkingSlotDto
    {
        public int Id { get; set; }
        public string SlotNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Available | Occupied | Reserved | Maintenance
        public string VehicleType { get; set; } = string.Empty; // Car | Bike
        public decimal HourlyRate { get; set; }
        public int ParkingLotId { get; set; }
    }
}
