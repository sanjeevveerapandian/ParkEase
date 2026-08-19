using ParkEase.Api.Models;

namespace ParkEase.Api.Services
{
    public interface INotificationService
    {
        // channel-agnostic by design — see project notes: SMS needs India DLT registration
        // (multi-day approval), so email is the live implementation; SMS would implement
        // this same interface later without touching any calling code.
        Task SendBookingConfirmationAsync(VehicleBooking booking, string recipientEmail);
        Task SendExitReceiptAsync(VehicleBooking booking, string recipientEmail);
    }
}