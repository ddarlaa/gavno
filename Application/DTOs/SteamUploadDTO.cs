// IceBreakerApp.Application/DTOs/StreamUploadDto.cs
using System.Text.Json.Serialization;

namespace IceBreakerApp.Application.DTOs
{
    public class StreamUploadDto
    {
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class StreamUploadResultDto
    {
        public Guid? Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDuplicate { get; set; }
        public Guid? ExistingFileId { get; set; }
        public string? Message { get; set; }
    }
}