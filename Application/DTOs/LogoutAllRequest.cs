using System.ComponentModel.DataAnnotations;

namespace IceBreakerApp.Application.DTOs
{
    public class LogoutAllRequest
    {
        [Required]
        public Guid UserId { get; set; }
    }
}