using IceBreakerApp.Domain;
using System.Linq.Expressions;
using IceBreakerApp.Application.DTOs;

namespace IceBreakerApp.Application.IRepositories;

public interface IFileMetadataRepository
{
    Task<FileMetadata?> GetByIdAsync(Guid id);
    Task AddAsync(FileMetadata metadata);
    Task UpdateAsync(FileMetadata metadata);
    Task DeleteAsync(FileMetadata metadata);
    Task<FileMetadata?> FindByHashAsync(string hash);
    Task<PaginatedResult<FileMetadata>> GetPaginatedFilesAsync(Expression<Func<FileMetadata, bool>>? filter = null, Expression<Func<FileMetadata, object>>? orderBy = null, bool descending = false, int pageNumber = 1, int pageSize = 10);
    Task SaveChangesAsync();

    Task<PaginatedResult<FileMetadata>> GetPaginatedAsync(Expression<Func<FileMetadata, bool>> filter, Expression<Func<FileMetadata, object>> orderBy, string sortDescending, int page, int pageSize);
}
