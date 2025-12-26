// Domain/Models/User.cs (добавь если нет)
using System.ComponentModel.DataAnnotations;

namespace IceBreakerApp.Domain.Models;

public class User : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = null!;

    [Required]
    public string PasswordHash { get; set; } = null!;

    [MaxLength(100)]
    public string? DisplayName { get; set; }

    [MaxLength(1000)]
    public string? Bio { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    public bool IsEmailConfirmed { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Навигационные свойства
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    public virtual ICollection<QuestionAnswer> Answers { get; set; } = new List<QuestionAnswer>();
    public virtual ICollection<QuestionLike> Likes { get; set; } = new List<QuestionLike>();
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public virtual ICollection<UserClaim> UserClaims { get; set; } = new List<UserClaim>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}