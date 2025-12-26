using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceBreakerApp.Domain.Models;

public class UserClaim : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ClaimType { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string ClaimValue { get; set; } = null!;

    // Навигационные свойства
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}