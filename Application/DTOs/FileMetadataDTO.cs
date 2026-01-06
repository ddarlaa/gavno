using Microsoft.AspNetCore.Http;

namespace IceBreakerApp.Application.DTOs;

public class FileUploadRequest
{
    public IFormFile File { get; set; }
    public bool IsPublic { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
}

public class MultipleFileUploadRequest
{
    public List<IFormFile> Files { get; set; }
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
    public int DownloadCount { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string FileType { get; set; } = null!;
    public bool IsPublic { get; set; }
    public DateTime? ExpiresAt { get; set; }
}



public class FilesQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string ContentTypeFilter { get; set; } // "image", "document", или конкретный MIME
    public string Search { get; set; }
    public string SortBy { get; set; } = "UploadedAt"; // UploadedAt, Size, OriginalFileName
    public bool SortDescending { get; set; } = true;
}


public class UploadProgressDto
{
    public Guid UploadId { get; set; }
    public int UploadedChunks { get; set; }
    public int TotalChunks { get; set; }
    public double Percentage { get; set; }
}

// Для задания 3
public enum ThumbnailSize
{
    Small = 0, // 200x200
    Medium = 1 // 800x600
}