using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Application.IServices;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using IceBreakerApp.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IUserRepository _userRepository;
        private readonly IRoleService _roleService;
        private readonly ILogger<JwtService> _logger;

        public JwtService(
            IOptions<JwtSettings> jwtSettings,
            IUserRepository userRepository,
            IRoleService roleService,
            ILogger<JwtService> logger)
        {
            _jwtSettings = jwtSettings.Value;
            _userRepository = userRepository;
            _roleService = roleService;
            _logger = logger;
        }

        public async Task<LoginResponseDTO> GenerateTokensAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                    throw new InvalidOperationException($"User with ID {userId} not found");

                // Генерация access token
                var accessToken = await GenerateAccessTokenAsync(user, cancellationToken);
                var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

                // Генерация refresh token
                var refreshToken = await GenerateRefreshTokenAsync(cancellationToken);
                var refreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
                var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

                // Сохранение refresh token в UserSession
                var session = new UserSession
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RefreshTokenHash = refreshTokenHash,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = refreshTokenExpiresAt,
                    IsRevoked = false
                };

                await _userRepository.AddSessionAsync(session, cancellationToken);

                // Создание UserInfo для возврата
                var userResponse = new LoginResponseDTO.UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    DisplayName = user.DisplayName,
                    IsEmailConfirmed = user.IsEmailConfirmed,
                    CreatedAt = user.CreatedAt
                };

                return new LoginResponseDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    AccessTokenExpiresAt = accessTokenExpiresAt,
                    RefreshTokenExpiresAt = refreshTokenExpiresAt,
                    User = userResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tokens for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed for token: {Token}", token[..Math.Min(token.Length, 50)]);
                return false;
            }
        }

        public async Task<Guid?> GetUserIdFromTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(token);

                var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract user ID from token: {Token}", token[..Math.Min(token.Length, 50)]);
                return null;
            }
        }

        public async Task<Guid?> GetUserIdFromExpiredTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(token);

                var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract user ID from expired token: {Token}", token[..Math.Min(token.Length, 50)]);
                return null;
            }
        }

        public async Task<string> GenerateRefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            // УВЕЛИЧИВАЕМ до 64 байт (512 бит) для безопасности
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return await Task.FromResult(Convert.ToBase64String(randomNumber));
        }

        // Добавляем метод для вычисления хэша refresh token (HMAC-SHA256)
        private string ComputeRefreshTokenHash(string refreshToken)
        {
            // Используем секретный ключ JWT для HMAC
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToBase64String(hashBytes);
        }


        // Исправляем метод отзыва refresh token
        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            try
            {
                // Вычисляем хэш для поиска
                var refreshTokenHash = ComputeRefreshTokenHash(refreshToken);
            
                // Находим и отзываем сессию по хэшу
                var result = await _userRepository.RevokeSessionByHashAsync(refreshTokenHash, cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token: {Token}", refreshToken[..Math.Min(refreshToken.Length, 50)]);
                return false;
            }
        }

        // Исправляем метод валидации refresh token
        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;try
            {
                // Вычисляем хэш для сравнения (HMAC вместо BCrypt)
                var refreshTokenHash = ComputeRefreshTokenHash(refreshToken);
            
                // Находим сессию по хэшу
                var session = await _userRepository.GetActiveSessionByHashAsync(refreshTokenHash, cancellationToken);
                return session != null && session.ExpiresAt > DateTime.UtcNow && !session.IsRevoked;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating refresh token: {Token}", refreshToken[..Math.Min(refreshToken.Length, 50)]);
                return false;
            }
        }


        // Исправляем метод получения UserId из refresh token
        public async Task<string?> GetUserIdFromRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            try
            {
                // Вычисляем хэш для поиска
                var refreshTokenHash = ComputeRefreshTokenHash(refreshToken);
            
                // Находим сессию по хэшу
                var session = await _userRepository.GetActiveSessionByHashAsync(refreshTokenHash, cancellationToken);
            
                return session?.UserId.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ID from refresh token: {Token}", refreshToken[..Math.Min(refreshToken.Length, 50)]);
                return null;
            }
        }

        private async Task<string> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken = default)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

            // Получаем роли пользователя
            var userRoles = await _roleService.GetUserRolesAsync(user.Id, cancellationToken);
            var roleClaims = userRoles.Select(role => new Claim(ClaimTypes.Role, role.Name)).ToList();

            // Получаем claims пользователя
            var userClaims = await _userRepository.GetUserClaimsAsync(user.Id, cancellationToken);
            var customClaims = userClaims.Select(claim => new Claim(claim.ClaimType, claim.ClaimValue)).ToList();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("EmailConfirmed", user.IsEmailConfirmed.ToString()),
            };

            // Добавляем имя и фамилию если есть
            if (!string.IsNullOrEmpty(user.FirstName))
                claims.Add(new Claim("FirstName", user.FirstName));
            if (!string.IsNullOrEmpty(user.LastName))
                claims.Add(new Claim("LastName", user.LastName));
            if (!string.IsNullOrEmpty(user.DisplayName))
                claims.Add(new Claim("DisplayName", user.DisplayName));

            claims.AddRange(roleClaims);
            claims.AddRange(customClaims);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            return tokenString;
        }
    }

    public class JwtSettings
    {
        public string SecretKey { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int AccessTokenExpirationMinutes { get; set; } = 30;
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}