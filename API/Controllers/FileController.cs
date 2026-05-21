using System.Security.Claims;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.Validators;
using IceBreakerApp.Domain.Models;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using ContentDispositionHeaderValue = System.Net.Http.Headers.ContentDispositionHeaderValue;

namespace IceBreakerApp.API.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IChunkedFileService _chunkedFileService;
    private readonly ILogger<FilesController> _logger;
    private readonly StorageSettings _storageSettings;

    public FilesController(IFileService fileService, IChunkedFileService chunkedFileService,
        ILogger<FilesController> logger, IOptions<StorageSettings> storageSettings)
    {
        _fileService = fileService;
        _chunkedFileService = chunkedFileService;
        _logger = logger;
        _storageSettings = storageSettings.Value;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] FileUploadRequest request)
    {
        Guid userId = Guid.Empty;

        try
        {
            // Получаем ID пользователя
            userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Валидация файла
            var validator = new FileUploadValidator();
            var result = await validator.ValidateAsync(request.File);

            if (!result.IsValid)
            {
                return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            }

            // Получаем сервис уведомлений
            var notificationService = HttpContext.RequestServices
                .GetRequiredService<IFileNotificationService>();

            // Отправляем уведомление о начале загрузки
            await notificationService.NotifyUploadStartedAsync(
                userId, request.File.FileName, request.File.Length);

            // Используем вашу существующую систему загрузки
            var metadata = await _fileService.UploadAsync(request.File, userId, request.IsPublic, request.ExpiresAt);

            // Отправляем уведомление о завершении
            var fileUrl = $"/api/files/{metadata.Id}";
            var thumbnailUrl = IsImage(metadata.ContentType)
                ? $"/api/files/{metadata.Id}/thumbnail?size=small"
                : null;

            await notificationService.NotifyUploadCompletedAsync(
                metadata.Id, userId, metadata.OriginalFileName, metadata.Size, fileUrl, thumbnailUrl);

            
            var dto = new FileMetadataDto
            {
                Id = metadata.Id,
                OriginalFileName = metadata.OriginalFileName,
                Size = metadata.Size,
                ContentType = metadata.ContentType,
                UploadedAt = metadata.UploadedAt,
                Url = fileUrl,
                ThumbnailUrl = thumbnailUrl,
                Width = metadata.Width,
                Height = metadata.Height,
                FileType = GetFileType(metadata.ContentType),
                IsPublic = metadata.IsPublic,
                ExpiresAt = metadata.ExpiresAt
            };
            // Возвращаем результат
            return Created(fileUrl, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при загрузке файла пользователем {userId}");

            // Отправляем уведомление об ошибке
            if (userId != Guid.Empty)
            {
                var notificationService = HttpContext.RequestServices
                    .GetRequiredService<IFileNotificationService>();
                await notificationService.NotifyUploadFailedAsync(
                    userId, request?.File?.FileName ?? "Unknown file", ex.Message);
            }

            return StatusCode(500, "Внутренняя ошибка сервера при загрузке файла");
        }
    }

    [HttpPost("upload-multiple")]
    public async Task<IActionResult> UploadMultiple([FromForm] MultipleFileUploadRequest request)
    {
        Guid userId = Guid.Empty;
        List<FileMetadataDto> uploadedFiles = new List<FileMetadataDto>();

        try
        {
            userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var validator = new MultipleFileUploadValidator();
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var notificationService = HttpContext.RequestServices
                .GetRequiredService<IFileNotificationService>();

            // Уведомление о начале пакетной загрузки
            var totalSize = request.Files.Sum(f => f.Length);
            await notificationService.NotifyBatchUploadStartedAsync(
                userId, request.Files.Count, totalSize);

            // Используем вашу существующую систему загрузки
            var metadataList = await _fileService.UploadMultipleAsync(
                request.Files, userId, request.IsPublic, request.ExpiresAt);

            // Преобразуем в DTO и отправляем уведомления о каждом файле
            foreach (var metadata in metadataList)
            {
                var fileUrl = $"/api/files/{metadata.Id}";
                var thumbnailUrl = IsImage(metadata.ContentType)
                    ? $"/api/files/{metadata.Id}/thumbnail?size=small"
                    : null;

                await notificationService.NotifyUploadCompletedAsync(
                    metadata.Id, userId, metadata.OriginalFileName, metadata.Size, fileUrl, thumbnailUrl);

                uploadedFiles.Add(new FileMetadataDto
                {
                    Id = metadata.Id,
                    OriginalFileName = metadata.OriginalFileName,
                    Size = metadata.Size,
                    ContentType = metadata.ContentType,
                    UploadedAt = metadata.UploadedAt,
                    Url = fileUrl,
                    ThumbnailUrl = thumbnailUrl,
                    Width = metadata.Width,
                    Height = metadata.Height,
                    FileType = GetFileType(metadata.ContentType),
                    IsPublic = metadata.IsPublic,
                    ExpiresAt = metadata.ExpiresAt
                });
            }

            // Уведомление о завершении пакетной загрузки
            await notificationService.NotifyBatchUploadCompletedAsync(
                userId, request.Files.Count, uploadedFiles);

            return Ok(uploadedFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при пакетной загрузке пользователем {userId}");

            if (userId != Guid.Empty)
            {
                var notificationService = HttpContext.RequestServices
                    .GetRequiredService<IFileNotificationService>();
                await notificationService.NotifyBatchUploadFailedAsync(
                    userId, ex.Message, uploadedFiles.Count);
            }

            return StatusCode(500, "Внутренняя ошибка сервера при пакетной загрузке");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles([FromQuery] FileFilterDto filter)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserId = userIdString != null ? Guid.Parse(userIdString) : Guid.Empty;
        var isAdmin = User.IsInRole("Admin");

        var paginatedResult = await _fileService.GetFilesAsync(
            currentUserId,
            isAdmin,
            filter.PageNumber,
            filter.PageSize,
            filter.ContentType,
            filter.Search,
            filter.SortBy,
            filter.SortOrder);

        var dtos = paginatedResult.Items.Select(metadata => new FileMetadataDto
        {
            Id = metadata.Id,
            OriginalFileName = metadata.OriginalFileName,
            Size = metadata.Size,
            ContentType = metadata.ContentType,
            UploadedAt = metadata.UploadedAt,
            Url = $"/api/files/{metadata.Id}",
            ThumbnailUrl = IsImage(metadata.ContentType) ? $"/api/files/{metadata.Id}/thumbnail?size=small" : null,
            Width = metadata.Width,
            Height = metadata.Height,
            FileType = GetFileType(metadata.ContentType),
            IsPublic = metadata.IsPublic,
            ExpiresAt = metadata.ExpiresAt
        }).ToList();

        return Ok(new PaginatedResult<FileMetadataDto>(dtos, paginatedResult.TotalCount, paginatedResult.PageNumber,
            paginatedResult.PageSize));
    }
    [HttpPost("test-notify")]
    public async Task<IActionResult> TestNotify()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notificationService = HttpContext.RequestServices
            .GetRequiredService<IFileNotificationService>();
    
        await notificationService.NotifyUploadStartedAsync(
            userId, "test-file.png", 1024);
    
        await notificationService.NotifyUploadCompletedAsync(
            Guid.NewGuid(), userId, "test-file.png", 1024, "/api/files/test");
    
        return Ok("Test notifications sent to userId: " + userId);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [Produces("application/octet-stream", "application/pdf", "image/jpeg", "image/png")]
    public async Task<IActionResult> Download(Guid id)
    {
        Guid currentUserId = Guid.Empty;
        FileMetadata metadata = null;

        try
        {
            // Получаем идентификатор пользователя
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            currentUserId = userId != null ? Guid.Parse(userId) : Guid.Empty;

            // Получаем метаданные файла
            metadata = await _fileService.GetAsync(id);

            // Проверка срока действия
            if (metadata.ExpiresAt.HasValue && metadata.ExpiresAt.Value < DateTime.UtcNow)
            {
                return NotFound("Файл истек или не найден.");
            }

            // Проверка прав доступа
            if (!metadata.IsPublic && metadata.UploadedById != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid("У вас нет прав для скачивания этого файла.");
            }

            // Создаем сервис уведомлений
            var notificationService = HttpContext.RequestServices
                .GetRequiredService<IFileNotificationService>();

            // Отправляем уведомление о начале скачивания
            await notificationService.NotifyDownloadStartedAsync(
                id, currentUserId, metadata.OriginalFileName, metadata.Size);

            // Увеличиваем счетчик скачиваний
            await _fileService.IncrementDownloadCountAsync(id);

            // Получаем поток файла
            var stream = await _fileService.GetFileStreamAsync(id);

            // Определяем тип контента
            string contentType = metadata.ContentType;
            var extension = Path.GetExtension(metadata.OriginalFileName)?.ToLowerInvariant();

            if (extension == ".pdf") contentType = "application/pdf";
            else if (extension == ".jpg" || extension == ".jpeg") contentType = "image/jpeg";
            else if (extension == ".png") contentType = "image/png";

            // Настраиваем заголовки для скачивания
            var contentDisposition = new ContentDispositionHeaderValue("attachment");
            contentDisposition.FileName = metadata.OriginalFileName;
            contentDisposition.FileNameStar = metadata.OriginalFileName;

            Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
            Response.Headers[HeaderNames.ContentType] = contentType;

            // Отправляем файл
            return File(stream, contentType, enableRangeProcessing: true);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, $"Файл с ID {id} не найден.");

            // Отправляем уведомление об ошибке
            if (metadata != null && currentUserId != Guid.Empty)
            {
                var notificationService = HttpContext.RequestServices
                    .GetRequiredService<IFileNotificationService>();
                await notificationService.NotifyDownloadFailedAsync(
                    id, currentUserId, metadata.OriginalFileName, "Файл не найден");
            }

            return NotFound("Файл не найден.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при скачивании файла с ID {id}.");

            // Отправляем уведомление об ошибке
            if (metadata != null && currentUserId != Guid.Empty)
            {
                var notificationService = HttpContext.RequestServices
                    .GetRequiredService<IFileNotificationService>();
                await notificationService.NotifyDownloadFailedAsync(
                    id, currentUserId, metadata.OriginalFileName, ex.Message);
            }

            return StatusCode(500, "Внутренняя ошибка сервера.");
        }
        finally
        {
            // После успешного скачивания отправляем уведомление о завершении
            if (metadata != null && currentUserId != Guid.Empty && Response.StatusCode == 200)
            {
                try
                {
                    var notificationService = HttpContext.RequestServices
                        .GetRequiredService<IFileNotificationService>();
                    await notificationService.NotifyDownloadCompletedAsync(
                        id, currentUserId, metadata.OriginalFileName, metadata.Size);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при отправке финального уведомления о скачивании");
                }
            }
        }
    }

    private bool IsInlineDisplayable(string contentType)
    {
        // Открываем в браузере картинки и PDF
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
               contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Потоковая отдача файла с поддержкой Range requests
    /// </summary>
    /// <param name="id">Идентификатор файла</param>
    [HttpGet("{id}/stream")]
    public async Task<IActionResult> Stream(Guid id)
    {
        try
        {
            var metadata = await _fileService.GetAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserId = userId != null ? Guid.Parse(userId) : (Guid?)null;

            // Проверка срока действия
            if (metadata.ExpiresAt.HasValue && metadata.ExpiresAt.Value < DateTime.UtcNow)
            {
                return NotFound("Файл истек или не найден.");
            }

            // Проверка прав доступа
            if (!metadata.IsPublic && metadata.UploadedById != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid("У вас нет прав для стриминга этого файла.");
            }

            var stream = await _fileService.GetFileStreamAsync(id);

            return File(stream, metadata.ContentType, enableRangeProcessing: true);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("истёк"))
        {
            _logger.LogWarning("Срок действия файла {FileId} истёк", id);
            return StatusCode(StatusCodes.Status410Gone,
                new { error = "Срок действия файла истёк" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при стриминге файла {FileId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Внутренняя ошибка сервера" });
        }
    }


    [HttpPost("upload-stream")]
    [Authorize]
    [RequestSizeLimit(2_000_000_000)] // 2GB
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadStream(
        IFormFile file,
        [FromQuery] bool isPublic = false,
        [FromQuery] DateTime? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();

            _logger.LogInformation("Начало стриминговой загрузки файла пользователем {UserId}", userId);

            // Используем новый метод для больших файлов
            var result = await _fileService.UploadLargeFileAsync(
                file, userId, isPublic, expiresAt, cancellationToken);

            _logger.LogInformation("Стриминговая загрузка завершена: {FileId}", result.Id);

            return CreatedAtAction(
                nameof(GetFileInfo),
                new { id = result.Id },
                new
                {
                    id = result.Id,
                    originalFileName = result.OriginalFileName,
                    size = result.Size,
                    contentType = result.ContentType,
                    url = $"/api/Files/{result.Id}",
                    thumbnailUrl = result.ContentType.StartsWith("image/")
                        ? $"/api/Files/{result.Id}/thumbnail?size=small"
                        : null,
                    isPublic = result.IsPublic,
                    expiresAt = result.ExpiresAt,
                });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Загрузка отменена пользователем");
            return StatusCode(499);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Ошибка валидации файла");
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Ошибка доступа");
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при стриминговой загрузке файла");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Внутренняя ошибка сервера" });
        }
    }


    [HttpGet("{id}/info")]
    public async Task<IActionResult> GetFileInfo(Guid id)
    {
        try
        {
            var metadata = await _fileService.GetAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserId = userId != null ? Guid.Parse(userId) : (Guid?)null;

            // Проверка срока действия
            if (metadata.ExpiresAt.HasValue && metadata.ExpiresAt.Value < DateTime.UtcNow)
            {
                return NotFound("Файл истек или не найден.");
            }

            // Проверка прав доступа
            if (!metadata.IsPublic && metadata.UploadedById != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid("У вас нет прав для просмотра информации об этом файле.");
            }

            return Ok(new FileMetadataDto
            {
                Id = metadata.Id,
                OriginalFileName = metadata.OriginalFileName,
                Size = metadata.Size,
                ContentType = metadata.ContentType,
                UploadedAt = metadata.UploadedAt,
                Url = $"/api/files/{metadata.Id}",
                ThumbnailUrl = IsImage(metadata.ContentType) ? $"/api/files/{metadata.Id}/thumbnail?size=small" : null,
                Width = metadata.Width,
                Height = metadata.Height,
                FileType = GetFileType(metadata.ContentType),
                IsPublic = metadata.IsPublic,
                ExpiresAt = metadata.ExpiresAt
            });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, $"Файл с ID {id} не найден.");
            return NotFound("Файл не найден.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при получении информации о файле с ID {id}. посредством GetFileInfo");
            return StatusCode(500, "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet("{id}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(Guid id, [FromQuery] string size = "small")
    {
        try
        {
            var metadata = await _fileService.GetAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserId = userId != null ? Guid.Parse(userId) : (Guid?)null;

            // Проверка срока действия
            if (metadata.ExpiresAt.HasValue && metadata.ExpiresAt.Value < DateTime.UtcNow)
            {
                return NotFound("Файл истек или не найден.");
            }

            // Проверка прав доступа
            if (!metadata.IsPublic && metadata.UploadedById != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid("У вас нет прав для просмотра миниатюры этого файла.");
            }

            var thumbPath = await _fileService.GetThumbnailPathAsync(id, size);
            var mimeType = "image/jpeg"; // Миниатюры всегда JPEG

            _logger.LogInformation($"[DEBUG] ID: {id}. Trying to serve thumbnail from path: '{thumbPath}'");

            if (!System.IO.File.Exists(thumbPath))
            {
                _logger.LogError($"[ERROR] Thumbnail file NOT FOUND at: '{thumbPath}'");
                return NotFound("Файл миниатюры физически отсутствует на диске.");
            }

            // Добавляем Cache-Control и ETag заголовки
            Response.Headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(365) // Кэшировать на 1 год
            }.ToString();
            Response.Headers.ETag = new Microsoft.Net.Http.Headers.EntityTagHeaderValue(
                $"\"{metadata.Hash}_{size}\"", true).ToString(); // ETag на основе хеша файла и размера миниатюры

            return PhysicalFile(thumbPath, mimeType);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, $"Миниатюра для файла с ID {id} не найдена.");
            return NotFound("Миниатюра не найдена.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, $"Попытка получить миниатюру для неизображения: {id}.");
            return BadRequest("Миниатюры доступны только для изображений.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, $"Неверный размер миниатюры для файла {id}.");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при получении миниатюры для файла с ID {id}. посредством GetThumbnail");
            return StatusCode(500, "Внутренняя ошибка сервера.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserId = userId != null ? Guid.Parse(userId) : (Guid?)null;
            var isAdmin = User.IsInRole("Admin");

            if (!currentUserId.HasValue)
            {
                return Unauthorized("Пользователь не авторизован.");
            }

            await _fileService.DeleteAsync(id, currentUserId.Value, isAdmin);
            return NoContent(); // 204 No Content
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, $"Файл с ID {id} не найден для удаления.");
            return NotFound("Файл не найден.");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, $"Пользователь {User.Identity?.Name} попытался удалить файл {id} без прав.");
            return Forbid("У вас нет прав для удаления этого файла.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при удалении файла с ID {id}. посредством DeleteFile");
            return StatusCode(500, "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("upload/chunked")]
    public async Task<IActionResult> UploadChunk([FromForm] ChunkUploadRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _chunkedFileService.UploadChunkAsync(request, userId);
        return Ok(response);
    }

    [HttpGet("upload/{uploadId}/progress")]
    public async Task<IActionResult> GetUploadProgress(Guid uploadId)
    {
        try
        {
            var progress = await _chunkedFileService.GetProgressAsync(uploadId);
            return Ok(progress);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, $"Upload session {uploadId} not found.");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting upload progress for session {uploadId}.");
            return StatusCode(500, "Internal server error.");
        }
    }

    private string GetFileType(string contentType)
    {
        if (contentType.StartsWith("image/")) return "image";
        if (contentType.StartsWith("application/pdf") || contentType.Contains("document") ||
            contentType.Contains("sheet")) return "document";
        return "other";
    }

    private bool IsImage(string contentType) => contentType.StartsWith("image/");


    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

// Вспомогательный класс для отслеживания прогресса скачивания
public class ProgressStream : Stream
{
    private readonly Stream _innerStream;
    private readonly long _totalBytes;
    private readonly Func<long, long, Task> _progressCallback;
    private long _bytesRead;

    public ProgressStream(Stream innerStream, long totalBytes, Func<long, long, Task> progressCallback)
    {
        _innerStream = innerStream;
        _totalBytes = totalBytes;
        _progressCallback = progressCallback;
        _bytesRead = 0;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        if (bytesRead > 0)
        {
            _bytesRead += bytesRead;
            await _progressCallback(_bytesRead, _totalBytes);
        }

        return bytesRead;
    }

    // Реализация остальных методов Stream
    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override void Flush() => _innerStream.Flush();
    public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
    public override void SetLength(long value) => _innerStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
}