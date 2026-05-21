namespace IceBreakerApp.Application.DTOs
{
    public class UploadNotificationDto
    {
        public Guid UserId { get; set; }
        public Guid? UploadId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string Status { get; set; } = string.Empty; // started, completed, failed, cancelled
        public int Progress { get; set; }
        public string? Error { get; set; }
        public string? EstimatedTime { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public partial class UploadProgressDto
    {
        public Guid UserId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long BytesUploaded { get; set; }
        public long TotalBytes { get; set; }
        public int Progress { get; set; }
        public string UploadSpeed { get; set; } = string.Empty;
        public string? EstimatedTimeRemaining { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class UploadCompletedDto
    {
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public TimeSpan UploadDuration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class BatchUploadNotificationDto
    {
        public Guid UserId { get; set; }
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public string Status { get; set; } = string.Empty;
        public int FilesCompleted { get; set; }
        public int Progress { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class BatchUploadProgressDto
    {
        public Guid UserId { get; set; }
        public int FilesCompleted { get; set; }
        public int TotalFiles { get; set; }
        public long BytesUploaded { get; set; }
        public long TotalBytes { get; set; }
        public int Progress { get; set; }
        public int CurrentFileProgress { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class BatchUploadCompletedDto
    {
        public Guid UserId { get; set; }
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Progress { get; set; }
        public List<FileMetadataDto> UploadedFiles { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }
}