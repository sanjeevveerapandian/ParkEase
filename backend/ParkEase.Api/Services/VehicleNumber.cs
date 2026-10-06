using System.Text.RegularExpressions;

namespace ParkEase.Api.Services
{
    public static class VehicleNumber
    {
        // Indian registration: 2-letter state, 1-2 digit RTO code, 1-3 letter series, 4 digits (TN09AB1234).
        private static readonly Regex Pattern = new(@"^[A-Z]{2}\d{1,2}[A-Z]{1,3}\d{4}$", RegexOptions.Compiled);

        // "tn 09-ab 1234" -> "TN09AB1234"
        public static string Normalize(string? raw) =>
            new string((raw ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        public static bool IsValid(string? raw) => Pattern.IsMatch(Normalize(raw));
    }
}
