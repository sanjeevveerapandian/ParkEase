using ParkEase.Api.DTOs;
using ParkEase.Api.Models;

namespace ParkEase.Api.Services.Notifications
{
    // Called by the booking service after each lifecycle event. Sends the driver an SMS and an
    // email and records both in the Notifications table. Never throws: a failed message must
    // not undo a booking.
    public interface INotificationService
    {
        Task NotifyAsync(NotificationType type, int bookingId, CancellationToken ct = default);
    }

    public interface INotificationInboxService
    {
        Task<List<NotificationDto>> GetForDriverAsync(int driverId, int take = 100);
    }
}
