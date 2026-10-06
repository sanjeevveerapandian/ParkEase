using Microsoft.EntityFrameworkCore;
using ParkEase.Api.Data;
using ParkEase.Api.DTOs;
using ParkEase.Api.Models;

namespace ParkEase.Api.Services.Notifications
{
    public class NotificationService : INotificationService, INotificationInboxService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsSender _sms;
        private readonly IEmailSender _email;
        private readonly TimeProvider _clock;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            ISmsSender sms,
            IEmailSender email,
            TimeProvider clock,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _sms = sms;
            _email = email;
            _clock = clock;
            _logger = logger;
        }

        public async Task NotifyAsync(NotificationType type, int bookingId, CancellationToken ct = default)
        {
            try
            {
                var now = _clock.GetUtcNow().UtcDateTime;
                var data = await _context.VehicleBookings
                    .Where(b => b.Id == bookingId)
                    .Select(b => new NotificationContext(
                        b.Id,
                        b.VehicleNumber,
                        b.ParkingSlot!.SlotNumber,
                        b.ParkingSlot.ParkingLot!.Name,
                        b.ParkingSlot.HourlyRate,
                        b.ReservedAt,
                        b.ReservationExpiresAt,
                        b.EntryTime,
                        b.ExitTime,
                        b.TotalFee,
                        b.PenaltyFee,
                        b.User!.FullName,
                        b.User.Email,
                        b.User.PhoneNumber,
                        now))
                    .FirstOrDefaultAsync(ct);

                if (data == null)
                {
                    _logger.LogWarning("Cannot notify for missing booking {BookingId}", bookingId);
                    return;
                }

                var content = NotificationTemplates.Build(type, data);

                var sms = await SendSmsAsync(data.DriverPhone, content.Sms, ct);
                var email = await SendEmailAsync(data, content, ct);

                _context.Notifications.AddRange(
                    new Notification
                    {
                        VehicleBookingId = bookingId,
                        Type = type,
                        Channel = NotificationChannel.Sms,
                        Recipient = sms.Recipient,
                        Message = content.Sms,
                        Status = sms.Result.Status,
                        Provider = _sms.ProviderName,
                        Error = Trim(sms.Result.Error),
                        CreatedAt = now
                    },
                    new Notification
                    {
                        VehicleBookingId = bookingId,
                        Type = type,
                        Channel = NotificationChannel.Email,
                        Recipient = data.DriverEmail,
                        Subject = content.Subject,
                        Message = content.EmailText,
                        Status = email.Status,
                        Provider = _email.ProviderName,
                        Error = Trim(email.Error),
                        CreatedAt = now
                    });
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification {Type} for booking {BookingId} failed", type, bookingId);
            }
        }

        private async Task<(string Recipient, SendResult Result)> SendSmsAsync(string rawPhone, string message, CancellationToken ct)
        {
            if (!PhoneNumber.TryNormalize(rawPhone, out var e164))
                return (rawPhone, new SendResult(NotificationStatus.Failed, $"'{rawPhone}' is not a valid mobile number."));

            try
            {
                return (e164, await _sms.SendAsync(e164, message, ct));
            }
            catch (Exception ex)
            {
                return (e164, new SendResult(NotificationStatus.Failed, ex.Message));
            }
        }

        private async Task<SendResult> SendEmailAsync(NotificationContext data, NotificationContent content, CancellationToken ct)
        {
            try
            {
                return await _email.SendAsync(data.DriverEmail, data.DriverName, content.Subject, content.EmailHtml, ct);
            }
            catch (Exception ex)
            {
                return new SendResult(NotificationStatus.Failed, ex.Message);
            }
        }

        private static string? Trim(string? error) =>
            error is { Length: > 500 } ? error[..500] : error;

        public async Task<List<NotificationDto>> GetForDriverAsync(int driverId, int take = 100)
        {
            return await _context.Notifications
                .Where(n => n.VehicleBooking!.UserId == driverId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(take)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    BookingId = n.VehicleBookingId,
                    Type = n.Type.ToString(),
                    Channel = n.Channel.ToString(),
                    Recipient = n.Recipient,
                    Subject = n.Subject,
                    Message = n.Message,
                    Status = n.Status.ToString(),
                    Provider = n.Provider,
                    Error = n.Error,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();
        }
    }
}
