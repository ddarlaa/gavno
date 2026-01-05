namespace IceBreakerApp.Application.DTOs.Response;

public class QuestionResponseDTO
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public Guid TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int AnswerCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Информация об изображении
    public Guid? ImageId { get; set; }
    public string? ImageThumbnailUrl { get; set; }
    public string? ImageFileUrl { get; set; }
    public long? ImageFileSize { get; set; }
    public string? ImageFileType { get; set; }
}