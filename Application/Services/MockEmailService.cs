using IceBreakerApp.Application.IServices;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application.Services
{
    public class MockEmailService : IEmailService
    {
        private readonly ILogger<MockEmailService> _logger;

        public MockEmailService(ILogger<MockEmailService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> SendWelcomeEmailAsync(
            string email, 
            string username, 
            string confirmationUrl, 
            CancellationToken cancellationToken = default)
        {
            // Mock реализация - имитация отправки email
            // В реальном проекте здесь был бы код для отправки через SMTP, SendGrid, и т.д.

            var emailContent = $@"
            Добро пожаловать в IceBreakerApp, {username}!
            
            Пожалуйста, подтвердите ваш email, перейдя по ссылке:
            {confirmationUrl}
            
            Ссылка действительна в течение 24 часов.
            
            С уважением,
            Команда IceBreakerApp
            ";

            // Логирование "отправленного" email
            _logger.LogInformation("Welcome email sent to {Email} for user {Username}", email, username);
            _logger.LogInformation("Email content:\n{EmailContent}", emailContent);

            // Имитация задержки отправки
            await Task.Delay(100, cancellationToken);

            // В mock реализации всегда возвращаем true
            return true;
        }

        public async Task<bool> SendConfirmationEmailAsync(
            string email, 
            string username, 
            string confirmationUrl, 
            CancellationToken cancellationToken = default)
        {
            // Аналогичная mock реализация для повторной отправки
            var emailContent = $@"
            Подтверждение email для {username}
            
            Перейдите по ссылке для подтверждения:
            {confirmationUrl}
            
            Ссылка действительна в течение 24 часов.
            ";

            _logger.LogInformation("Confirmation email resent to {Email} for user {Username}", email, username);
            _logger.LogInformation("Email content:\n{EmailContent}", emailContent);

            await Task.Delay(100, cancellationToken);

            return true;
        }
    }
}