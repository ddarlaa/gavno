using System.ComponentModel.DataAnnotations;

namespace IceBreakerApp.Domain.Models;

public class UploadSession
{
    public Guid UploadId { get; set; } // PK, Изменено на Guid
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public int TotalChunks { get; set; }
    public int UploadedChunks { get; set; }
    public Guid? FileId { get; set; } // После сборки
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UploadedChunkIndexes { get; set; } = ""; // Новое поле для отслеживания загруженных чанков

    [Timestamp] // Добавлено для оптимистической блокировки
    public byte[] RowVersion { get; set; } = null!;

    // Navigation
    public FileMetadata? File { get; set; }
    public User User { get; set; } = null!;
}