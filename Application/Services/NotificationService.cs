using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application.Services
{
    public class FileNotificationService : IFileNotificationService
    {
        private readonly IHubContext<FileNotificationHub> _hubContext;
        private readonly ILogger<FileNotificationService> _logger;

        public FileNotificationService(
            IHubContext<FileNotificationHub> hubContext,
            ILogger<FileNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        #region Upload Notifications

        public async Task NotifyUploadStartedAsync(Guid userId, string fileName, long fileSize)
        {
            try
            {
                _logger.LogInformation(
                    "[NotifyUploadStarted] Sending to userId: {UserId}, File: {FileName}",
                    userId, fileName);
                
                await _hubContext.Clients
                    .Group(userId.ToString())
                    .SendAsync("OnUploadStarted", new
                    {
                        UserId = userId,
                        FileName = fileName,
                        FileSize = fileSize,
                        Status = "started",
                        Timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation("Уведомление о начале загрузки: UserId={UserId}, File={FileName}", 
                    userId, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления о начале загрузки");
            }
        }

        public async Task NotifyUploadCompletedAsync(Guid fileId, Guid userId, string fileName, long fileSize, string fileUrl, string? thumbnailUrl = null)
        {
            try
            {
                await _hubContext.Clients
                    .Group(userId.ToString()).SendAsync("OnUploadCompleted", new
                    {
                        FileId = fileId,
                        UserId = userId,
                        FileName = fileName,
                        FileSize = fileSize,
                        Status = "completed",
                        Timestamp = DateTime.UtcNow,
                        FileUrl = fileUrl,
                        ThumbnailUrl = thumbnailUrl
                    });

                _logger.LogInformation("Уведомление о завершении загрузки: FileId={FileId}, UserId={UserId}", 
                    fileId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления о завершении загрузки");
            }
        }

        public async Task NotifyUploadFailedAsync(Guid userId, string fileName, string error)
        {
            try
            {
                await _hubContext.Clients
                    .Group(userId.ToString()).SendAsync("OnUploadFailed", new
                    {
                        UserId = userId,
                        FileName = fileName,
                        Status = "failed",
                        Error = error,
                        Timestamp = DateTime.UtcNow
                    });

                _logger.LogWarning("Уведомление об ошибке загрузки: UserId={UserId}, File={FileName}", 
                    userId, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления об ошибке загрузки");
            }
        }

        #endregion

        #region Batch Upload Notifications

        public async Task NotifyBatchUploadStartedAsync(Guid userId, int totalFiles, long totalSize)
        {
            try
            {
                await _hubContext.Clients
                    .Group(userId.ToString()).SendAsync("OnBatchUploadStarted", new
                    {
                        UserId = userId,
                        TotalFiles = totalFiles,
                        TotalSize = totalSize,
                        Status = "started",
                        Timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation("Начата пакетная загрузка: UserId={UserId}, Files={TotalFiles}", 
                    userId, totalFiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления о начале пакетной загрузки");
            }
        }

        public async Task NotifyBatchUploadCompletedAsync(Guid userId, int totalFiles, List<FileMetadataDto> uploadedFiles)
        {
            try
            {
                await _hubContext.Clients
                    .Group(userId.ToString()).SendAsync("OnBatchUploadCompleted", new
                    {
                        UserId = userId,
                        TotalFiles = totalFiles,
                        Status = "completed",
                        Timestamp = DateTime.UtcNow,
                        UploadedFiles = uploadedFiles.Select(f => new
                        {
                            f.Id,
                            f.OriginalFileName,
                            f.Size,
                            f.ContentType,
                            Url = $"/api/files/{f.Id}",
                            f.ThumbnailUrl
                        })
                    });

                _logger.LogInformation("Пакетная загрузка завершена: UserId={UserId}, Files={TotalFiles}", 
                    userId, totalFiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления о завершении пакетной загрузки");
            }
        }

        public async Task NotifyBatchUploadFailedAsync(Guid userId, string error, int? successfulFiles = null)
        {
            try
            {
                await _hubContext.Clients
                    .Group(userId.ToString())
                    .SendAsync("OnBatchUploadFailed", new
                    {
                        UserId = userId,
                        Status = "failed",
                        Error = error,
                        SuccessfulFiles = successfulFiles,
                        Timestamp = DateTime.UtcNow
                    });

                _logger.LogWarning("Ошибка пакетной загрузки: UserId={UserId}, Error={Error}", userId, error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления об ошибке пакетной загрузки");
            }
        }

        #endregion

        #region Download Notifications

        public async Task NotifyDownloadStartedAsync(Guid fileId, Guid userId, string fileName, long fileSize)
        {
            try
            {
                await _hubContext.Clients
                    .Group(userId.ToString())
                    .SendAsync("OnDownloadStarted", new
                    {
                        FileId = fileId,
                        UserId = userId,
                        FileName = fileName,
                        FileSize = fileSize,
                        Status = "started",
                        Timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation("Уведомление о начале скачивания: FileId={FileId}, UserId={UserId}", 
                    fileId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления о начале скачивания");
            }
        }

        public async Task NotifyDownloadCompletedAsync(Guid fileId, Guid userId, string fileName, long fileSize)
        {
            try
            {
                await _hubContext.Clients
                    .Group(userId.ToString())
                    .SendAsync("OnDownloadCompleted", new
                    {
                        FileId = fileId,
                        UserId = userId,
                        FileName = fileName,
                        FileSize = fileSize,
                        Status = "completed",
                        Timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation("Уведомление о завершении скачивания: FileId={FileId}, UserId={UserId}", 
                    fileId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления о завершении скачивания");
            }
        }

        public async Task NotifyDownloadFailedAsync(Guid fileId, Guid userId, string fileName, string error)
        {
            try
            {
                await _hubContext.Clients
                    .Group(userId.ToString())
                    .SendAsync("OnDownloadFailed", new
                    {
                        FileId = fileId,
                        UserId = userId,
                        FileName = fileName,
                        Status = "failed",
                        Error = error,
                        Timestamp = DateTime.UtcNow
                    });

                _logger.LogWarning("Уведомление об ошибке скачивания: FileId={FileId}, Error={Error}", 
                    fileId, error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления об ошибке скачивания");
            }
        }

        #endregion
    }
}