using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceBreakerApp.Domain.Models;

public class QuestionLike : BaseEntity
{
    [Required]
    public Guid QuestionId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    // Навигационные свойства
    [ForeignKey(nameof(QuestionId))]
    public Question Question { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}