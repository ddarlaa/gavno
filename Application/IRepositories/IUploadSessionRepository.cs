// Application/IRepositories/IUploadSessionRepository.cs

using IceBreakerApp.Domain.Models;

public interface IUploadSessionRepository
{
    Task<UploadSession?> GetByUploadIdAsync(string uploadId);
    Task<UploadSession?> GetByUploadIdWithFileAsync(string uploadId);
    Task AddAsync(UploadSession session);
    Task UpdateAsync(UploadSession session);
    Task<bool> ExistsAsync(string uploadId);
    Task SaveChangesAsync();
}
