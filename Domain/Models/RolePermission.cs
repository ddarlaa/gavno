using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceBreakerApp.Domain.Models;

public class RolePermission
{
    [Required]
    public Guid RoleId { get; set; }

    [Required]
    public Guid PermissionId { get; set; }

    // Навигационные свойства
    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;

    [ForeignKey(nameof(PermissionId))]
    public Permission Permission { get; set; } = null!;

    // Составной ключ
    public static bool operator ==(RolePermission? left, RolePermission? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(RolePermission? left, RolePermission? right)
    {
        return !Equals(left, right);
    }

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