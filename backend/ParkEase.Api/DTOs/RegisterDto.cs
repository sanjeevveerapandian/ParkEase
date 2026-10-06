using System.ComponentModel.DataAnnotations;
using ParkEase.Api.Models;
using ParkEase.Api.Validators;

namespace ParkEase.Api.DTOs
{
    public class RegisterDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        // Used for SMS alerts, so it has to be a real, reachable mobile number.
        [Required, ValidPhone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, EnumName(typeof(UserRole))]
        public string Role { get; set; } = string.Empty; // "Driver", "Operator", or "Admin"
    }
}
