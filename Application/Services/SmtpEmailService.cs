using IceBreakerApp.Application.IServices;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace IceBreakerApp.Application.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly SmtpClient _smtpClient;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SmtpEmailService(
            ILogger<SmtpEmailService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            
            var smtpHost = configuration["Smtp:Host"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(configuration["Smtp:Port"] ?? "587");
            var smtpUsername = configuration["Smtp:Username"] ?? throw new ArgumentException("SMTP username is required");
            var smtpPassword = configuration["Smtp:Password"] ?? throw new ArgumentException("SMTP password is required");
            var enableSsl = bool.Parse(configuration["Smtp:EnableSsl"] ?? "true");
            
            _fromEmail = configuration["Smtp:FromEmail"] ?? smtpUsername;
            _fromName = configuration["Smtp:FromName"] ?? "IceBreakerApp";
            
            _smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };
        }

        public async Task<bool> SendWelcomeEmailAsync(
            string email, 
            string username, 
            string confirmationUrl, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = "Добро пожаловать в IceBreakerApp!",
                    IsBodyHtml = true,
                    Body = GenerateWelcomeEmailHtml(username, confirmationUrl)
                };
                
                mailMessage.To.Add(new MailAddress(email, username));

                // Создаем альтернативную текстовую версию
                var textVersion = $@"Привет, {username}!

Добро пожаловать в IceBreakerApp!

Для завершения регистрации подтвердите ваш email:
{confirmationUrl}

Ссылка действительна в течение 24 часов.

С уважением,
Команда IceBreakerApp";

                var textView = AlternateView.CreateAlternateViewFromString(textVersion, Encoding.UTF8, "text/plain");
                mailMessage.AlternateViews.Add(textView);

                await _smtpClient.SendMailAsync(mailMessage);
                
                _logger.LogInformation("Welcome email sent successfully to {Email} for user {Username}", email, username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending welcome email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendConfirmationEmailAsync(
            string email, 
            string username, 
            string confirmationUrl, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = "Повторная отправка - Подтвердите email",
                    IsBodyHtml = true,
                    Body = GenerateConfirmationEmailHtml(username, confirmationUrl)
                };
                
                mailMessage.To.Add(new MailAddress(email, username));

                // Создаем альтернативную текстовую версию
                var textVersion = $@"{username}, для подтверждения email перейдите по ссылке:
{confirmationUrl}

Ссылка действительна в течение 24 часов.

IceBreakerApp";

                var textView = AlternateView.CreateAlternateViewFromString(textVersion, Encoding.UTF8, "text/plain");
                mailMessage.AlternateViews.Add(textView);

                await _smtpClient.SendMailAsync(mailMessage);
                
                _logger.LogInformation("Confirmation email sent successfully to {Email} for user {Username}", email, username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending confirmation email to {Email}", email);
                return false;
            }
        }

        private string GenerateWelcomeEmailHtml(string username, string confirmationUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Добро пожаловать в IceBreakerApp</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; border-radius: 10px; text-align: center; margin-bottom: 20px;'>
        <h1 style='color: white; margin: 0; font-size: 28px;'>🎉 Добро пожаловать!</h1>
        <p style='color: white; margin: 10px 0 0 0; font-size: 18px;'>в IceBreakerApp</p>
    </div>
    
    <div style='background: #f8f9fa; padding: 25px; border-radius: 8px; margin-bottom: 20px;'>
        <h2 style='color: #495057; margin-top: 0;'>Привет, {username}! 👋</h2>
        <p style='margin-bottom: 20px;'>Мы рады приветствовать вас в нашем сообществе!</p>
        
        <div style='background: white; padding: 20px; border-radius: 6px; border-left: 4px solid #007bff; margin: 20px 0;'>
            <h3 style='color: #007bff; margin-top: 0;'>📧 Подтвердите ваш email</h3>
            <p style='margin-bottom: 15px;'>Для завершения регистрации нажмите кнопку ниже:</p>
            
            <div style='text-align: center; margin: 25px 0;'>
                <a href='{confirmationUrl}' 
                   style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block; font-size: 16px;'>
                    ✅ Подтвердить email
                </a>
            </div>
            
            <p style='font-size: 14px; color: #6c757d; margin: 0;'>
                <strong>Внимание:</strong> Ссылка действительна в течение 24 часов.
            </p>
        </div>
        
        <p style='font-size: 14px; color: #6c757d; margin: 20px 0 0 0;'>
            Если кнопка не работает, скопируйте и вставьте эту ссылку в браузер:<br>
            <code style='background: #e9ecef; padding: 2px 6px; border-radius: 3px; word-break: break-all;'>{confirmationUrl}</code>
        </p>
    </div>
    
    <div style='text-align: center; padding: 20px; color: #6c757d; font-size: 14px;'>
        <p>С уважением,<br><strong>Команда IceBreakerApp</strong></p>
        <p style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #dee2e6;'>
            Это автоматическое сообщение. Не отвечайте на него.
        </p>
    </div>
</body>
</html>";
        }

        private string GenerateConfirmationEmailHtml(string username, string confirmationUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Подтверждение email</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: #ffc107; padding: 20px; border-radius: 8px; text-align: center; margin-bottom: 20px;'>
        <h1 style='color: #856404; margin: 0;'>📧 Подтверждение email</h1>
    </div>
    
    <div style='background: #f8f9fa; padding: 25px; border-radius: 8px;'>
        <p>Привет, <strong>{username}</strong>!</p>
        <p>Мы повторно отправили вам ссылку для подтверждения email.</p>
        
        <div style='text-align: center; margin: 25px 0;'>
            <a href='{confirmationUrl}' 
               style='background: #ffc107; color: #856404; padding: 15px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block; font-size: 16px;'>
                🔗 Подтвердить email
            </a>
        </div>
        
        <p style='font-size: 14px; color: #6c757d; margin: 20px 0 0 0;'>
            Ссылка действительна в течение 24 часов.
        </p>
    </div>
</body>
</html>";
        }
    }
}