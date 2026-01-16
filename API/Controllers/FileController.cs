using System.Security.Claims;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using IceBreakerApp.Domain;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using ContentDispositionHeaderValue = System.Net.Http.Headers.ContentDispositionHeaderValue;
using FileUploadRequest = IceBreakerApp.Application.DTOs.FileUploadRequest;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;
using Microsoft.Net.Http.Headers; // Для HeaderNames и ContentDispositionHeaderValue




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
        var validator = new FileUploadValidator();
        var result = await validator.ValidateAsync(request.File);

        if (!result.IsValid)
        {
            return BadRequest(result.Errors.Select(e => e.ErrorMessage));
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var metadata = await _fileService.UploadAsync(request.File, userId, request.IsPublic, request.ExpiresAt);
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

    [HttpPost("upload-multiple")]
    public async Task<IActionResult> UploadMultiple([FromForm] MultipleFileUploadRequest request)
    {
        var validator = new MultipleFileUploadValidator();
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var metadataList =
            await _fileService.UploadMultipleAsync(request.Files, userId, request.IsPublic, request.ExpiresAt);

        var dtos = metadataList.Select(metadata => new FileMetadataDto
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

        return Ok(dtos);
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
    

   [HttpGet("{id}")]
    // ЭТИ АТРИБУТЫ ВАЖНЫ ДЛЯ SWAGGER:
    // Говорит Swagger-у, что метод возвращает поток файла
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    // Говорит Swagger-у, что возможны бинарные типы (чтобы он не пытался парсить их как JSON/Текст)
    [Produces("application/octet-stream", "application/pdf", "image/jpeg", "image/png")] 
    public async Task<IActionResult> Download(Guid id)
    {
        try
        {
            var metadata = await _fileService.GetAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserId = userId != null ? Guid.Parse(userId) : (Guid?)null;

            if (metadata.ExpiresAt.HasValue && metadata.ExpiresAt.Value < DateTime.UtcNow)
            {
                return NotFound("Файл истек или не найден.");
            }

            if (!metadata.IsPublic && metadata.UploadedById != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid("У вас нет прав для скачивания этого файла.");
            }

            await _fileService.IncrementDownloadCountAsync(id);
            var stream = await _fileService.GetFileStreamAsync(id);

            // ОПРЕДЕЛЯЕМ ТИП КОНТЕНТА
            string contentType = metadata.ContentType;
            var extension = Path.GetExtension(metadata.OriginalFileName)?.ToLowerInvariant();

            if (extension == ".pdf") contentType = "application/pdf";
            else if (extension == ".jpg" || extension == ".jpeg") contentType = "image/jpeg";
            else if (extension == ".png") contentType = "image/png";

            // ВАЖНО: Если вы хотите, чтобы файл ВСЕГДА скачивался (даже в браузере),
            // а не открывался для просмотра, используйте "attachment".
            // Если хотите предпросмотр в браузере, но кнопку в Swagger — используйте код ниже с атрибутами.
            
            // Чтобы гарантированно была кнопка "Скачать" и никакого предпросмотра нигде:
            var contentDisposition = new ContentDispositionHeaderValue("attachment");
            
            // Если все же хотите открывать PDF в браузере, но в Swagger скачивать, 
            // оставьте "inline", но атрибуты [Produces...] сверху решат проблему отображения текста в Swagger.
            // var isInline = IsInlineDisplayable(contentType);
            // var contentDisposition = new ContentDispositionHeaderValue(isInline ? "inline" : "attachment");

            contentDisposition.FileName = metadata.OriginalFileName; 
            contentDisposition.FileNameStar = metadata.OriginalFileName;

            Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
            Response.Headers[HeaderNames.ContentType] = contentType;

            return File(stream, contentType, enableRangeProcessing: true);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, $"Файл с ID {id} не найден.");
            return NotFound("Файл не найден.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при скачивании файла с ID {id}.");
            return StatusCode(500, "Внутренняя ошибка сервера.");
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

    //
    // /// <summary>
    //     /// Скачать файл
    //     /// </summary>
    //     /// <param name="id">Идентификатор файла</param>
    //     [HttpGet("{id}")]
    //     [AllowAnonymous]
    //     [ProducesResponseType(StatusCodes.Status200OK)]
    //     [ProducesResponseType(StatusCodes.Status404NotFound)]
    //     [ProducesResponseType(StatusCodes.Status403Forbidden)]
    //     [ProducesResponseType(StatusCodes.Status410Gone)]
    //     public async Task<IActionResult> DownloadFile(Guid id)
    //     {
    //         try
    //         {
    //             _logger.LogInformation("Запрос на скачивание файла {FileId}", id);
    //
    //             // Получаем текущего пользователя (может быть null для анонимных)
    //             var currentUserId = GetCurrentUserId();
    //             Guid? userId = currentUserId != Guid.Empty ? currentUserId : null;
    //
    //             // Проверяем права доступа и получаем метаданные
    //             var fileInfo = await _fileService.GetFileMetadataAsync(id, userId);
    //             if (fileInfo == null)
    //             {
    //                 _logger.LogWarning("Файл {FileId} не найден", id);
    //                 return NotFound(new { error = "Файл не найден" });
    //             }
    //
    //             // Проверяем права доступа (IsPublic, владелец, админ)
    //             var isOwner = fileInfo.UploadedById == currentUserId;
    //             var isAdmin = User.IsInRole("Admin");
    //             var isPublic = fileInfo.IsPublic;
    //
    //             if (!isPublic && !isOwner && !isAdmin)
    //             {
    //                 _logger.LogWarning("Доступ к файлу {FileId} запрещен для пользователя {UserId}",
    //                     id, currentUserId);
    //                 return StatusCode(StatusCodes.Status403Forbidden,
    //                     new { error = "Доступ запрещен" });
    //             }
    //
    //             // Проверяем срок действия
    //             if (fileInfo.ExpiresAt.HasValue && fileInfo.ExpiresAt.Value < DateTime.UtcNow)
    //             {
    //                 _logger.LogWarning("Срок действия файла {FileId} истёк", id);
    //                 return StatusCode(StatusCodes.Status410Gone,
    //                     new { error = "Срок действия файла истёк" });
    //             }
    //
    //             // Скачиваем файл (метод уже увеличивает DownloadCount)
    //             var (stream, contentType, fileName) =
    //                 await _fileService.DownloadFileAsync(id, userId);
    //
    //             _logger.LogInformation("Файл {FileId} скачивается, ContentType: {ContentType}",
    //                 id, contentType);
    //
    //             // Определяем Content-Disposition
    //             var isImage = contentType.StartsWith("image/");
    //             var isPdf = contentType == "application/pdf";
    //             var contentDisposition = isImage || isPdf ? "inline" : "attachment";
    //
    //             // Устанавливаем заголовки
    //             Response.Headers.Append("Content-Disposition",
    //                 $"{contentDisposition}; filename=\"{fileName}\"");
    //             Response.Headers.Append("X-File-Id", id.ToString());
    //             Response.Headers.Append("X-File-Name", fileName);
    //
    //             return File(stream, contentType);
    //         }
    //         catch (FileNotFoundException)
    //         {
    //             _logger.LogWarning("Файл {FileId} не найден при скачивании", id);
    //             return NotFound(new { error = "Файл не найден" });
    //         }
    //         catch (UnauthorizedAccessException)
    //         {
    //             _logger.LogWarning("Доступ к файлу {FileId} запрещен", id);
    //             return StatusCode(StatusCodes.Status403Forbidden,
    //                 new { error = "Доступ запрещен" });
    //         }
    //         catch (InvalidOperationException ex) when (ex.Message.Contains("истёк"))
    //         {
    //             _logger.LogWarning("Срок действия файла {FileId} истёк", id);
    //             return StatusCode(StatusCodes.Status410Gone,
    //                 new { error = "Срок действия файла истёк" });
    //         }
    //         catch (Exception ex)
    //         {
    //             _logger.LogError(ex, "Ошибка при скачивании файла {FileId}", id);
    //             return StatusCode(StatusCodes.Status500InternalServerError,
    //                 new { error = "Внутренняя ошибка сервера" });
    //         }
    //     }

    

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
            Response.Headers["Cache-Control"] = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(365) // Кэшировать на 1 год
            }.ToString();
            Response.Headers["ETag"] = new Microsoft.Net.Http.Headers.EntityTagHeaderValue(
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
    
    
    #region Range Streaming Support

    private class RangeHelper
    {
        public long Start { get; set; }
        public long End { get; set; }
        public long Length { get; set; }
        public long TotalLength { get; set; }

        public static bool TryParse(string rangeHeader, long totalLength, out RangeHelper range)
        {
            range = null;

            if (string.IsNullOrEmpty(rangeHeader) || !rangeHeader.StartsWith("bytes="))
                return false;

            var rangeValue = rangeHeader.Substring("bytes=".Length);
            var ranges = rangeValue.Split('-');

            if (ranges.Length != 2)
                return false;

            long start, end;

            if (string.IsNullOrEmpty(ranges[0]))
            {
                // Формат: bytes=-500 (последние 500 байт)
                if (!long.TryParse(ranges[1], out var suffixLength))
                    return false;

                start = totalLength - suffixLength;
                end = totalLength - 1;
            }
            else if (string.IsNullOrEmpty(ranges[1]))
            {
                // Формат: bytes=500- (с 500 байта до конца)
                if (!long.TryParse(ranges[0], out start))
                    return false;

                end = totalLength - 1;
            }
            else
            {
                // Формат: bytes=0-1023 (конкретный диапазон)
                if (!long.TryParse(ranges[0], out start) || !long.TryParse(ranges[1], out end))
                    return false;
            }

            // Валидация
            if (start < 0 || end >= totalLength || start > end)
                return false;

            range = new RangeHelper
            {
                Start = start,
                End = end,
                Length = end - start + 1,
                TotalLength = totalLength
            };

            return true;
        }
    }

    private async Task CopyStreamRangeAsync(Stream source, Stream destination, long start, long length)
    {
        var buffer = new byte[4096]; // 4KB chunks
        long bytesCopied = 0;

        source.Seek(start, SeekOrigin.Begin);

        while (bytesCopied < length)
        {
            var bytesToRead = (int)Math.Min(buffer.Length, length - bytesCopied);
            var bytesRead = await source.ReadAsync(buffer, 0, bytesToRead);

            if (bytesRead == 0) break;

            await destination.WriteAsync(buffer, 0, bytesRead);
            bytesCopied += bytesRead;
        }
    }

    #endregion

}