using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceBreakerApp.Domain.Models;

public class RolePermission
{
 
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public Guid RoleId { get; set; }

    [Required]
    public Guid PermissionId { get; set; }

    // Навигационные свойства
    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;

    [ForeignKey(nameof(PermissionId))]
    public Permission Permission { get; set; } = null!;

    // Уникальный индекс для предотвращения дубликатов
    public override bool Equals(object? obj)
    {
        if (obj is not RolePermission other)
            return false;
        return RoleId == other.RoleId && PermissionId == other.PermissionId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RoleId, PermissionId);
    }
}