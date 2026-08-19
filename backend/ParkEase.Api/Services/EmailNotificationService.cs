using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ParkEase.Api.Data;
using ParkEase.Api.Models;

namespace ParkEase.Api.Services
{
    public class EmailNotificationService : INotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public EmailNotificationService(HttpClient httpClient, IConfiguration configuration, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;
        }

        public async Task SendBookingConfirmationAsync(VehicleBooking booking, string recipientEmail)
        {
            var subject = "ParkEase — Booking Confirmed";
            var body = $"<p>Your vehicle <strong>{booking.VehicleNumber}</strong> has been checked in at " +
                       $"{booking.EntryTime:dd MMM yyyy, hh:mm tt} UTC.</p>";

            bool sent = await SendAsync(recipientEmail, subject, body);
            await LogNotificationAsync(booking.Id, NotificationType.BookingConfirmation, recipientEmail, sent);
        }

        public async Task SendExitReceiptAsync(VehicleBooking booking, string recipientEmail)
        {
            var subject = "ParkEase — Exit Receipt";
            var body = $"<p>Vehicle <strong>{booking.VehicleNumber}</strong> exited at " +
                       $"{booking.ExitTime:dd MMM yyyy, hh:mm tt} UTC.</p>" +
                       $"<p>Total Fee: ₹{booking.TotalFee}<br/>" +
                       (booking.PenaltyFee > 0 ? $"Overstay Penalty: ₹{booking.PenaltyFee}<br/>" : "") +
                       $"</p>";

            bool sent = await SendAsync(recipientEmail, subject, body);
            await LogNotificationAsync(booking.Id, NotificationType.ExpiryAlert, recipientEmail, sent);
        }

        // Returns whether Brevo actually accepted the send — the caller uses this
        // to log an honest IsSent value instead of assuming success.
        private async Task<bool> SendAsync(string toEmail, string subject, string htmlBody)
        {
            var apiKey = _configuration["Brevo:ApiKey"];
            var senderEmail = _configuration["Brevo:SenderEmail"];
            var senderName = _configuration["Brevo:SenderName"];

            var payload = new
            {
                sender = new { email = senderEmail, name = senderName },
                to = new[] { new { email = toEmail } },
                subject,
                htmlContent = htmlBody
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("api-key", apiKey);
            request.Headers.Add("accept", "application/json");

            // Deliberately not throwing on failure — a failed email should never crash
            // the booking flow itself. We log it and move on; the booking still succeeded.
            try
            {
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task LogNotificationAsync(int bookingId, NotificationType type, string recipient, bool sent)
        {
            _context.Notifications.Add(new Notification
            {
                VehicleBookingId = bookingId,
                Type = type,
                Channel = NotificationChannel.Email,
                Recipient = recipient,
                IsSent = sent,
                SentAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}