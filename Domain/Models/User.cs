using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceBreakerApp.Domain.Models;

public class User : BaseEntity
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = null!;

    [Required]
    [MaxLength(512)]
    public string PasswordHash { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string PasswordSalt { get; set; } = null!;

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(255)]
    public string? DisplayName { get; set; }

    [MaxLength(1000)]
    public string? Bio { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public DateTime? LastLoginAt { get; set; }

    [Required]
    public bool IsEmailConfirmed { get; set; } = false;

    [Required]
    public bool IsDeleted { get; set; } = false;

    // // Навигационные свойства
    // public ICollection<Question> Questions { get; set; } = new List<Question>();
    // public ICollection<QuestionAnswer> Answers { get; set; } = new List<QuestionAnswer>();
    // public ICollection<QuestionLike> Likes { get; set; } = new List<QuestionLike>();
    
    // Навигационные свойства для системы авторизации
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserClaim> UserClaims { get; set; } = new List<UserClaim>();
}