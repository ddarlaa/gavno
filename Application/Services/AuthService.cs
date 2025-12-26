using FluentValidation;
using FluentValidation.Results;
using IceBreakerApp.Application.Authorization;
using IceBreakerApp.Application.Authorization.Handlers;
using IceBreakerApp.Application.Authorization.Requirements;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.DTOs.Update;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.Validators;
using IceBreakerApp.Domain;
using IceBreakerApp.Domain.IRepositories;
using IceBreakerApp.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleService _roleService;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRoleService roleService,
            IJwtService jwtService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _roleService = roleService;
            _jwtService = jwtService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RegisterResponseDTO> RegisterAsync(
            RegisterRequestDTO request, 
            CancellationToken cancellationToken = default)
        {
            try
            {
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

                // 3. Создание пользователя в локальной базе
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                var userId = Guid.NewGuid();
                
                var user = new User
                {
                    Id = userId,
                    Email = request.Email,
                    Username = request.Username,
                    PasswordHash = passwordHash,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    IsActive = true,
                    IsDeleted = false
                };

                await _userRepository.AddAsync(user, cancellationToken);

                // 4. Назначение роли "User" по умолчанию
                await _roleService.AssignRoleToUserAsync(userId, "User", cancellationToken);

                // 5. Отправка приветственного email (без подтверждения)
                var emailSent = await _emailService.SendWelcomeEmailAsync(
                    request.Email, 
                    request.Username, 
                    string.Empty, // Без ссылки подтверждения
                    cancellationToken);

                _logger.LogInformation("User registered successfully in local database: {UserId}, Email: {Email}, Email sent: {EmailSent}", 
                    userId, request.Email, emailSent);

                return new RegisterResponseDTO
                {
                    Success = true,
                    Message = "Registration successful. Welcome to Ice Breaker App!",
                    UserId = userId,
                    Username = request.Username,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName
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
                // 1. Поиск пользователя по email или username
                User? user = null;
                
                if (loginDto.EmailOrUsername.Contains("@"))
                {
                    user = await _userRepository.FindByEmailAsync(loginDto.EmailOrUsername, cancellationToken);
                }
                else
                {
                    user = await _userRepository.FindByUsernameAsync(loginDto.EmailOrUsername, cancellationToken);
                }

                if (user == null || !user.IsActive)
                {
                    throw new Exception("Invalid credentials");
                }

                // 2. Проверка пароля
                if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    throw new Exception("Invalid credentials");
                }

                // 3. Обновление времени последнего входа
                user.LastLoginAt = DateTime.UtcNow.ToPostgreSafeUtc();
                await _userRepository.UpdateAsync(user, cancellationToken);

                // 4. Генерация JWT токенов
                var userRoles = await _roleService.GetUserRolesAsync(user.Id, cancellationToken);
                var tokensResponse = await _jwtService.GenerateTokensAsync(user.Id, cancellationToken);
                
                var accessToken = tokensResponse.AccessToken;
                var refreshToken = tokensResponse.RefreshToken;
                var refreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);

                // 5. Сохранение refresh токена в сессии
                var session = new UserSession
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RefreshTokenHash = refreshTokenHash,
                    ExpiresAt = DateTime.UtcNow.AddDays(7).ToPostgreSafeUtc(), // Refresh токен действует 7 дней
                    CreatedAt = DateTime.UtcNow.ToPostgreSafeUtc()
                };

                await _userRepository.AddSessionAsync(session, cancellationToken);

                _logger.LogInformation("User logged in successfully: {EmailOrUsername}", 
                    loginDto.EmailOrUsername);

                return new LoginResponseDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };
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
                // Находим сессию по refresh токену
                var sessions = await _userRepository.GetActiveSessionsByRefreshTokenAsync(refreshTokenDto.RefreshToken, cancellationToken);
                var session = sessions.FirstOrDefault();

                if (session == null)
                {
                    throw new Exception("Invalid refresh token");
                }

                // Проверяем, что токен не истек
                if (session.ExpiresAt <= DateTime.UtcNow.ToPostgreSafeUtc())
                {
                    session.IsRevoked = true;
                    await _userRepository.SaveChangesAsync(cancellationToken);
                    throw new Exception("Refresh token expired");
                }

                // Получаем пользователя
                var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
                if (user == null || !user.IsActive)
                {
                    throw new Exception("User not found or inactive");
                }

                // Генерируем новые токены
                var userRoles = await _roleService.GetUserRolesAsync(user.Id, cancellationToken);
                var newTokensResponse = await _jwtService.GenerateTokensAsync(user.Id, cancellationToken);
                
                var newAccessToken = newTokensResponse.AccessToken;
                var newRefreshToken = newTokensResponse.RefreshToken;
                var newRefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);

                // Обновляем сессию
                session.RefreshTokenHash = newRefreshTokenHash;
                session.ExpiresAt = DateTime.UtcNow.AddDays(7).ToPostgreSafeUtc();
                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);

                return new LoginResponseDTO
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken
                };
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
                await _userRepository.RevokeRefreshTokenAsync(Guid.Empty, refreshToken, cancellationToken);
                _logger.LogInformation("User logged out successfully");
                return true;
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
                // Для локальной реализации без двухфакторной валидации
                // Возвращаем true, так как email считается подтвержденным
                return await Task.FromResult(true);
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
            // Генерируем простой токен подтверждения (но не используем)
            return await Task.FromResult(Guid.NewGuid().ToString());
        }

        public async Task<bool> IsEmailConfirmedAsync(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                return user?.IsEmailConfirmed ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email confirmation status for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ResendConfirmationEmailAsync(
            string email, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FindByEmailAsync(email, cancellationToken);
                if (user == null)
                {
                    return false;
                }

                // Отправляем приветственное письмо повторно
                var success = await _emailService.SendWelcomeEmailAsync(
                    email, 
                    user.Username, 
                    string.Empty, 
                    cancellationToken);
                
                if (success)
                {
                    _logger.LogInformation("Welcome email resent for: {Email}", email);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending confirmation email for: {Email}", email);
                return false;
            }
        }

        public async Task<ValidationResult> ValidateUserUniquenessAsync(
            RegisterRequestDTO request, 
            CancellationToken cancellationToken = default)
        {
            var validationResult = new ValidationResult();
            
            try
            {
                // Проверка уникальности email
                var emailExists = await _userRepository.EmailExistsAsync(request.Email, cancellationToken);
                if (emailExists)
                {
                    validationResult.Errors.Add(new ValidationFailure(
                        nameof(request.Email), 
                        "Email already exists"));
                }

                // Проверка уникальности username
                var usernameExists = await _userRepository.UsernameExistsAsync(request.Username, cancellationToken);
                if (usernameExists)
                {
                    validationResult.Errors.Add(new ValidationFailure(
                        nameof(request.Username), 
                        "Username already exists"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user uniqueness for: {Email}", request.Email);
                validationResult.Errors.Add(new ValidationFailure(
                    string.Empty, 
                    "Database error during validation"));
            }

            return validationResult;
        }
    }
}