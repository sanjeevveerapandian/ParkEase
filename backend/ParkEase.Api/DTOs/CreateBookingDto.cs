using System.ComponentModel.DataAnnotations;

namespace ParkEase.Api.DTOs
{
    // The driver's identity comes from the JWT, never from the request body. The vehicle must
    // be one of that driver's registered vehicles — that's the vehicle half of the mapping.
    public class CreateBookingDto
    {
        [Range(1, int.MaxValue)]
        public int VehicleId { get; set; }

        [Range(1, int.MaxValue)]
        public int ParkingSlotId { get; set; }
    }
}
