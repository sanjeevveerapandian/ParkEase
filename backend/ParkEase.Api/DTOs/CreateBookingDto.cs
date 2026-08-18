namespace ParkEase.Api.DTOs
{
    // No UserId here either — same pattern as CreateParkingLotDto.
    // The driver's identity comes from their JWT, never from the request body.
    public class CreateBookingDto
    {
        public int ParkingSlotId { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
    }
}