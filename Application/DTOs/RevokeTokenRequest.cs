using System.ComponentModel.DataAnnotations;

namespace IceBreakerApp.Application.DTOs
{
    public class RevokeTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }
}