using System.Security.Claims;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using FileUploadRequest = IceBreakerApp.Application.DTOs.FileUploadRequest;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IChunkedFileService _chunkedFileService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileService fileService, IChunkedFileService chunkedFileService, ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _chunkedFileService = chunkedFileService;
        _logger = logger;
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

        return Ok(new PaginatedResult<FileMetadataDto>(dtos, paginatedResult.TotalCount, paginatedResult.PageNumber, paginatedResult.PageSize));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Download(Guid id)
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
                return Forbid("У вас нет прав для скачивания этого файла.");
            }

            await _fileService.IncrementDownloadCountAsync(id);

            var stream = await _fileService.GetFileStreamAsync(id);

            // Определяем Content-Disposition
            var contentDisposition = new ContentDisposition
            {
                FileName = metadata.OriginalFileName,
                Inline = IsInlineDisplayable(metadata.ContentType) // true для отображения в браузере, false для скачивания
            };
            Response.Headers.Add("Content-Disposition", contentDisposition.ToString());

            return File(stream, metadata.ContentType, enableRangeProcessing: true);
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
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, $"Файл с ID {id} не найден.");
            return NotFound("Файл не найден.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при стриминге файла с ID {id}.");
            return StatusCode(500, "Внутренняя ошибка сервера.");
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

    private bool IsInlineDisplayable(string contentType)
    {
        // Определяем, какие типы файлов можно отображать прямо в браузере
        return contentType.StartsWith("image/") ||
               contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    }
}
