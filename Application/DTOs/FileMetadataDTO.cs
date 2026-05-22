using Microsoft.AspNetCore.Http;

namespace IceBreakerApp.Application.DTOs;

public class FileUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public bool IsPublic { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
}

public class MultipleFileUploadRequest
{
    public List<IFormFile> Files { get; set; } = null!;
    public bool IsPublic { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
}
public class FileMetadataDto
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public long Size { get; set; }
    public string ContentType { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
    public string? Url { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string FileType { get; set; } = null!;
    public bool IsPublic { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

