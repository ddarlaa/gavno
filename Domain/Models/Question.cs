using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceBreakerApp.Domain.Models;

public class Question : BaseEntity
{
    [Required] [MaxLength(500)] public string Title { get; set; } = null!;

    [Required] public string Content { get; set; } = null!;

    [Required] public Guid UserId { get; set; }

    [Required] public Guid TopicId { get; set; }

    public int ViewCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;
    public int AnswerCount { get; set; } = 0;

    [Required] public bool IsActive { get; set; } = true;

    // Навигационные свойства
    [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

    [ForeignKey(nameof(TopicId))] public Topic Topic { get; set; } = null!;

    public IEnumerable<QuestionAnswer>? Answers { get; set; } = new List<QuestionAnswer>();
    public IEnumerable<QuestionLike>? Likes { get; set; } = new List<QuestionLike>();

    public void Delete()
    {
        throw new NotImplementedException();
    }
}