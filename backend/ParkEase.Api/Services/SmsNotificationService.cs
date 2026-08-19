using ParkEase.Api.Data;
using ParkEase.Api.Models;

namespace ParkEase.Api.Services
{
    // Implements the SAME INotificationService interface as EmailNotificationService.
    // Not registered as the active implementation in Program.cs — SMS to Indian numbers
    // requires DLT (telecom regulator) sender/template registration, which takes several
    // business days of approval, outside this project's timeline.
    // This class proves the architecture is provider-swappable: activating SMS in
    // production is a one-line change in Program.cs, not a redesign.
    public class SmsNotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public SmsNotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendBookingConfirmationAsync(VehicleBooking booking, string recipientPhone)
        {
            await LogSimulatedSms(booking.Id, NotificationType.BookingConfirmation, recipientPhone);
        }

        public async Task SendExitReceiptAsync(VehicleBooking booking, string recipientPhone)
        {
            await LogSimulatedSms(booking.Id, NotificationType.ExpiryAlert, recipientPhone);
        }

        private async Task LogSimulatedSms(int bookingId, NotificationType type, string recipient)
        {
            _context.Notifications.Add(new Notification
            {
                VehicleBookingId = bookingId,
                Type = type,
                Channel = NotificationChannel.Sms,
                Recipient = recipient,
                IsSent = false, // honestly reflects: not actually delivered, pending DLT registration
                SentAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}