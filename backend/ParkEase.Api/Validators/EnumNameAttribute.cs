using System.ComponentModel.DataAnnotations;

namespace ParkEase.Api.Validators
{
    // Validates that a string is the name of a value of the given enum (case-insensitive),
    // since the API exchanges enums as readable strings.
    public class EnumNameAttribute : ValidationAttribute
    {
        private readonly Type _enumType;

        public EnumNameAttribute(Type enumType)
            : base($"Must be one of: {string.Join(", ", Enum.GetNames(enumType))}.")
        {
            _enumType = enumType;
        }

        public override bool IsValid(object? value) =>
            value is string s
            && Enum.TryParse(_enumType, s, ignoreCase: true, out var parsed)
            && Enum.IsDefined(_enumType, parsed!);
    }
}
