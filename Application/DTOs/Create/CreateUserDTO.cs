using System.ComponentModel.DataAnnotations;

namespace IceBreakerApp.Application.DTOs.Create
{
    public class CreateUserDTO
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = null!;

        [StringLength(100)]
        public string? DisplayName { get; set; }

        [StringLength(1000)]
        public string? Bio { get; set; }
    }
}