using IceBreakerApp.Domain.Models;

namespace IceBreakerApp.Domain;
public class FileMetadata
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;           // 8f3d2e...jpg
    public string OriginalFileName { get; set; } = null!;   // myphoto.jpg
    public string ContentType { get; set; } = null!;        // image/jpeg
    public long Size { get; set; }                          // in bytes
    public Guid UploadedById { get; set; }                  // FK to User
    public DateTime UploadedAt { get; set; }
    public string Path { get; set; } = null!;               // 2025/04/18/
    public string Hash { get; set; } = null!;               // SHA256
    public bool IsPublic { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int DownloadCount { get; set; }
    public bool IsDeleted { get; set; }

    // Для изображений
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? CameraModel { get; set; }
    public DateTime? DateTaken { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Orientation { get; set; } // EXIF Orientation

    // Paths to generated thumbnails
    public string? SmallThumbnailPath { get; set; }
    public string? MediumThumbnailPath { get; set; }

    public bool IsAvatar { get; set; } = false; // Добавлено: для идентификации файлов-аватаров

    // Navigation
    public User UploadedBy { get; set; } = null!;
}
