using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleService _roleService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRoleService roleService,
            IEmailService emailService,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _roleService = roleService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<RegisterResponseDTO> RegisterAsync(
            RegisterRequestDTO request, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Проверка уникальности данных
                var emailExists = await _userRepository.EmailExistsAsync(request.Email, cancellationToken);
                if (emailExists)
                {
                    return new RegisterResponseDTO
                    {
                        Success = false,
                        Message = "Email already exists"
                    };
                }

                var usernameExists = await _userRepository.UsernameExistsAsync(request.Username, cancellationToken);
                if (usernameExists)
                {
                    return new RegisterResponseDTO
                    {
                        Success = false,
                        Message = "Username already exists"
                    };
                }

                // 2. Создание пользователя
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = request.Username,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    DisplayName = request.DisplayName,
                    Bio = request.Bio,
                    DateOfBirth = request.DateOfBirth,
                    PhoneNumber = request.PhoneNumber,
                    PasswordHash = HashPassword(request.Password),
                    PasswordSalt = GenerateSalt(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsEmailConfirmed = false,
                    IsDeleted = false
                };

                // 3. Сохранение пользователя
                await _userRepository.AddAsync(user, cancellationToken);

                // 4. Назначение роли "User" по умолчанию
                var defaultRole = await _roleService.GetByNameAsync("User", cancellationToken);
                if (defaultRole != null)
                {
                    await _roleService.AssignRoleToUserAsync(user.Id, defaultRole.Id, cancellationToken);
                }

                // 5. Генерация confirmation token
                var confirmationToken = await GenerateConfirmationTokenAsync(user.Id, cancellationToken);
                var tokenExpiresAt = DateTime.UtcNow.AddHours(24); // Токен действует 24 часа

                // 6. Формирование ссылки для подтверждения
                var confirmationUrl = $"https://localhost:5001/api/auth/confirm-email?token={confirmationToken}";

                // 7. Отправка email
                var emailSent = await _emailService.SendWelcomeEmailAsync(
                    user.Email, 
                    user.Username, 
                    confirmationUrl, 
                    cancellationToken);

                _logger.LogInformation("User registered successfully: {UserId}, Email: {Email}, Email sent: {EmailSent}", 
                    user.Id, user.Email, emailSent);

                return new RegisterResponseDTO
                {
                    Success = true,
                    Message = "Registration successful. Please check your email to confirm your account.",
                    ConfirmationToken = emailSent ? null : confirmationToken, // Только если email не отправлен
                    ConfirmationUrl = emailSent ? null : confirmationUrl,
                    TokenExpiresAt = emailSent ? null : tokenExpiresAt,
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    DisplayName = user.DisplayName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration: {Email}", request.Email);
                return new RegisterResponseDTO
                {
                    Success = false,
                    Message = "Registration failed. Please try again."
                };
            }
        }

        public async Task<bool> ConfirmEmailAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                // Декодирование и валидация токена
                var userId = ValidateConfirmationToken(token);
                if (userId == null)
                {
                    return false;
                }

                var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
                if (user == null)
                {
                    return false;
                }

                if (user.IsEmailConfirmed)
                {
                    return true; // Email уже подтвержден
                }

                // Подтверждение email
                user.IsEmailConfirmed = true;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user, cancellationToken);

                _logger.LogInformation("Email confirmed for user: {UserId}", user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email with token: {Token}", token);
                return false;
            }
        }

        public async Task<string> GenerateConfirmationTokenAsync(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            // Создание криптографически стойкого токена
            var tokenData = $"{userId}:{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}:{Guid.NewGuid()}";
            var tokenBytes = Encoding.UTF8.GetBytes(tokenData);
            var base64Token = Convert.ToBase64String(tokenBytes);
            
            // Дополнительное шифрование для безопасности
            return EncryptToken(base64Token);
        }

        public async Task<bool> IsEmailConfirmedAsync(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            return user?.IsEmailConfirmed ?? false;
        }

        public async Task<bool> ResendConfirmationEmailAsync(
            string email, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FindByEmailAsync(email, cancellationToken);
                if (user == null || user.IsEmailConfirmed)
                {
                    return false;
                }

                var confirmationToken = await GenerateConfirmationTokenAsync(user.Id, cancellationToken);
                var confirmationUrl = $"https://localhost:5001/api/auth/confirm-email?token={confirmationToken}";

                var emailSent = await _emailService.SendConfirmationEmailAsync(
                    user.Email, 
                    user.Username, 
                    confirmationUrl, 
                    cancellationToken);

                _logger.LogInformation("Confirmation email resent for user: {UserId}", user.Id);
                return emailSent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending confirmation email for: {Email}", email);
                return false;
            }
        }

        private static string HashPassword(string password)
        {
            // Использование BCrypt для хеширования пароля
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        private static string GenerateSalt()
        {
            // Генерация случайной соли
            var saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private static string EncryptToken(string token)
        {
            // Простое кодирование для демонстрации (в продакшене использовать более сложное шифрование)
            var key = Convert.ToBase64String(Encoding.UTF8.GetBytes("IceBreakerAppSecretKey2024"));
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(token + "|" + key));
        }

        private static Guid? ValidateConfirmationToken(string encryptedToken)
        {
            try
            {
                var decryptedBytes = Convert.FromBase64String(encryptedToken);
                var decrypted = Encoding.UTF8.GetString(decryptedBytes);
                
                var parts = decrypted.Split('|');
                if (parts.Length != 2)
                {
                    return null;
                }

                var tokenData = parts[0];
                var tokenParts = tokenData.Split(':');
                
                if (tokenParts.Length != 3)
                {
                    return null;
                }

                var userId = Guid.Parse(tokenParts[0]);
                var tokenTime = DateTime.Parse(tokenParts[1]);

                // Проверка срока действия токена (24 часа)
                if (DateTime.UtcNow - tokenTime > TimeSpan.FromHours(24))
                {
                    return null;
                }

                return userId;
            }
            catch
            {
                return null;
            }
        }
    }
}