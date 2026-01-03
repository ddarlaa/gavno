namespace IceBreakerApp.Application.DTOs.Response;

public class QuestionAnswerResponseDTO
{
    public QuestionAnswerResponseDTO()
    {
        Content = string.Empty;
    }

    public QuestionAnswerResponseDTO(string content, Guid userId)
    {
        Content = content;
        UserId = userId;
    }

    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; }
    public bool IsAccepted { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Username { get; set; }
    public int ViewCount { get; set; }
    public bool IsActive { get; set; }
}