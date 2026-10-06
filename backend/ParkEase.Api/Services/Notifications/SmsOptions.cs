namespace ParkEase.Api.Services.Notifications
{
    public class SmsOptions
    {
        // "Log" (default) records the message and delivers nothing; "Twilio" sends for real.
        public string Provider { get; set; } = "Log";
        public TwilioOptions Twilio { get; set; } = new();
    }

    public class TwilioOptions
    {
        public string AccountSid { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        // A Twilio number in E.164 form, or leave blank and set MessagingServiceSid instead.
        public string From { get; set; } = string.Empty;
        public string MessagingServiceSid { get; set; } = string.Empty;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(AccountSid)
            && !string.IsNullOrWhiteSpace(AuthToken)
            && (!string.IsNullOrWhiteSpace(From) || !string.IsNullOrWhiteSpace(MessagingServiceSid));
    }
}
