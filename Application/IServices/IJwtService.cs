using IceBreakerApp.Application.DTOs;

namespace IceBreakerApp.Application.IServices
{
    public interface IJwtService
    {
        Task<LoginResponseDTO> GenerateTokensAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<Guid?> GetUserIdFromTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<Guid?> GetUserIdFromExpiredTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<string> GenerateRefreshTokenAsync(CancellationToken cancellationToken = default);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task<string?> GetUserIdFromRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    }
}