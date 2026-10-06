using System.Globalization;
using System.Net;
using System.Text;
using ParkEase.Api.Models;

namespace ParkEase.Api.Services.Notifications
{
    public record NotificationContext(
        int BookingId,
        string VehicleNumber,
        string SlotNumber,
        string LotName,
        decimal HourlyRate,
        DateTime ReservedAt,
        DateTime? ReservationExpiresAt,
        DateTime? EntryTime,
        DateTime? ExitTime,
        decimal? TotalFee,
        decimal? PenaltyFee,
        string DriverName,
        string DriverEmail,
        string DriverPhone,
        DateTime NowUtc);

    public record NotificationContent(string Sms, string Subject, string EmailText, string EmailHtml);

    // Every message the system sends is defined here, so the wording of each booking event
    // (see the notification matrix in docs/FLOW.md) lives in exactly one place.
    // SMS text uses "Rs." instead of the rupee sign: the sign isn't in the GSM-7 alphabet and
    // would force the whole message into 70-character UCS-2 segments.
    public static class NotificationTemplates
    {
        public static string FormatIst(DateTime utc) =>
            utc.AddHours(5.5).ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture) + " IST";

        public static string FormatDuration(TimeSpan span)
        {
            var hours = (int)span.TotalHours;
            return hours > 0 ? $"{hours}h {span.Minutes}m" : $"{span.Minutes}m";
        }

        private static string Money(decimal amount) => amount.ToString("F2", CultureInfo.InvariantCulture);

        public static NotificationContent Build(NotificationType type, NotificationContext c)
        {
            var booking = $"Booking #{c.BookingId}";
            var holdMinutes = c.ReservationExpiresAt is { } exp ? (int)Math.Round((exp - c.ReservedAt).TotalMinutes) : 0;

            switch (type)
            {
                case NotificationType.BookingConfirmation:
                {
                    var by = c.ReservationExpiresAt is { } e ? FormatIst(e) : "soon";
                    return Compose(
                        sms: $"ParkEase: Slot {c.SlotNumber} at {c.LotName} reserved for {c.VehicleNumber}. Check in by {by}. {booking}.",
                        subject: $"ParkEase — Slot {c.SlotNumber} reserved",
                        title: "Your parking slot is reserved",
                        intro: $"Hi {c.DriverName}, we're holding a slot for you. Check in before the hold runs out, otherwise it is released.",
                        rows: new[]
                        {
                            ("Vehicle", c.VehicleNumber),
                            ("Lot", c.LotName),
                            ("Slot", c.SlotNumber),
                            ("Rate", $"Rs.{Money(c.HourlyRate)} per hour (billing starts at check-in)"),
                            ("Check in by", by),
                            ("Booking", $"#{c.BookingId}")
                        });
                }

                case NotificationType.CheckIn:
                {
                    var at = FormatIst(c.EntryTime ?? c.NowUtc);
                    return Compose(
                        sms: $"ParkEase: {c.VehicleNumber} checked in at slot {c.SlotNumber}, {c.LotName} at {at}. Rs.{Money(c.HourlyRate)}/hr, billing started. {booking}.",
                        subject: $"ParkEase — Checked in at slot {c.SlotNumber}",
                        title: "You're checked in",
                        intro: "Your vehicle is parked and billing has started.",
                        rows: new[]
                        {
                            ("Vehicle", c.VehicleNumber),
                            ("Lot", c.LotName),
                            ("Slot", c.SlotNumber),
                            ("Checked in", at),
                            ("Rate", $"Rs.{Money(c.HourlyRate)} per hour"),
                            ("Booking", $"#{c.BookingId}")
                        });
                }

                case NotificationType.ExitReceipt:
                {
                    var entry = c.EntryTime ?? c.ReservedAt;
                    var exit = c.ExitTime ?? c.NowUtc;
                    var parked = FormatDuration(exit - entry);
                    var total = (c.TotalFee ?? 0) + (c.PenaltyFee ?? 0);
                    var penalty = c.PenaltyFee ?? 0;
                    var penaltyText = penalty > 0 ? $" (incl. Rs.{Money(penalty)} overstay)" : "";

                    var rows = new List<(string, string)>
                    {
                        ("Vehicle", c.VehicleNumber),
                        ("Lot / slot", $"{c.LotName} / {c.SlotNumber}"),
                        ("Checked in", FormatIst(entry)),
                        ("Checked out", FormatIst(exit)),
                        ("Time parked", parked),
                        ("Parking fee", $"Rs.{Money(c.TotalFee ?? 0)}")
                    };
                    if (penalty > 0) rows.Add(("Overstay penalty", $"Rs.{Money(penalty)}"));
                    rows.Add(("Total", $"Rs.{Money(total)}"));
                    rows.Add(("Booking", $"#{c.BookingId}"));

                    return Compose(
                        sms: $"ParkEase: {c.VehicleNumber} left {c.LotName} at {FormatIst(exit)}. Parked {parked}. Total Rs.{Money(total)}{penaltyText}. {booking}.",
                        subject: $"ParkEase — Receipt for {c.VehicleNumber}",
                        title: "Thanks for parking with us",
                        intro: "Here is your receipt.",
                        rows: rows);
                }

                case NotificationType.ReservationCancelled:
                    return Compose(
                        sms: $"ParkEase: Reservation #{c.BookingId} for slot {c.SlotNumber} at {c.LotName} was cancelled. No charge.",
                        subject: $"ParkEase — Reservation #{c.BookingId} cancelled",
                        title: "Reservation cancelled",
                        intro: "The slot has been released. You have not been charged.",
                        rows: new[] { ("Vehicle", c.VehicleNumber), ("Lot / slot", $"{c.LotName} / {c.SlotNumber}"), ("Booking", $"#{c.BookingId}") });

                case NotificationType.ReservationExpired:
                    return Compose(
                        sms: $"ParkEase: Reservation #{c.BookingId} for slot {c.SlotNumber} at {c.LotName} expired - no check-in within {holdMinutes} min. No charge.",
                        subject: $"ParkEase — Reservation #{c.BookingId} expired",
                        title: "Reservation expired",
                        intro: $"You didn't check in within {holdMinutes} minutes, so the slot was released. You have not been charged.",
                        rows: new[] { ("Vehicle", c.VehicleNumber), ("Lot / slot", $"{c.LotName} / {c.SlotNumber}"), ("Booking", $"#{c.BookingId}") });

                case NotificationType.OverstayWarning:
                {
                    var parkedHours = (int)((c.NowUtc - (c.EntryTime ?? c.NowUtc)).TotalHours);
                    var pastLimit = parkedHours >= FeeCalculator.OverstayThresholdHours;
                    var detail = pastLimit
                        ? $"the overstay rate ({FeeCalculator.OverstayPenaltyMultiplier}x) now applies"
                        : $"after {FeeCalculator.OverstayThresholdHours}h the overstay rate ({FeeCalculator.OverstayPenaltyMultiplier}x) applies";
                    return Compose(
                        sms: $"ParkEase: {c.VehicleNumber} has been parked {parkedHours}h at {c.LotName}; {detail}. Please exit soon. {booking}.",
                        subject: $"ParkEase — {c.VehicleNumber} is nearing the overstay limit",
                        title: "Overstay warning",
                        intro: $"{c.VehicleNumber} has been parked for about {parkedHours} hours; {detail}.",
                        rows: new[] { ("Vehicle", c.VehicleNumber), ("Lot / slot", $"{c.LotName} / {c.SlotNumber}"), ("Parked since", FormatIst(c.EntryTime ?? c.NowUtc)), ("Booking", $"#{c.BookingId}") });
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "No template for this notification type.");
            }
        }

        private static NotificationContent Compose(string sms, string subject, string title, string intro, IEnumerable<(string Label, string Value)> rows)
        {
            var list = rows.ToList();

            var text = new StringBuilder().AppendLine(title).AppendLine().AppendLine(intro).AppendLine();
            foreach (var (label, value) in list) text.AppendLine($"{label}: {value}");

            var html = new StringBuilder()
                .Append("<div style=\"font-family:Arial,sans-serif;max-width:480px;margin:auto;color:#0f172a\">")
                .Append($"<h2 style=\"color:#4338ca;margin-bottom:4px\">{WebUtility.HtmlEncode(title)}</h2>")
                .Append($"<p>{WebUtility.HtmlEncode(intro)}</p>")
                .Append("<table style=\"width:100%;border-collapse:collapse\">");
            foreach (var (label, value) in list)
            {
                html.Append("<tr>")
                    .Append($"<td style=\"padding:6px 0;color:#64748b;width:130px\">{WebUtility.HtmlEncode(label)}</td>")
                    .Append($"<td style=\"padding:6px 0;font-weight:600\">{WebUtility.HtmlEncode(value)}</td>")
                    .Append("</tr>");
            }
            html.Append("</table><p style=\"color:#94a3b8;font-size:12px\">ParkEase — smart parking</p></div>");

            return new NotificationContent(sms, subject, text.ToString().TrimEnd(), html.ToString());
        }
    }
}
