namespace ParkEase.Api.Models
{
    // One value per booking event that notifies the driver. Numbers 0 and 1 match the
    // old BookingConfirmation / ExpiryAlert values (ExpiryAlert was really being used
    // for exit receipts), so existing rows keep their meaning.
    public enum NotificationType
    {
        BookingConfirmation = 0,
        ExitReceipt = 1,
        CheckIn = 3,
        ReservationCancelled = 4,
        ReservationExpired = 5,
        OverstayWarning = 6
    }

    public enum NotificationChannel
    {
        Email = 0,
        Sms = 1
    }

    public enum NotificationStatus
    {
        Pending = 0,
        Sent = 1,      // the provider accepted the message
        Failed = 2,    // the provider rejected it or couldn't be reached (see Error)
        Simulated = 3  // no real provider configured — message recorded, nothing delivered
    }

    // A delivery log entry: what was sent, to whom, through which provider, and what
    // happened. The driver sees these in the app's Notifications page.
    public class Notification
    {
        public int Id { get; set; }
        public int VehicleBookingId { get; set; }
        public VehicleBooking? VehicleBooking { get; set; }

        public NotificationType Type { get; set; }
        public NotificationChannel Channel { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string? Subject { get; set; }          // email only
        public string Message { get; set; } = string.Empty;
        public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
        public string Provider { get; set; } = string.Empty; // e.g. "Twilio", "Log", "Brevo"
        public string? Error { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
