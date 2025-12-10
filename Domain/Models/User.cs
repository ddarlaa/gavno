namespace IceBreakerApp.Domain.Models;

public class User : BaseEntity
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }

    // Навигационные свойства
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuestionAnswer> Answers { get; set; } = new List<QuestionAnswer>();

}