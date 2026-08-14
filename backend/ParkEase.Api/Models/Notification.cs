namespace ParkEase.Api.Models
{
    public enum NotificationType
    {
        BookingConfirmation,
        ExpiryAlert,
        PaymentReminder
    }

    public enum NotificationChannel
    {
        Email,
        Sms
    }

    public class Notification
    {
        public int Id { get; set; }
        public int VehicleBookingId { get; set; }
        public VehicleBooking? VehicleBooking { get; set; }
        public NotificationType Type { get; set; }
        public NotificationChannel Channel { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public bool IsSent { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}