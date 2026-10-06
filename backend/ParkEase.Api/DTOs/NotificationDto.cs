namespace ParkEase.Api.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string Type { get; set; } = string.Empty;    // BookingConfirmation | CheckIn | ExitReceipt | ...
        public string Channel { get; set; } = string.Empty; // Sms | Email
        public string Recipient { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;  // Sent | Simulated | Failed | Pending
        public string Provider { get; set; } = string.Empty;
        public string? Error { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
