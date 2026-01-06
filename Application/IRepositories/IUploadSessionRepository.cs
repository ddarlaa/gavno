// Application/IRepositories/IUploadSessionRepository.cs

using IceBreakerApp.Domain.Models;

public interface IUploadSessionRepository
{
    Task<UploadSession?> GetByUploadIdAsync(Guid uploadId);
    Task<UploadSession?> GetByUploadIdWithFileAsync(Guid uploadId);
    Task AddAsync(UploadSession session);
    Task UpdateAsync(UploadSession session);
    Task<bool> ExistsAsync(Guid uploadId);
    Task SaveChangesAsync();
    Task ReloadSessionAsync(UploadSession session);
}
