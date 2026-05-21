namespace IceBreakerApp.Application.DTOs.Create;

public class CreateTopicDTO
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ImageId { get; set; }
}