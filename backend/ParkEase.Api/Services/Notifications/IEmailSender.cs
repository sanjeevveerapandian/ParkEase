namespace ParkEase.Api.Services.Notifications
{
    public interface IEmailSender
    {
        string ProviderName { get; }

        Task<SendResult> SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default);
    }
}
