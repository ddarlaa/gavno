namespace IceBreakerApp.Application.DTOs.ListItem;

public class TopicListItemDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    // Информация об изображении
    public Guid? ImageId { get; set; }
    public string? ImageThumbnailUrl { get; set; }
    public string? ImageFileUrl { get; set; }
    public long? ImageFileSize { get; set; }
    public string? ImageFileType { get; set; }
}