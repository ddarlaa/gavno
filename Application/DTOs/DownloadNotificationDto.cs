namespace IceBreakerApp.Application.DTOs
{
    public class DownloadNotificationDto
    {
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Status { get; set; } = string.Empty; // started, completed, failed, cancelled
        public int Progress { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DownloadProgressDto
    {
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public int Progress { get; set; }
        public DateTime Timestamp { get; set; }
    }
}