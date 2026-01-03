using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceBreakerApp.Domain.Models;

public class QuestionAnswer : BaseEntity
{
    [Required]
    public string Content { get; set; } = null!;

    [Required]
    public Guid QuestionId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public int ViewCount { get; set; } = 0;

    [Required]
    public bool IsAccepted { get; set; } = false;

    [Required]
    public bool IsActive { get; set; } = true;

    // Навигационные свойства
    [ForeignKey(nameof(QuestionId))]
    public virtual Question Question { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}