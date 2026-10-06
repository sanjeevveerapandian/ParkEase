using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ParkEase.Api.Models;

namespace ParkEase.Api.Services.Notifications
{
    // Sends SMS through Twilio's REST API (no SDK needed: it's one form-encoded POST).
    public class TwilioSmsSender : ISmsSender
    {
        private readonly HttpClient _http;
        private readonly TwilioOptions _options;
        private readonly ILogger<TwilioSmsSender> _logger;

        public TwilioSmsSender(HttpClient http, IOptions<SmsOptions> options, ILogger<TwilioSmsSender> logger)
        {
            _http = http;
            _options = options.Value.Twilio;
            _logger = logger;
        }

        public string ProviderName => "Twilio";

        public async Task<SendResult> SendAsync(string toE164, string message, CancellationToken ct = default)
        {
            var form = new Dictionary<string, string> { ["To"] = toE164, ["Body"] = message };
            if (!string.IsNullOrWhiteSpace(_options.MessagingServiceSid))
                form["MessagingServiceSid"] = _options.MessagingServiceSid;
            else
                form["From"] = _options.From;

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://api.twilio.com/2010-04-01/Accounts/{_options.AccountSid}/Messages.json")
            {
                Content = new FormUrlEncodedContent(form)
            };
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            try
            {
                using var response = await _http.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                    return new SendResult(NotificationStatus.Sent, ProviderMessageId: ReadString(body, "sid"));

                var error = $"Twilio {ReadString(body, "code") ?? ((int)response.StatusCode).ToString()}: " +
                            (ReadString(body, "message") ?? response.ReasonPhrase ?? "request failed");
                _logger.LogWarning("Twilio rejected SMS to {To}: {Error}", toE164, error);
                return new SendResult(NotificationStatus.Failed, error);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Could not reach Twilio for SMS to {To}", toE164);
                return new SendResult(NotificationStatus.Failed, $"Could not reach Twilio: {ex.Message}");
            }
        }

        private static string? ReadString(string json, string property)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty(property, out var value)) return null;
                return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
