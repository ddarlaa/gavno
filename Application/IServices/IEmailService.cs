namespace IceBreakerApp.Application.IServices
{
    public interface IEmailService
    {
        Task<bool> SendWelcomeEmailAsync(string email, string username, string confirmationUrl, CancellationToken cancellationToken = default);
        Task<bool> SendConfirmationEmailAsync(string email, string username, string confirmationUrl, CancellationToken cancellationToken = default);
    }
}