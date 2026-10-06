namespace ParkEase.Api.DTOs
{

    public class ParkingLotDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int OperatorId { get; set; }
        public int TotalSlots { get; set; }
        public int AvailableSlots { get; set; }
    }
}