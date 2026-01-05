
namespace IceBreakerApp.Application.IServices;

public interface IFileStorageSettings
{
    // Основные пути (нужны для FileService)
    string StoragePath { get; }
    string FilesPath { get; }
    string TempPath { get; }
    string ThumbnailsPath { get; }
    
    // Ограничения (из ТЗ)
    long MaxFileSize { get; } // 50MB
    long MaxMultipleFileSize { get; } // 100MB
    int MaxFilesPerUpload { get; } // 10
    
    // Структура хранения (из ТЗ: "ИЛИ по UserId")
    bool UseUserIdStructure { get; }
    
    // Проверка типов файлов (из ТЗ)
    bool IsAllowedFileType(string contentType, string fileName);
    
    // Для изображений (из ТЗ)
    bool IsImageType(string contentType);
    bool IsImageExtension(string extension);
    
    // Вспомогательные методы для работы с путями и файловой системой
    string GetDateStructuredPath(DateTime date);
    string GetFullFilePath(string relativePath, string fileName);
    string GetFullThumbnailPath(string relativeThumbPath);
    string PrepareFilePath(DateTime uploadDate, string fileName);
    string PrepareThumbnailPath(string relativePath, string size, string thumbnailName);
    string GetRelativePathFromFiles(string fullPath);
    void EnsureStorageDirectoriesExist();
    void DeleteFileAndThumbnails(string relativeFilePath, string fileName, string? smallThumbPath, string? mediumThumbPath);
}