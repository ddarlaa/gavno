namespace IceBreakerApp.Domain.Models;

public class UploadSession
{
    public string UploadId { get; set; } = null!; // PK
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public int TotalChunks { get; set; }
    public int UploadedChunks { get; set; }
    public Guid? FileId { get; set; } // После сборки
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivity { get; set; }

    // Navigation
    public FileMetadata? File { get; set; }
    public User User { get; set; } = null!;
}