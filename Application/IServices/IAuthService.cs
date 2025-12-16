using IceBreakerApp.Application.DTOs;

namespace IceBreakerApp.Application.IServices
{
    public interface IAuthService
    {
        Task<RegisterResponseDTO> RegisterAsync(RegisterRequestDTO request, CancellationToken cancellationToken = default);
        Task<bool> ConfirmEmailAsync(string token, CancellationToken cancellationToken = default);
        Task<string> GenerateConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> IsEmailConfirmedAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> ResendConfirmationEmailAsync(string email, CancellationToken cancellationToken = default);
    }
}