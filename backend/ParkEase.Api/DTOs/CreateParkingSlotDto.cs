using System.ComponentModel.DataAnnotations;
using ParkEase.Api.Models;
using ParkEase.Api.Validators;

namespace ParkEase.Api.DTOs
{
    public class CreateParkingSlotDto
    {
        [Required, StringLength(20, MinimumLength = 1)]
        public string SlotNumber { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "100000")]
        public decimal HourlyRate { get; set; }

        // Which vehicles may park here; bookings only map a vehicle to a slot of its own type.
        [EnumName(typeof(VehicleType))]
        public string VehicleType { get; set; } = nameof(Models.VehicleType.Car);
    }

    public class UpdateSlotStatusDto
    {
        // Operators can only toggle between these two; Reserved/Occupied are driven by bookings.
        [Required, AllowedValues("Available", "Maintenance")]
        public string Status { get; set; } = string.Empty;
    }
}
