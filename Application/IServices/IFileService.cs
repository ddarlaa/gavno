using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Domain;
using Microsoft.AspNetCore.Http;

namespace IceBreakerApp.Application.IServices;

public interface IFileService
{
    // Загрузка (по ТЗ)
    Task<FileMetadata> UploadAsync(
        IFormFile file, 
        Guid userId, 
        bool isPublic = false, 
        DateTime? expiresAt = null);
    
    Task<List<FileMetadata>> UploadMultipleAsync(
        List<IFormFile> files, 
        Guid userId, 
        bool isPublic = false, 
        DateTime? expiresAt = null);
    
    // Получение (по ТЗ)
    Task<FileMetadata> GetAsync(Guid id);
    Task<FileMetadata> GetInfoAsync(Guid id, Guid currentUserId, bool isAdmin);
    Task<Stream> GetFileStreamAsync(Guid id);
    Task IncrementDownloadCountAsync(Guid id);
    
    // Thumbnails (по ТЗ)
    Task<string> GenerateThumbnailAsync(Guid id, string size);
    
    // Список с пагинацией (по ТЗ)
    Task<PaginatedResult<FileMetadata>> GetFilesAsync(Guid userId,
        bool isAdmin,
        int page,
        int pageSize,
        string? contentTypeFilter,
        string? search,
        string sortBy,
        bool sortDescending);
    
    // Удаление (по ТЗ: soft delete ИЛИ физическое)
    Task DeleteAsync(Guid id, Guid currentUserId, bool isAdmin); // Soft delete
    Task HardDeleteAsync(Guid id, Guid currentUserId, bool isAdmin); // Физическое
    Task<string> GetThumbnailPathAsync(Guid id, string size);
}