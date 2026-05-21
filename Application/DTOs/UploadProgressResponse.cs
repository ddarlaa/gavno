using IceBreakerApp.Domain.Models;

namespace IceBreakerApp.Application.DTOs;

public class UploadProgressResponse
{
    public Guid UploadId { get; set; }
    public int UploadedChunks { get; set; }
    public int TotalChunks { get; set; }
    public double Percentage { get; set; }
    public bool IsComplete { get; set; }
    public Guid? FileId { get; set; } // ID созданного файла
    public FileMetadata? File { get; set; } // Полные метаданные (если завершено)
}