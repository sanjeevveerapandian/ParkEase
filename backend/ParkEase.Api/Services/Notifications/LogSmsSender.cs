using ParkEase.Api.Models;

namespace ParkEase.Api.Services.Notifications
{
    // Default provider when no SMS gateway is configured. Nothing is delivered: the message is
    // written to the log and recorded as Simulated, so the flow is fully visible (including in
    // the app's Notifications page) without needing gateway credentials.
    public class LogSmsSender : ISmsSender
    {
        private readonly ILogger<LogSmsSender> _logger;

        public LogSmsSender(ILogger<LogSmsSender> logger)
        {
            _logger = logger;
        }

        public string ProviderName => "Log";

        public Task<SendResult> SendAsync(string toE164, string message, CancellationToken ct = default)
        {
            _logger.LogInformation("SMS (simulated, not delivered) to {To}: {Message}", toE164, message);
            return Task.FromResult(new SendResult(NotificationStatus.Simulated));
        }
    }
}
