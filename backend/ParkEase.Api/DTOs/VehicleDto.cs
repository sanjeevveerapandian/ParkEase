using System.ComponentModel.DataAnnotations;
using ParkEase.Api.Models;
using ParkEase.Api.Validators;

namespace ParkEase.Api.DTOs
{
    public class VehicleDto
    {
        public int Id { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Car" | "Bike"
        public DateTime CreatedAt { get; set; }
    }

    public class CreateVehicleDto
    {
        [Required, ValidVehicleNumber]
        public string VehicleNumber { get; set; } = string.Empty;

        [Required, EnumName(typeof(VehicleType))]
        public string Type { get; set; } = nameof(VehicleType.Car);
    }
}
