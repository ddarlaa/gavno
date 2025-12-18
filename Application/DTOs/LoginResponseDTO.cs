namespace IceBreakerApp.Application.DTOs
{
    public class LoginResponseDTO
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
        
        // Минимальная структура пользователя для JWT
        public class UserInfo
        {
            public Guid Id { get; set; }
            public string Username { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? DisplayName { get; set; }
            public bool IsEmailConfirmed { get; set; }
            public DateTime CreatedAt { get; set; }
        }
        
        public UserInfo? User { get; set; }
    }
}