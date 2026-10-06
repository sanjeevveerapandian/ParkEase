using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ParkEase.Api.Models;
using ParkEase.Api.Services.Notifications;

namespace ParkEase.Api.Tests
{
    public class TwilioSmsSenderTests
    {
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

            public HttpRequestMessage? Request { get; private set; }
            public string? Body { get; private set; }

            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Request = request;
                Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
                return _respond(request);
            }
        }

        private static TwilioSmsSender Sender(StubHandler handler, string from = "+15005550006", string messagingServiceSid = "") =>
            new(new HttpClient(handler),
                Options.Create(new SmsOptions
                {
                    Provider = "Twilio",
                    Twilio = new TwilioOptions { AccountSid = "AC123", AuthToken = "secret", From = from, MessagingServiceSid = messagingServiceSid }
                }),
                NullLogger<TwilioSmsSender>.Instance);

        private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
            new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        [Fact]
        public async Task A_message_is_posted_to_the_account_messages_endpoint_with_basic_auth()
        {
            var handler = new StubHandler(_ => Json(HttpStatusCode.Created, "{\"sid\":\"SM999\"}"));

            var result = await Sender(handler).SendAsync("+919876543210", "Hello & welcome");

            Assert.Equal(HttpMethod.Post, handler.Request!.Method);
            Assert.Equal("https://api.twilio.com/2010-04-01/Accounts/AC123/Messages.json", handler.Request.RequestUri!.ToString());
            Assert.Equal("Basic", handler.Request.Headers.Authorization!.Scheme);
            Assert.Equal(Convert.ToBase64String(Encoding.ASCII.GetBytes("AC123:secret")), handler.Request.Headers.Authorization.Parameter);

            Assert.Contains("To=%2B919876543210", handler.Body);
            Assert.Contains("From=%2B15005550006", handler.Body);
            Assert.Contains("Body=Hello+%26+welcome", handler.Body);
            Assert.DoesNotContain("MessagingServiceSid", handler.Body);

            Assert.Equal(NotificationStatus.Sent, result.Status);
            Assert.Equal("SM999", result.ProviderMessageId);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task A_messaging_service_replaces_the_from_number()
        {
            var handler = new StubHandler(_ => Json(HttpStatusCode.Created, "{\"sid\":\"SM1\"}"));

            await Sender(handler, from: "", messagingServiceSid: "MG42").SendAsync("+919876543210", "hi");

            Assert.Contains("MessagingServiceSid=MG42", handler.Body);
            Assert.DoesNotContain("From=", handler.Body);
        }

        [Fact]
        public async Task A_twilio_error_becomes_a_failed_result_with_code_and_message()
        {
            var handler = new StubHandler(_ => Json(HttpStatusCode.BadRequest,
                "{\"code\":21608,\"message\":\"The number is unverified\",\"status\":400}"));

            var result = await Sender(handler).SendAsync("+919876543210", "hi");

            Assert.Equal(NotificationStatus.Failed, result.Status);
            Assert.Equal("Twilio 21608: The number is unverified", result.Error);
            Assert.Null(result.ProviderMessageId);
        }

        [Fact]
        public async Task A_non_json_error_falls_back_to_the_http_status()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("<html>oops</html>"),
                ReasonPhrase = "Bad Gateway"
            });

            var result = await Sender(handler).SendAsync("+919876543210", "hi");

            Assert.Equal(NotificationStatus.Failed, result.Status);
            Assert.Equal("Twilio 502: Bad Gateway", result.Error);
        }

        [Fact]
        public async Task A_network_failure_is_reported_not_thrown()
        {
            var handler = new StubHandler(_ => throw new HttpRequestException("no route to host"));

            var result = await Sender(handler).SendAsync("+919876543210", "hi");

            Assert.Equal(NotificationStatus.Failed, result.Status);
            Assert.Contains("no route to host", result.Error);
        }

        [Fact]
        public async Task A_timeout_is_reported_not_thrown()
        {
            var handler = new StubHandler(_ => throw new TaskCanceledException("timed out"));

            var result = await Sender(handler).SendAsync("+919876543210", "hi");

            Assert.Equal(NotificationStatus.Failed, result.Status);
        }

        [Fact]
        public void Twilio_is_only_configured_with_credentials_and_a_sender()
        {
            Assert.False(new TwilioOptions().IsConfigured);
            Assert.False(new TwilioOptions { AccountSid = "AC", AuthToken = "t" }.IsConfigured);
            Assert.True(new TwilioOptions { AccountSid = "AC", AuthToken = "t", From = "+1500" }.IsConfigured);
            Assert.True(new TwilioOptions { AccountSid = "AC", AuthToken = "t", MessagingServiceSid = "MG" }.IsConfigured);
        }

        [Fact]
        public async Task The_log_sender_delivers_nothing_and_says_so()
        {
            var sender = new LogSmsSender(NullLogger<LogSmsSender>.Instance);

            var result = await sender.SendAsync("+919876543210", "hi");

            Assert.Equal(NotificationStatus.Simulated, result.Status);
            Assert.Equal("Log", sender.ProviderName);
        }
    }
}
