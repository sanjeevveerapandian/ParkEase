using System.Text.RegularExpressions;

namespace ParkEase.Api.Services.Notifications
{
    public static class PhoneNumber
    {
        private static readonly Regex IndianMobile = new(@"^[6-9]\d{9}$", RegexOptions.Compiled);
        private static readonly Regex E164 = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);

        // Accepts Indian mobiles in the forms people actually type (9876543210, 09876543210,
        // 919876543210, +91 98765 43210) plus any other valid international E.164 number,
        // and returns the canonical +E.164 form SMS gateways expect. Safe to call twice.
        public static bool TryNormalize(string? raw, out string e164)
        {
            e164 = string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var cleaned = new string(raw.Where(c => char.IsDigit(c) || c == '+').ToArray());
            if (cleaned.LastIndexOf('+') > 0) return false; // '+' only allowed at the start

            if (cleaned.StartsWith('+'))
            {
                if (!E164.IsMatch(cleaned)) return false;
                if (cleaned.StartsWith("+91") && !IndianMobile.IsMatch(cleaned[3..])) return false;
                e164 = cleaned;
                return true;
            }

            string national = cleaned.Length switch
            {
                10 => cleaned,
                11 when cleaned[0] == '0' => cleaned[1..],
                12 when cleaned.StartsWith("91") => cleaned[2..],
                _ => string.Empty
            };

            if (!IndianMobile.IsMatch(national)) return false;
            e164 = "+91" + national;
            return true;
        }

        public static bool IsValid(string? raw) => TryNormalize(raw, out _);
    }
}
