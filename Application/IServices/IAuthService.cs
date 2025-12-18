using IceBreakerApp.Application.DTOs;

namespace IceBreakerApp.Application.IServices
{
    public interface IAuthService
    {
        Task<RegisterResponseDTO> RegisterAsync(RegisterRequestDTO request, CancellationToken cancellationToken = default);
        Task<LoginResponseDTO> LoginAsync(LoginDTO loginDto, CancellationToken cancellationToken = default);
        Task<LoginResponseDTO> RefreshTokenAsync(RefreshTokenDTO refreshTokenDto, CancellationToken cancellationToken = default);
        Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task<bool> LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> ConfirmEmailAsync(string token, CancellationToken cancellationToken = default);
        Task<string> GenerateConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> IsEmailConfirmedAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> ResendConfirmationEmailAsync(string email, CancellationToken cancellationToken = default);
    }
}