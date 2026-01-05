using IceBreakerApp.Application.IServices;

namespace Infrastructure.Data;

public class StorageSettings : IFileStorageSettings
{
    public StorageSettings()
    {
        EnsureStorageDirectoriesExist();
    }
    public string StoragePath { get; set; } = "Storage";

    public string FilesPath => Path.Combine(StoragePath, "Files");
    public string TempPath => Path.Combine(StoragePath, "Temp");
    public string ThumbnailsPath => Path.Combine(StoragePath, "Thumbnails");

    public long MaxFileSize { get; set; } = 50 * 1024 * 1024; // 50MB
    public long MaxMultipleFileSize { get; set; } = 100 * 1024 * 1024; // 100MB
    public int MaxFilesPerUpload { get; set; } = 10; // 10
    public bool UseUserIdStructure { get; set; } = false;

    public string GetFullFilePath(string relativePath, string fileName)
    {
        return Path.Combine(FilesPath, relativePath, fileName);
    }

    public string GetFullThumbnailPath(string relativeThumbPath)
    {
        return Path.Combine(ThumbnailsPath, relativeThumbPath);
    }

    public bool IsImageExtension(string extension)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        return imageExtensions.Contains(extension.ToLowerInvariant());
    }

    public string GetDateStructuredPath(DateTime date)
    {
        return $"{date:yyyy}/{date:MM}/{date:dd}/";
    }

    public void EnsureStorageDirectoriesExist()
    {
        Directory.CreateDirectory(StoragePath);
        Directory.CreateDirectory(FilesPath);
        Directory.CreateDirectory(TempPath);
        Directory.CreateDirectory(ThumbnailsPath);

        Directory.CreateDirectory(Path.Combine(ThumbnailsPath, "small"));
        Directory.CreateDirectory(Path.Combine(ThumbnailsPath, "medium"));
    }

    public string PrepareFilePath(DateTime uploadDate, string fileName)
    {
        var relativePath = GetDateStructuredPath(uploadDate);
        var fullPath = Path.Combine(FilesPath, relativePath);
        Directory.CreateDirectory(fullPath);
        return Path.Combine(fullPath, fileName);
    }

    public string PrepareThumbnailPath(string relativePath, string size, string thumbnailName)
    {
        var thumbDir = Path.Combine(ThumbnailsPath, relativePath, size);
        Directory.CreateDirectory(thumbDir);
        return Path.Combine(thumbDir, thumbnailName);
    }

    public void DeleteFileAndThumbnails(string relativeFilePath, string fileName, 
                                        string? smallThumbPath, string? mediumThumbPath)
    {
        try
        {
            var mainFilePath = Path.Combine(FilesPath, relativeFilePath, fileName);
            if (File.Exists(mainFilePath))
            {
                File.Delete(mainFilePath);
            }

            if (!string.IsNullOrEmpty(smallThumbPath))
            {
                var smallThumbFullPath = Path.Combine(ThumbnailsPath, smallThumbPath);
                if (File.Exists(smallThumbFullPath))
                {
                    File.Delete(smallThumbFullPath);
                }
            }

            if (!string.IsNullOrEmpty(mediumThumbPath))
            {
                var mediumThumbFullPath = Path.Combine(ThumbnailsPath, mediumThumbPath);
                if (File.Exists(mediumThumbFullPath))
                {
                    File.Delete(mediumThumbFullPath);
                }
            }

            var dirPath = Path.Combine(FilesPath, relativeFilePath);
            if (Directory.Exists(dirPath) && !Directory.EnumerateFileSystemEntries(dirPath).Any())
            {
                Directory.Delete(dirPath);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error deleting file: {ex.Message}", ex);
        }
    }

    public string GetRelativePathFromFiles(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return string.Empty;

        var filesPath = FilesPath;
        if (fullPath.StartsWith(filesPath))
        {
            return fullPath.Substring(filesPath.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        return fullPath;
    }

    public bool IsAllowedFileType(string contentType, string fileName)
    {
        return FileSignature.IsAllowedExtension(fileName, contentType);
    }

    public bool IsImageType(string contentType)
    {
        return contentType.StartsWith("image/");
    }
}