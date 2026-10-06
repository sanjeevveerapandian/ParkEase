using System.ComponentModel.DataAnnotations;
using ParkEase.Api.Services.Notifications;

namespace ParkEase.Api.Validators
{
    public class ValidPhoneAttribute : ValidationAttribute
    {
        public ValidPhoneAttribute()
            : base("Enter a valid mobile number, e.g. 9876543210 or +919876543210.")
        {
        }

        public override bool IsValid(object? value) =>
            value is string s && PhoneNumber.IsValid(s);
    }
}
