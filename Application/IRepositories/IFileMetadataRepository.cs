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
   Task SaveChangesAsync();

   Task<PaginatedResult<FileMetadata>> GetPaginatedAsync(
       Expression<Func<FileMetadata, bool>>? filter,
       Expression<Func<FileMetadata, object>>? orderBy,
       bool descending,
       int pageNumber,
       int pageSize);
}
