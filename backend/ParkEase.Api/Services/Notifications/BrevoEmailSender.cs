using System.Text;
using System.Text.Json;
using ParkEase.Api.Models;

namespace ParkEase.Api.Services.Notifications
{
    public class BrevoEmailSender : IEmailSender
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BrevoEmailSender> _logger;

        public BrevoEmailSender(HttpClient http, IConfiguration configuration, ILogger<BrevoEmailSender> logger)
        {
            _http = http;
            _configuration = configuration;
            _logger = logger;
        }

        public string ProviderName => "Brevo";

        public async Task<SendResult> SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
        {
            var apiKey = _configuration["Brevo:ApiKey"];
            var senderEmail = _configuration["Brevo:SenderEmail"];
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogInformation("Email (simulated, Brevo not configured) to {To}: {Subject}", toEmail, subject);
                return new SendResult(NotificationStatus.Simulated, "Brevo is not configured.");
            }

            var payload = new
            {
                sender = new { email = senderEmail, name = _configuration["Brevo:SenderName"] ?? "ParkEase" },
                to = new[] { new { email = toEmail, name = toName } },
                subject,
                htmlContent = htmlBody
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("api-key", apiKey);
            request.Headers.Add("accept", "application/json");

            try
            {
                using var response = await _http.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (response.IsSuccessStatusCode)
                    return new SendResult(NotificationStatus.Sent, ProviderMessageId: ReadMessageId(body));

                // Surface the provider's own explanation (e.g. an unrecognised-IP block) so a failed
                // email is diagnosable from the Notifications page instead of silently lost.
                var detail = body.Length > 250 ? body[..250] : body;
                var error = $"Brevo {(int)response.StatusCode}: {detail}";
                _logger.LogWarning("Brevo rejected email to {To}: {Error}", toEmail, error);
                return new SendResult(NotificationStatus.Failed, error);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Could not reach Brevo for email to {To}", toEmail);
                return new SendResult(NotificationStatus.Failed, $"Could not reach Brevo: {ex.Message}");
            }
        }

        private static string? ReadMessageId(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty("messageId", out var id) ? id.GetString() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
