using System.ComponentModel.DataAnnotations;

namespace IceBreakerApp.Application.DTOs
{
    public class LoginDTO
    {
        [Required]
        [StringLength(255)]
        public string EmailOrUsername { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
    }
}