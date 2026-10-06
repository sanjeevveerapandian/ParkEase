using System.ComponentModel.DataAnnotations;
using ParkEase.Api.Services;

namespace ParkEase.Api.Validators
{
    public class ValidVehicleNumberAttribute : ValidationAttribute
    {
        public ValidVehicleNumberAttribute()
            : base("Enter a valid vehicle number, e.g. TN09AB1234.")
        {
        }

        public override bool IsValid(object? value) =>
            value is string s && VehicleNumber.IsValid(s);
    }
}
