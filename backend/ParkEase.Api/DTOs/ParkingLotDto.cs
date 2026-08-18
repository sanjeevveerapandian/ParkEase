namespace ParkEase.Api.DTOs
{
    // What the client receives when reading a lot — includes computed slot counts,
    // never the raw EF navigation properties (avoids circular JSON serialization issues).
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