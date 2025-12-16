using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceBreakerApp.Domain.Models;

public class UserRole
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid RoleId { get; set; }

    [Required]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public Guid? AssignedBy { get; set; }

    // Навигационные свойства
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;

    [ForeignKey(nameof(AssignedBy))]
    public User? AssignedByUser { get; set; }

    // Составной ключ
    public static bool operator ==(UserRole? left, UserRole? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(UserRole? left, UserRole? right)
    {
        return !Equals(left, right);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not UserRole other)
            return false;
        return UserId == other.UserId && RoleId == other.RoleId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(UserId, RoleId);
    }
}