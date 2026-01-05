namespace IceBreakerApp.Application.DTOs;

public class FileStorageSettingsDto
{
    public string StoragePath { get; set; } = "Storage";
    public string FilesPath => Path.Combine(StoragePath, "Files");
    public string TempPath => Path.Combine(StoragePath, "Temp");
    public string ThumbnailsPath => Path.Combine(StoragePath, "Thumbnails");
    
    public long MaxFileSize { get; set; } = 50 * 1024 * 1024;
    public int MaxFilesPerUpload { get; set; } = 10;
}