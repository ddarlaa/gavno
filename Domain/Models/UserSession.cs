using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceBreakerApp.Domain.Models;

public class UserSession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(512)]
    public string RefreshTokenHash { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ExpiresAt { get; set; }

    [MaxLength(500)]
    public string? DeviceInfo { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [Required]
    public bool IsRevoked { get; set; } = false;

    // Навигационные свойства
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}