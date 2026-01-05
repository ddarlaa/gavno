using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceBreakerApp.Domain.Models;

public class UserRole
{
    
    [Key, Column(Order = 0)]
    public Guid UserId { get; set; }

    [Key, Column(Order = 1)]
    public Guid RoleId { get; set; }

    [Required]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public Guid? AssignedBy { get; set; }

    // Навигационные свойства
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey(nameof(AssignedBy))]
    public virtual User? AssignedByUser { get; set; }
}