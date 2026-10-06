using ParkEase.Api.Models;

namespace ParkEase.Api.Services.Notifications
{
    public record SendResult(NotificationStatus Status, string? Error = null, string? ProviderMessageId = null);

    public interface ISmsSender
    {
        string ProviderName { get; }

        // toE164 is already normalized (e.g. +919876543210). Implementations report failures
        // through SendResult rather than throwing.
        Task<SendResult> SendAsync(string toE164, string message, CancellationToken ct = default);
    }
}
