using IceBreakerApp.Application.Utils;
using System.Security.Cryptography;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using IceBreakerApp.Application.DTOs;
using System.Linq.Expressions;
using IceBreakerApp.Application.IRepositories;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace IceBreakerApp.Application.Services;

public class FileService : IFileService
{
    private readonly IFileMetadataRepository _fileMetadataRepository;
    private readonly IFileStorageSettings _storageSettings;
    private readonly ILogger<FileService> _logger;

    public FileService(
        IFileMetadataRepository fileMetadataRepository,
        IFileStorageSettings storageSettings,
        ILogger<FileService> logger)
    {
        _fileMetadataRepository = fileMetadataRepository;
        _storageSettings = storageSettings;
        _logger = logger;
    }

    public async Task<FileMetadata> UploadAsync(
        IFormFile file,
        Guid userId,
        bool isPublic = false,
        DateTime? expiresAt = null)
    {
        // 1. ВАЛИДАЦИЯ (по ТЗ)
        await ValidateFileAsync(file);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{extension}";
        var now = DateTime.UtcNow;

        // 2. СТРУКТУРИРОВАННОЕ ХРАНЕНИЕ (по ТЗ: по дате ИЛИ по userId)
        var path = _storageSettings.UseUserIdStructure
            ? $"users/{userId}/{_storageSettings.GetDateStructuredPath(now)}"
            : _storageSettings.GetDateStructuredPath(now);

        var filePath = _storageSettings.PrepareFilePath(now, fileName);

        // 3. ХЕШ и проверка дубликата
        string hash;
        await using (var fileStream = file.OpenReadStream())
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(fileStream);
            hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            fileStream.Position = 0;

            // Проверка дубликата (опционально по ТЗ)
            var existing = await _fileMetadataRepository.FindByHashAsync(hash);
            if (existing != null)
            {
                _logger.LogInformation($"File with hash {hash} already exists.");
                return existing;
            }

            // Сохранение файла
            await using var outputStream = new FileStream(
                filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await fileStream.CopyToAsync(outputStream);
        }

        // 4. МЕТАДАННЫЕ
        var metadata = new FileMetadata
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            UploadedById = userId,
            UploadedAt = now,
            Path = path,
            Hash = hash,
            IsPublic = isPublic,
            ExpiresAt = expiresAt,
            DownloadCount = 0,
            IsDeleted = false // для soft delete
        };

        // 5. ОБРАБОТКА ИЗОБРАЖЕНИЙ (по ТЗ)
        if (_storageSettings.IsImageType(file.ContentType))
        {
            await ProcessImageAsync(filePath, metadata, isPublic);
        }

        await _fileMetadataRepository.AddAsync(metadata);
        await _fileMetadataRepository.SaveChangesAsync();

        return metadata;
    }

    public async Task<List<FileMetadata>> UploadMultipleAsync(
        List<IFormFile> files,
        Guid userId,
        bool isPublic = false,
        DateTime? expiresAt = null)
    {
        // Валидация по ТЗ: максимум 10 файлов
        if (files.Count > _storageSettings.MaxFilesPerUpload)
        {
            throw new InvalidOperationException(
                $"Maximum {_storageSettings.MaxFilesPerUpload} files allowed per upload.");
        }

        // Валидация по ТЗ: общий размер не более 100MB
        var totalSize = files.Sum(f => f.Length);
        if (totalSize > _storageSettings.MaxMultipleFileSize)
        {
            throw new InvalidOperationException(
                $"Total file size cannot exceed {_storageSettings.MaxMultipleFileSize / (1024 * 1024)}MB.");
        }

        var uploadedFiles = new List<FileMetadata>();
        var savedPaths = new List<string>();

        try
        {
            foreach (var file in files)
            {
                var metadata = await UploadAsync(file, userId, isPublic, expiresAt);
                uploadedFiles.Add(metadata);
                savedPaths.Add(_storageSettings.GetFullFilePath(metadata.Path, metadata.FileName));
            }
        }
        catch (Exception ex)
        {
            // Откат по ТЗ: если один файл не прошёл - отклонить все
            _logger.LogError(ex, "Error in batch upload, rolling back...");

            foreach (var path in savedPaths)
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                catch
                {
                    /* ignore */
                }
            }

            throw new InvalidOperationException("Batch upload failed. All files have been rolled back.", ex);
        }

        return uploadedFiles;
    }

    public async Task<FileMetadata> GetAsync(Guid id)
    {
        var metadata = await _fileMetadataRepository.GetByIdAsync(id);
        if (metadata == null || metadata.IsDeleted)
        {
            throw new FileNotFoundException($"File with ID {id} not found.");
        }

        return metadata;
    }

    public async Task<FileMetadata> GetInfoAsync(Guid id, Guid currentUserId, bool isAdmin)
    {
        var metadata = await GetAsync(id);

        // Проверка истечения срока
        if (metadata.ExpiresAt.HasValue && metadata.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException($"File has expired.");
        }

        // Проверка прав доступа по ТЗ
        if (!metadata.IsPublic && metadata.UploadedById != currentUserId && !isAdmin)
        {
            throw new UnauthorizedAccessException("No access to this file.");
        }

        return metadata;
    }

    public async Task<Stream> GetFileStreamAsync(Guid id)
    {
        var metadata = await GetAsync(id);
        var fullPath = _storageSettings.GetFullFilePath(metadata.Path, metadata.FileName);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found at {fullPath}");
        }

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
    }

    public async Task IncrementDownloadCountAsync(Guid id)
    {
        var metadata = await _fileMetadataRepository.GetByIdAsync(id);
        if (metadata != null)
        {
            metadata.DownloadCount++;
            await _fileMetadataRepository.UpdateAsync(metadata);
            await _fileMetadataRepository.SaveChangesAsync();
        }
    }

    public async Task<string> GenerateThumbnailAsync(Guid id, string size)
    {
        var metadata = await GetAsync(id);

        if (!_storageSettings.IsImageType(metadata.ContentType))
        {
            throw new InvalidOperationException("Thumbnails only for images.");
        }

        // Реализация генерации (у вас уже есть)
        var originalPath = _storageSettings.GetFullFilePath(metadata.Path, metadata.FileName);
        var (smallPath, mediumPath) =
            await GenerateThumbnailsInternalAsync(originalPath, metadata.Path, metadata.FileName);

        // Обновление путей
        metadata.SmallThumbnailPath = smallPath;
        metadata.MediumThumbnailPath = mediumPath;
        await _fileMetadataRepository.UpdateAsync(metadata);
        await _fileMetadataRepository.SaveChangesAsync();

        return size.ToLower() == "small"
            ? _storageSettings.GetFullThumbnailPath(smallPath)
            : _storageSettings.GetFullThumbnailPath(mediumPath);
    }


    public async Task<string> GetThumbnailPathAsync(Guid id, string size)
    {
        var metadata = await GetAsync(id);

        if (!_storageSettings.IsImageType(metadata.ContentType))
        {
            throw new InvalidOperationException("Thumbnails are only available for image files.");
        }

        string thumbnailRelativePath;
        if (size.ToLower() == "small")
        {
            thumbnailRelativePath = metadata.SmallThumbnailPath;
        }
        else if (size.ToLower() == "medium")
        {
            thumbnailRelativePath = metadata.MediumThumbnailPath;
        }
        else
        {
            throw new ArgumentException("Invalid thumbnail size. Supported sizes are 'small' and 'medium'.");
        }

        if (string.IsNullOrEmpty(thumbnailRelativePath))
        {
            // Если миниатюра еще не сгенерирована, генерируем ее
            _logger.LogInformation($"Thumbnail for file {id} (size: {size}) not found, generating now.");
            await GenerateThumbnailAsync(id, size); // Это обновит метаданные
            metadata = await GetAsync(id); // Перечитываем обновленные метаданные

            if (size.ToLower() == "small")
            {
                thumbnailRelativePath = metadata.SmallThumbnailPath;
            }
            else
            {
                thumbnailRelativePath = metadata.MediumThumbnailPath;
            }
        }

        if (string.IsNullOrEmpty(thumbnailRelativePath))
        {
            throw new InvalidOperationException(
                $"Failed to generate or retrieve thumbnail for file {id} with size {size}.");
        }

        return _storageSettings.GetFullThumbnailPath(thumbnailRelativePath);
    }

    public async Task<PaginatedResult<FileMetadata>> GetFilesAsync(
        Guid userId,
        bool isAdmin,
        int page,
        int pageSize,
        string? contentTypeFilter,
        string? search,
        string sortBy,
        string sortDescending)
    {
        // По ТЗ: админ видит все файлы, пользователь - только свои или публичные
        Expression<Func<FileMetadata, bool>> filter = isAdmin
            ? f => !f.IsDeleted
            : f => !f.IsDeleted && (f.UploadedById == userId || f.IsPublic);

        // Фильтр по типу контента (по ТЗ)
        if (!string.IsNullOrEmpty(contentTypeFilter))
        {
            if (contentTypeFilter.ToLower() == "image")
            {
                filter = ExpressionCombiner.CombineExpressions(filter,
                    f => _storageSettings.IsImageType(f.ContentType));
            }
            else if (contentTypeFilter.ToLower() == "document")
            {
                filter = ExpressionCombiner.CombineExpressions(filter, f =>
                    f.ContentType.StartsWith("application/pdf") ||
                    f.ContentType.Contains("document") ||
                    f.ContentType.Contains("sheet"));
            }
        }

        // Поиск по имени (по ТЗ)
        if (!string.IsNullOrEmpty(search))
        {
            filter = ExpressionCombiner.CombineExpressions(filter, f => f.OriginalFileName.Contains(search));
        }

        // Сортировка (по ТЗ: по дате загрузки)
        Expression<Func<FileMetadata, object>> orderBy = sortBy?.ToLower() switch
        {
            "originalfilename" => f => f.OriginalFileName,
            "size" => f => f.Size,
            _ => f => f.UploadedAt // по умолчанию по ТЗ
        };

        return await _fileMetadataRepository.GetPaginatedAsync(
            filter, orderBy, sortDescending, page, pageSize);
    }

    public async Task DeleteAsync(Guid id, Guid currentUserId, bool isAdmin)
    {
        var metadata = await GetAsync(id);

        // Проверка прав по ТЗ: владелец ИЛИ админ
        if (metadata.UploadedById != currentUserId && !isAdmin)
        {
            throw new UnauthorizedAccessException("No permission to delete this file.");
        }

        // SOFT DELETE (по ТЗ: "ИЛИ soft delete")
        metadata.IsDeleted = true;
        await _fileMetadataRepository.UpdateAsync(metadata);
        await _fileMetadataRepository.SaveChangesAsync();

        _logger.LogInformation($"File {id} soft deleted by user {currentUserId}");
    }

    public async Task HardDeleteAsync(Guid id, Guid currentUserId, bool isAdmin)
    {
        var metadata = await GetAsync(id);

        if (metadata.UploadedById != currentUserId && !isAdmin)
        {
            throw new UnauthorizedAccessException("No permission to delete this file.");
        }

        // Физическое удаление (по ТЗ)
        try
        {
            _storageSettings.DeleteFileAndThumbnails(
                metadata.Path, metadata.FileName, metadata.SmallThumbnailPath, metadata.MediumThumbnailPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting physical files for {id}");
        }

        await _fileMetadataRepository.DeleteAsync(metadata);
        await _fileMetadataRepository.SaveChangesAsync();
    }

    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

    private async Task ValidateFileAsync(IFormFile file)
    {
        // 1. Размер: максимум 50MB (по ТЗ)
        if (file.Length > _storageSettings.MaxFileSize)
        {
            throw new InvalidOperationException(
                $"File size cannot exceed {_storageSettings.MaxFileSize / (1024 * 1024)}MB. Current: {file.Length} bytes");
        }

        // 2. Имя файла: проверка на path traversal (по ТЗ)
        var fileName = file.FileName;
        if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
        {
                
            throw new InvalidOperationException("Invalid file name.");
        }

        // 3. Whitelist типов (по ТЗ)
        var contentType = file.ContentType.ToLower();
        if (!_storageSettings.IsAllowedFileType(contentType, fileName))
        {
            throw new InvalidOperationException($"File type '{contentType}' is not allowed.");
        }

        // 4. Magic bytes проверка (по ТЗ)
        var (isValidSignature, detectedContentType) = FileSignature.Validate(file);
        if (!isValidSignature)
        {
            throw new InvalidOperationException("File signature does not match any known file types or is invalid.");
        }

        // Дополнительная проверка: совпадает ли заявленный Content-Type с обнаруженным по сигнатуре
        if (!string.Equals(contentType, detectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                $"Declared Content-Type \'{contentType}\' does not match detected \'{detectedContentType}\' for file \'{file.FileName}\'.");
            // Можно решить, что делать: отклонить, или использовать detectedContentType
            // Для безопасности, пока отклоняем, если не совпадает
            throw new InvalidOperationException(
                $"Declared file type \'{contentType}\' does not match actual file type \'{detectedContentType}\'.");
        }
    }

    private async Task ProcessImageAsync(string filePath, FileMetadata metadata, bool isPublic)
    {
        try
        {
            await using var imageStream = File.OpenRead(filePath);
            using var image = await Image.LoadAsync(imageStream);

            metadata.Width = image.Width;
            metadata.Height = image.Height;

            // Извлечение EXIF данных
            var exifProfile = image.Metadata.ExifProfile;
            if (exifProfile != null)
            {
                // Для DateTimeOriginal
                if (exifProfile.TryGetValue(ExifTag.DateTimeOriginal, out var dateTimeOriginalValue))
                {
                    metadata.DateTaken = (DateTime?)dateTimeOriginalValue.GetValue();
                }

                // Для Model (камера)
                if (exifProfile.TryGetValue(ExifTag.Model, out var modelValue))
                {
                    metadata.CameraModel = modelValue.ToString();
                }

                // Для Orientation
                if (exifProfile.TryGetValue(ExifTag.Orientation, out var orientationValue))
                {
                    metadata.Orientation = (int?)orientationValue.GetValue();
                }
            }


            image.Mutate(x => x.AutoOrient());

            // Создаем новый MemoryStream для сохранения
            using var outputStream = new MemoryStream();

            if (isPublic)
            {
                // Для публичных файлов удаляем EXIF и сохраняем как PNG (нет EXIF)
                image.Metadata.ExifProfile = null; // Удаляем EXIF профиль
                await image.SaveAsPngAsync(outputStream);
            }
            else
            {
                // Для приватных сохраняем как JPEG с качеством 90%
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 90 });
            }

            // Перезаписываем файл
            outputStream.Position = 0;
            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await outputStream.CopyToAsync(fileStream);

            // Генерация thumbnails
            var (smallPath, mediumThumbPath) = await GenerateThumbnailsInternalAsync(
                filePath, metadata.Path, metadata.FileName);

            metadata.SmallThumbnailPath = smallPath;
            metadata.MediumThumbnailPath = mediumThumbPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $" Error processing image {filePath}");
            throw new InvalidOperationException("Failed to process image file.", ex);
        }
    }

    private async Task<(string smallPath, string mediumPath)> GenerateThumbnailsInternalAsync(
        string originalPath, string relativePath, string fileName)
    {
        using var image = await Image.LoadAsync(originalPath);

        var smallPath = "";
        var mediumPath = "";

        // Small: 200x200 
        var thumbName = Path.GetFileNameWithoutExtension(fileName) + "_small" + Path.GetExtension(fileName);
        var thumbFile = _storageSettings.PrepareThumbnailPath(relativePath, "small", thumbName);

        using var smallImage = image.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(200, 200),
            Mode = ResizeMode.Max
        })); // Копируем, чтобы не нарушать оригинал
        await smallImage.SaveAsJpegAsync(thumbFile, new JpegEncoder { Quality = 90 });
        smallPath = _storageSettings.GetRelativePathFromFiles(thumbFile);

        // Medium: 800x600 
        thumbName = Path.GetFileNameWithoutExtension(fileName) + "_medium" + Path.GetExtension(fileName);
        var thumbFileMedium = _storageSettings.PrepareThumbnailPath(relativePath, "medium", thumbName);

        using var mediumImage = image.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(800, 600),
            Mode = ResizeMode.Max
        }));
        await mediumImage.SaveAsJpegAsync(thumbFileMedium, new JpegEncoder { Quality = 90 });
        mediumPath = _storageSettings.GetRelativePathFromFiles(thumbFileMedium);

        return (smallPath, mediumPath);
    }
}

    