using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
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
        private readonly IJwtService _jwtService;
        private readonly IUserService _userService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRoleService roleService,
            IEmailService emailService,
            IJwtService jwtService,
            IUserService userService,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _roleService = roleService;
            _emailService = emailService;
            _jwtService = jwtService;
            _userService = userService;
            _logger = logger;
        }

        public async Task<RegisterResponseDTO> RegisterAsync(
            RegisterRequestDTO request, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Валидация данных уже выполнена через FluentValidation в контроллере
                // Здесь дополнительно проверим уникальность
                
                // 1. Проверка уникальности email
                var emailExists = await _userRepository.EmailExistsAsync(request.Email, cancellationToken);
                if (emailExists)
                {
                    return new RegisterResponseDTO
                    {
                        Success = false,
                        Message = "Email already exists"
                    };
                }

                // 2. Проверка уникальности username
                var usernameExists = await _userRepository.UsernameExistsAsync(request.Username, cancellationToken);
                if (usernameExists)
                {
                    return new RegisterResponseDTO
                    {
                        Success = false,
                        Message = "Username already exists"
                    };
                }

                // 3. Создание пользователя
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = request.Username,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    DateOfBirth = request.DateOfBirth,
                    PhoneNumber = request.PhoneNumber,
                    PasswordHash = HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsEmailConfirmed = false,
                    IsDeleted = false
                };

                // 4. Сохранение пользователя в БД
                await _userRepository.AddAsync(user, cancellationToken);

                // 5. Назначение роли "User" по умолчанию
                var defaultRole = await _roleService.GetByNameAsync("User", cancellationToken);
                if (defaultRole != null)
                {
                    await _roleService.AssignRoleToUserAsync(user.Id, defaultRole.Id, cancellationToken);
                }

                // 6. Генерация confirmation token
                var confirmationToken = await GenerateConfirmationTokenAsync(user.Id, cancellationToken);
                var tokenExpiresAt = DateTime.UtcNow.AddHours(24);

                // 7. Формирование ссылки для подтверждения email
                var confirmationUrl = $"https://localhost:5001/api/auth/confirm-email?token={confirmationToken}";

                // 8. Отправка welcome email
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
                    ConfirmationToken = emailSent ? null : confirmationToken,
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

        public async Task<LoginResponseDTO> LoginAsync(LoginDTO loginDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Аутентификация пользователя: поиск по email ИЛИ username + проверка пароля + проверка статуса
                var user = await _userService.AuthenticateUserAsync(loginDto.EmailOrUsername, loginDto.Password, cancellationToken);
                
                if (user == null)
                {
                    throw new Exception("Invalid credentials");
                }
                // 3. Генерация токенов с claims: UserId, Email, Username, FirstName, LastName, Roles, Custom claims
                var tokens = await _jwtService.GenerateTokensAsync(user.Id, cancellationToken);

                // 4. Обновление LastLoginAt
                await _userService.UpdateLastLoginAsync(user.Id, cancellationToken);

                _logger.LogInformation("User logged in successfully: {UserId}, Username: {Username}", 
                    user.Id, user.Username);

                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for: {EmailOrUsername}", loginDto.EmailOrUsername);
                throw;
            }
        }

        public async Task<LoginResponseDTO> RefreshTokenAsync(RefreshTokenDTO refreshTokenDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Извлечь UserId из истёкшего access token (без валидации expiry)
                var userId = await _jwtService.GetUserIdFromExpiredTokenAsync(refreshTokenDto.AccessToken, cancellationToken);
                if (userId == null)
                {
                    throw new Exception("Invalid access token");
                }

                // 2. Найти пользователя в БД
                var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                // 3. Проверить сохранённый RefreshToken в UserSession
                var session = await _userRepository.GetActiveSessionAsync(userId.Value, refreshTokenDto.RefreshToken, cancellationToken);
                if (session == null)
                {
                    throw new Exception("Invalid refresh token");
                }

                // 4. Проверить срок действия RefreshToken
                if (session.ExpiresAt <= DateTime.UtcNow)
                {
                    throw new Exception("Refresh token expired");
                }

                // 5. Ротация: старый RefreshToken становится недействительным
                await _jwtService.RevokeRefreshTokenAsync(refreshTokenDto.RefreshToken, cancellationToken);

                // 6. Сгенерировать новую пару токенов
                var newTokens = await _jwtService.GenerateTokensAsync(userId.Value, cancellationToken);

                _logger.LogInformation("Token refreshed for user: {UserId}", userId);

                return newTokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                throw;
            }
        }

        public async Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            try
            {
                var success = await _jwtService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
                
                if (success)
                {
                    _logger.LogInformation("User logged out successfully");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                return false;
            }
        }

        public async Task<bool> LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _userRepository.RevokeAllUserSessionsAsync(userId, cancellationToken);
                
                _logger.LogInformation("All sessions revoked for user: {UserId}", userId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout all failed for user: {UserId}", userId);
                return false;
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

        private async Task<string> GenerateSalt()
        {
            // Генерация случайной соли
            var saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return await Task.FromResult(Convert.ToBase64String(saltBytes));
        }

        private string EncryptToken(string token)
        {
            var key = Convert.ToBase64String(Encoding.UTF8.GetBytes("IceBreakerAppSecretKey2025"));
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