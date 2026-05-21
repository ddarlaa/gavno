using System.Security.Cryptography;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System.Linq.Expressions;
using IceBreakerApp.Application.Utils;
using IceBreakerApp.Domain.Models;

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

    #region Основные методы загрузки

    public async Task<FileMetadata> UploadLargeFileAsync(
        IFormFile file,
        Guid userId,
        bool isPublic = false,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Начало загрузки БОЛЬШОГО файла: {FileName} пользователем {UserId}", 
                file.FileName, userId);

            // Прямая валидация
            if (file.Length == 0)
                throw new ArgumentException("Файл пустой");

            // Увеличиваем лимит для больших файлов
            long largeFileLimit = 2_000_000_000; // 2GB
            if (file.Length > largeFileLimit)
                throw new ArgumentException($"Размер файла превышает {largeFileLimit / 1024 / 1024 / 1024}GB");

            // Используем валидацию
            await ValidateFileAsync(file);

            // Используем основной метод UploadAsync
            return await UploadAsync(file, userId, isPublic, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке БОЛЬШОГО файла: {FileName}", file.FileName);
            throw;
        }
    }
    
    public async Task<FileMetadata> UploadAsync(
        IFormFile file,
        Guid userId,
        bool isPublic = false,
        DateTime? expiresAt = null)
    {
        try
        {
            _logger.LogInformation("Начало загрузки файла: {FileName} пользователем {UserId}", 
                file.FileName, userId);

            // 1. ВАЛИДАЦИЯ
            await ValidateFileAsync(file);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var correctContentType = GetContentType(extension);
            var now = DateTime.UtcNow;

            
            
            // 2. СТРУКТУРИРОВАННОЕ ХРАНЕНИЕ
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

                // Проверка дубликата
                var existing = await _fileMetadataRepository.FindByHashAsync(hash);
                if (existing != null)
                {
                    _logger.LogInformation("Файл с хешем {Hash} уже существует (ID: {FileId})", hash, existing.Id);
                    
                    // Инкрементируем счетчик скачиваний
                    existing.DownloadCount++;
                    await _fileMetadataRepository.UpdateAsync(existing);
                    await _fileMetadataRepository.SaveChangesAsync();
                    
                    return existing;
                }

                // Сохранение файла
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory!);

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
                ContentType = correctContentType,
                Size = file.Length,
                UploadedById = userId,
                UploadedAt = now,
                Path = path,
                Hash = hash,
                IsPublic = isPublic,
                ExpiresAt = expiresAt,
                DownloadCount = 0,
                IsDeleted = false
            };

            // 5. ОБРАБОТКА ИЗОБРАЖЕНИЙ
            if (IsImageType(correctContentType))
            {
                await ProcessImageAsync(filePath, metadata, isPublic);
            }

            await _fileMetadataRepository.AddAsync(metadata);
            await _fileMetadataRepository.SaveChangesAsync();

            _logger.LogInformation("Файл успешно загружен: {FileId}", metadata.Id);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке файла: {FileName}", file.FileName);
            throw;
        }
    }

    public async Task<StreamUploadResultDto> UploadFileStreamAsync(
        Stream fileStream,
        StreamUploadDto uploadDto,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Начало потоковой загрузки файла: {FileName} пользователем {UserId}",
                uploadDto.OriginalFileName, userId);

            // ВАЛИДАЦИЯ
            if (string.IsNullOrEmpty(uploadDto.OriginalFileName))
                throw new ArgumentException("Имя файла обязательно");

            var fileExtension = Path.GetExtension(uploadDto.OriginalFileName)?.ToLowerInvariant();
            
            var correctContentType = GetContentType(fileExtension ?? ""); 
            
            // Базовая проверка расширения
            if (string.IsNullOrEmpty(fileExtension))
                throw new ArgumentException("Недопустимое расширение файла");

            // Проверка Content-Type
            var expectedContentType = GetContentType(fileExtension);
            if (!string.Equals(uploadDto.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Declared Content-Type '{ContentType}' does not match expected '{ExpectedContentType}'",
                    uploadDto.ContentType, expectedContentType);
            }

            // Создаем временный файл для обработки потока
            var tempFileName = $"{Guid.NewGuid()}{fileExtension}";
            var tempFilePath = Path.Combine(_storageSettings.StoragePath, "Temp", tempFileName);

            // Создаем директорию для временных файлов
            Directory.CreateDirectory(Path.Combine(_storageSettings.StoragePath, "Temp"));

            long fileSize = 0;

            // 1. Пишем поток во временный файл
            using (var tempFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await fileStream.CopyToAsync(tempFileStream, cancellationToken);
                await tempFileStream.FlushAsync(cancellationToken);
                fileSize = tempFileStream.Length;
            }

            // 2. Проверяем размер файла
            if (fileSize == 0)
                throw new ArgumentException("Файл пустой");

            if (fileSize > _storageSettings.MaxFileSize)
                throw new ArgumentException($"Размер файла превышает {_storageSettings.MaxFileSize / 1024 / 1024}MB");

            // 3. Проверяем сигнатуру файла (Magic Bytes)
            using (var verifyStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
            {
                if (!FileSignature.ValidateStream(verifyStream, fileExtension ?? ""))
                    throw new ArgumentException("Сигнатура файла не соответствует расширению");
            }

            // 4. Вычисляем хэш файла
            string fileHash;
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(tempFilePath))
            {
                var hashBytes = await sha256.ComputeHashAsync(stream);
                fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            // 5. Проверяем дубликат
            var existingFile = await _fileMetadataRepository.FindByHashAsync(fileHash);
            if (existingFile != null)
            {
                // Удаляем временный файл
                File.Delete(tempFilePath);

                _logger.LogInformation("Обнаружен дубликат файла: {FileId}", existingFile.Id);

                return new StreamUploadResultDto
                {
                    IsDuplicate = true,
                    ExistingFileId = existingFile.Id,
                    FileName = existingFile.FileName,
                    OriginalFileName = existingFile.OriginalFileName,
                    Size = existingFile.Size,
                    ContentType = correctContentType,
                    IsPublic = existingFile.IsPublic,
                    ExpiresAt = existingFile.ExpiresAt,
                    CreatedAt = existingFile.UploadedAt,
                    Message = "Файл уже существует"
                };
            }

            // 6. Генерируем окончательное имя файла и путь
            var now = DateTime.UtcNow;
            var safeFileName = $"{Guid.NewGuid()}{fileExtension}";
            
            // Используем структурированное хранение
            var path = _storageSettings.UseUserIdStructure
                ? $"users/{userId}/{_storageSettings.GetDateStructuredPath(now)}"
                : _storageSettings.GetDateStructuredPath(now);

            var finalFilePath = _storageSettings.PrepareFilePath(now, safeFileName);
            
            // Создаем директорию
            var directory = Path.GetDirectoryName(finalFilePath)!;
            Directory.CreateDirectory(directory);

            // 7. Перемещаем временный файл в окончательное место
            File.Move(tempFilePath, finalFilePath);

            // 8. Создаем запись в БД
            var fileMetadata = new FileMetadata
            {
                Id = Guid.NewGuid(),
                FileName = safeFileName,
                OriginalFileName = SanitizeFileName(uploadDto.OriginalFileName),
                ContentType = correctContentType,
                Size = fileSize,
                Path = path,
                Hash = fileHash,
                IsPublic = uploadDto.IsPublic,
                ExpiresAt = uploadDto.ExpiresAt,
                DownloadCount = 0,
                UploadedById = userId,
                UploadedAt = now,
                IsDeleted = false
            };

            // 9. Если это изображение - обрабатываем
            if (IsImageType(uploadDto.ContentType))
            {
                await ProcessImageAsync(finalFilePath, fileMetadata, uploadDto.IsPublic);
            }

            await _fileMetadataRepository.AddAsync(fileMetadata);
            await _fileMetadataRepository.SaveChangesAsync();

            _logger.LogInformation("Потоковая загрузка завершена: {FileId}", fileMetadata.Id);

            // 10. Возвращаем результат
            return new StreamUploadResultDto
            {
                Id = fileMetadata.Id,
                FileName = fileMetadata.FileName,
                OriginalFileName = fileMetadata.OriginalFileName,
                Size = fileMetadata.Size,
                ContentType = fileMetadata.ContentType,
                IsPublic = fileMetadata.IsPublic,
                ExpiresAt = fileMetadata.ExpiresAt,
                CreatedAt = fileMetadata.UploadedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при потоковой загрузке файла {FileName} пользователем {UserId}",
                uploadDto.OriginalFileName, userId);
            throw;
        }
    }
    
    public async Task<List<FileMetadata>> UploadMultipleAsync(
        List<IFormFile> files,
        Guid userId,
        bool isPublic = false,
        DateTime? expiresAt = null)
    {
        // Валидация: максимум файлов
        if (files.Count > _storageSettings.MaxFilesPerUpload)
        {
            throw new InvalidOperationException(
                $"Maximum {_storageSettings.MaxFilesPerUpload} files allowed per upload.");
        }

        // Валидация: общий размер
        var totalSize = files.Sum(f => f.Length);
        if (totalSize > _storageSettings.MaxMultipleFileSize)
        {
            throw new InvalidOperationException(
                $"Total file size cannot exceed {_storageSettings.MaxMultipleFileSize / (1024 * 1024)}MB.");
        }

        var uploadedFiles = new List<FileMetadata>();

        foreach (var file in files)
        {
            try
            {
                var metadata = await UploadAsync(file, userId, isPublic, expiresAt);
                uploadedFiles.Add(metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке файла {FileName} в пакете", file.FileName);
                
                // Откат уже загруженных файлов
                foreach (var uploadedFile in uploadedFiles)
                {
                    try
                    {
                        var filePath = _storageSettings.GetFullFilePath(uploadedFile.Path, uploadedFile.FileName);
                        if (File.Exists(filePath))
                            File.Delete(filePath);
                        
                        await _fileMetadataRepository.DeleteAsync(uploadedFile);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "Ошибка при откате файла {FileId}", uploadedFile.Id);
                    }
                }
                
                throw new InvalidOperationException($"Ошибка при пакетной загрузке файлов: {ex.Message}", ex);
            }
        }

        return uploadedFiles;
    }

    #endregion

    #region Вспомогательные методы

    private async Task ValidateFileAsync(IFormFile file)
    {
        try
        {
            _logger.LogDebug("Начало валидации файла: {FileName}", file.FileName);

            // 1. Базовые проверки
            if (file.Length == 0)
                throw new ArgumentException("Файл пустой");

            if (file.Length > _storageSettings.MaxFileSize)
                throw new ArgumentException(
                    $"Размер файла превышает {_storageSettings.MaxFileSize / (1024 * 1024)}MB. " +
                    $"Текущий размер: {file.Length} байт");

            // 2. Проверка имени файла
            var fileName = SanitizeFileName(file.FileName);
            if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
                throw new ArgumentException("Недопустимое имя файла (содержит опасные символы)");

            // 3. Проверка расширения файла
            var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension))
                throw new ArgumentException("Файл не имеет расширения");

            // 4. Проверка Content-Type
            var contentType = file.ContentType;
            var expectedContentType = GetContentType(fileExtension);
            
            if (!string.Equals(contentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Declared Content-Type '{ContentType}' does not match expected '{ExpectedContentType}' for file '{FileName}'",
                    contentType, expectedContentType, fileName);
            }

            // 5. Проверка сигнатуры файла (Magic Bytes)
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            if (!FileSignature.ValidateStream(memoryStream, fileExtension))
            {
                // Для отладки логируем первые байты
                memoryStream.Position = 0;
                var headerBytes = new byte[16];
                var bytesRead = await memoryStream.ReadAsync(headerBytes.AsMemory(0, 16));
                var hexSignature = BitConverter.ToString(headerBytes, 0, bytesRead).Replace("-", "");
                
                _logger.LogError(
                    "Сигнатура файла не соответствует расширению '{FileExtension}'. " +
                    "Первые байты: {HexSignature}",
                    fileExtension, hexSignature);
                
                throw new ArgumentException($"Сигнатура файла не соответствует расширению '{fileExtension}'");
            }

            _logger.LogInformation("Файл {FileName} успешно прошел валидацию", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка валидации файла {FileName}", file.FileName);
            throw;
        }
    }

    private async Task ProcessImageAsync(string filePath, FileMetadata metadata, bool isPublic)
    {
        try
        {
            _logger.LogInformation("Обработка изображения: {FilePath}", filePath);

            // Загружаем изображение из файла
            using var image = await Image.LoadAsync(filePath);

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

            // Перезаписываем файл из MemoryStream
            outputStream.Position = 0;
            await File.WriteAllBytesAsync(filePath, outputStream.ToArray());

            // Генерация thumbnails
            var (smallPath, mediumThumbPath) = await GenerateThumbnailsInternalAsync(
                filePath, metadata.Path, metadata.FileName);

            metadata.SmallThumbnailPath = smallPath;
            metadata.MediumThumbnailPath = mediumThumbPath;

            _logger.LogInformation("Обработка изображения завершена: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки изображения {FilePath}", filePath);
            throw new InvalidOperationException("Не удалось обработать изображение", ex);
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
        }));
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

    private bool IsImageType(string contentType)
    {
        return !string.IsNullOrEmpty(contentType) && 
               contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "unnamed_file";

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(ch => !invalidChars.Contains(ch))
            .ToArray());

        if (string.IsNullOrEmpty(sanitized))
            sanitized = "unnamed_file";

        if (sanitized.Length > 255)
            sanitized = sanitized[..255];

        return sanitized;
    }

    private string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            // Изображения
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            
            // PDF и документы
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            
            // Видео
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            
            // Аудио
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            
            // Архивы
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            
            _ => "application/octet-stream"
        };
    }

    #endregion

    #region Методы получения файлов

    public async Task<FileMetadata> GetAsync(Guid id)
    {
        var metadata = await _fileMetadataRepository.GetByIdAsync(id);
        if (metadata == null || metadata.IsDeleted)
        {
            throw new FileNotFoundException($"Файл с ID {id} не найден.");
        }

        return metadata;
    }

    public async Task<FileMetadata> GetInfoAsync(Guid id, Guid currentUserId, bool isAdmin)
    {
        var metadata = await GetAsync(id);

        // Проверка истечения срока
        if (metadata.ExpiresAt.HasValue && metadata.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Срок действия файла истек.");
        }

        // Проверка прав доступа
        if (!metadata.IsPublic && metadata.UploadedById != currentUserId && !isAdmin)
        {
            throw new UnauthorizedAccessException("Нет доступа к этому файлу.");
        }

        return metadata;
    }

    public async Task<Stream> GetFileStreamAsync(Guid id)
    {
        var metadata = await GetAsync(id);
        var fullPath = _storageSettings.GetFullFilePath(metadata.Path, metadata.FileName);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Файл не найден по пути: {fullPath}");
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

        if (!IsImageType(metadata.ContentType))
        {
            throw new InvalidOperationException("Миниатюры доступны только для изображений.");
        }

        var originalPath = _storageSettings.GetFullFilePath(metadata.Path, metadata.FileName);
        var (smallPath, mediumPath) =
            await GenerateThumbnailsInternalAsync(originalPath, metadata.Path, metadata.FileName);

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

        if (!IsImageType(metadata.ContentType))
        {
            throw new InvalidOperationException("Миниатюры доступны только для изображений.");
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
            throw new ArgumentException("Недопустимый размер миниатюры. Поддерживаемые размеры: 'small' и 'medium'.");
        }

        if (string.IsNullOrEmpty(thumbnailRelativePath))
        {
            _logger.LogInformation("Миниатюра для файла {FileId} (размер: {Size}) не найдена, генерируем", id, size);
            await GenerateThumbnailAsync(id, size);
            metadata = await GetAsync(id);

            thumbnailRelativePath = size.ToLower() == "small"
                ? metadata.SmallThumbnailPath
                : metadata.MediumThumbnailPath;
        }

        if (string.IsNullOrEmpty(thumbnailRelativePath))
        {
            throw new InvalidOperationException(
                $"Не удалось создать или получить миниатюру для файла {id} с размером {size}.");
        }

        return _storageSettings.GetFullThumbnailPath(thumbnailRelativePath);
    }

    #endregion

    #region Методы управления файлами

    public async Task<PaginatedResult<FileMetadata>> GetFilesAsync(Guid userId,
        bool isAdmin,
        int page,
        int pageSize,
        string? contentTypeFilter,
        string? search,
        string sortBy,
        bool sortDescending)
    {
        // Админ видит все файлы, пользователь - только свои или публичные
        Expression<Func<FileMetadata, bool>> filter = isAdmin
            ? f => !f.IsDeleted
            : f => !f.IsDeleted && (f.UploadedById == userId || f.IsPublic);

        // Фильтр по типу контента
        if (!string.IsNullOrEmpty(contentTypeFilter))
        {
            if (contentTypeFilter.ToLower() == "image")
            {
                filter = ExpressionCombiner.CombineExpressions(filter,
                    f => IsImageType(f.ContentType));
            }
            else if (contentTypeFilter.ToLower() == "document")
            {
                filter = ExpressionCombiner.CombineExpressions(filter, f =>
                    f.ContentType.StartsWith("application/pdf") ||
                    f.ContentType.Contains("document") ||
                    f.ContentType.Contains("sheet"));
            }
        }

        // Поиск по имени
        if (!string.IsNullOrEmpty(search))
        {
            filter = ExpressionCombiner.CombineExpressions(filter, f => f.OriginalFileName.Contains(search));
        }

        // Сортировка
        Expression<Func<FileMetadata, object>> orderBy = sortBy?.ToLower() switch
        {
            "originalfilename" => f => f.OriginalFileName,
            "size" => f => f.Size,
            _ => f => f.UploadedAt
        };

        return await _fileMetadataRepository.GetPaginatedAsync(
            filter, orderBy, sortDescending, page, pageSize);
    }

    public async Task DeleteAsync(Guid id, Guid currentUserId, bool isAdmin)
    {
        var metadata = await GetAsync(id);

        // Проверка прав: владелец ИЛИ админ
        if (metadata.UploadedById != currentUserId && !isAdmin)
        {
            throw new UnauthorizedAccessException("Нет прав на удаление этого файла.");
        }

        // SOFT DELETE
        metadata.IsDeleted = true;
        await _fileMetadataRepository.UpdateAsync(metadata);
        await _fileMetadataRepository.SaveChangesAsync();

        _logger.LogInformation("Файл {FileId} помечен как удаленный пользователем {UserId}", id, currentUserId);
    }

    public async Task HardDeleteAsync(Guid id, Guid currentUserId, bool isAdmin)
    {
        var metadata = await GetAsync(id);

        if (metadata.UploadedById != currentUserId && !isAdmin)
        {
            throw new UnauthorizedAccessException("Нет прав на удаление этого файла.");
        }

        // Физическое удаление файла
        try
        {
            var filePath = _storageSettings.GetFullFilePath(metadata.Path, metadata.FileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
            
            // Удаление миниатюр
            if (!string.IsNullOrEmpty(metadata.SmallThumbnailPath))
            {
                var smallThumbPath = _storageSettings.GetFullThumbnailPath(metadata.SmallThumbnailPath);
                if (File.Exists(smallThumbPath))
                    File.Delete(smallThumbPath);
            }
            
            if (!string.IsNullOrEmpty(metadata.MediumThumbnailPath))
            {
                var mediumThumbPath = _storageSettings.GetFullThumbnailPath(metadata.MediumThumbnailPath);
                if (File.Exists(mediumThumbPath))
                    File.Delete(mediumThumbPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении физических файлов для {FileId}", id);
        }

        await _fileMetadataRepository.DeleteAsync(metadata);
        await _fileMetadataRepository.SaveChangesAsync();
        
        _logger.LogInformation("Файл {FileId} полностью удален пользователем {UserId}", id, currentUserId);
    }

    #endregion
}

public static class FileSignature
{
    // Расширенный словарь сигнатур
    private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        // Изображения
        { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, 
                                       new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                                       new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 } } },
        { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, 
                                        new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                                        new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 } } },
        { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, 
                                       new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 } } },
        { ".bmp", new List<byte[]> { new byte[] { 0x42, 0x4D } } },
        { ".webp", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 } } },
        
        // PDF
        { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
        
        // Документы
        { ".doc", new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } },
        { ".docx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
        { ".xls", new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } },
        { ".xlsx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
        
        // Архивы
        { ".zip", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
        { ".rar", new List<byte[]> { new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 } } },
        
        // Видео
        { ".mp4", new List<byte[]> { new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 } } },
        { ".avi", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 } } },
        { ".mov", new List<byte[]> { new byte[] { 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70 } } },
        
        // Аудио
        { ".mp3", new List<byte[]> { new byte[] { 0xFF, 0xFB }, new byte[] { 0xFF, 0xF3 }, new byte[] { 0xFF, 0xF2 } } },
        { ".wav", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 } } },
        
        // Текстовые файлы (нет фиксированной сигнатуры)
        { ".txt", new List<byte[]> { } },
        { ".csv", new List<byte[]> { } },
        { ".json", new List<byte[]> { } },
        { ".xml", new List<byte[]> { } }
    };

    /// <summary>
    /// Валидирует поток файла на соответствие расширению по Magic Bytes.
    /// </summary>
    public static bool ValidateStream(Stream fileStream, string extension)
    {
        if (string.IsNullOrEmpty(extension)) 
            return false;
        
        extension = extension.ToLowerInvariant();
        
        // Если расширения нет в словаре, разрешаем (можно изменить на false для строгой проверки)
        if (!_fileSignatures.ContainsKey(extension))
        {
            // Для отладки можно раскомментировать:
            // Console.WriteLine($"Расширение {extension} не найдено в списке сигнатур. Пропускаем проверку.");
            return true;
        }

        var signatures = _fileSignatures[extension];
        
        // Если для этого расширения нет требований к сигнатуре, разрешаем
        if (signatures.Count == 0)
        {
            return true;
        }

        // Находим максимальную длину сигнатуры
        var maxHeaderBytes = signatures.Max(m => m.Length);
        
        fileStream.Position = 0;
        var headerBytes = new byte[maxHeaderBytes];
        var bytesRead = fileStream.Read(headerBytes, 0, maxHeaderBytes);
        
        // Возвращаем позицию потока обратно
        fileStream.Position = 0;

        if (bytesRead < maxHeaderBytes)
        {
            return false;
        }

        // Проверяем, совпадает ли начало файла с одной из допустимых сигнатур
        return signatures.Any(signature => 
        {
            if (signature.Length == 0 || bytesRead < signature.Length)
                return false;
            
            return headerBytes.Take(signature.Length).SequenceEqual(signature);
        });
    }
}
