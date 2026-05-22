namespace IceBreakerApp.Application.DTOs.Create;

public class CreateQuestionAnswerDTO
{
    public Guid QuestionId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = null!;
}