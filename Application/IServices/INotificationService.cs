using IceBreakerApp.Application.DTOs;

namespace IceBreakerApp.Application.IServices
{
    public interface IFileNotificationService
    {
        // Уведомления о загрузке (Upload)
        Task NotifyUploadStartedAsync(Guid userId, string fileName, long fileSize);
        Task NotifyUploadCompletedAsync(Guid fileId, Guid userId, string fileName, long fileSize, string fileUrl, string? thumbnailUrl = null);
        Task NotifyUploadFailedAsync(Guid userId, string fileName, string error);
        
        // Уведомления о пакетной загрузке
        Task NotifyBatchUploadStartedAsync(Guid userId, int totalFiles, long totalSize);
        Task NotifyBatchUploadCompletedAsync(Guid userId, int totalFiles, List<FileMetadataDto> uploadedFiles);
        Task NotifyBatchUploadFailedAsync(Guid userId, string error, int? successfulFiles = null);
        
        // Уведомления о скачивании
        Task NotifyDownloadStartedAsync(Guid fileId, Guid userId, string fileName, long fileSize);
        Task NotifyDownloadCompletedAsync(Guid fileId, Guid userId, string fileName, long fileSize);
        Task NotifyDownloadFailedAsync(Guid fileId, Guid userId, string fileName, string error);
    }
}