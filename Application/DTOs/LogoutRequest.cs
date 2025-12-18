using System.ComponentModel.DataAnnotations;

namespace IceBreakerApp.Application.DTOs
{
    public class LogoutRequest
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }
}