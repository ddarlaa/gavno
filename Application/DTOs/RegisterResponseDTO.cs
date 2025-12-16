namespace IceBreakerApp.Application.DTOs
{
    public class RegisterResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ConfirmationToken { get; set; }
        public string? ConfirmationUrl { get; set; }
        public DateTime? TokenExpiresAt { get; set; }
        
        // Данные пользователя (без чувствительной информации)
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? DisplayName { get; set; }
    }
}