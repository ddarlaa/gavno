using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceBreakerApp.Domain.Models;

public class UserSession : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(512)]
    public string RefreshTokenHash { get; set; } = null!;

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
    public virtual User User { get; set; } = null!;
}