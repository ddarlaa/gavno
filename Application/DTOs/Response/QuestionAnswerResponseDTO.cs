namespace IceBreakerApp.Application.DTOs.Response;

public class QuestionAnswerResponseDto(string content, Guid userId, string username)
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public Guid UserId { get; set; } = userId;
    public string Content { get; set; } = content;
    public bool IsAccepted { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Username { get; set; } = username;
    public int ViewCount { get; set; }
    public bool IsActive { get; set; }
}