using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceBreakerApp.Domain.Models;

public class Topic : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    // Навигационные свойства
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}