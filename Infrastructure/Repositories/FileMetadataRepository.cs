using System.Linq.Expressions;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class FileMetadataRepository : IFileMetadataRepository
{
    private readonly ApplicationDbContext _context;

    public FileMetadataRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FileMetadata?> GetByIdAsync(Guid id)
    {
        return await _context.FileMetadata.FindAsync(id);
    }

    public async Task AddAsync(FileMetadata metadata)
    {
        await _context.FileMetadata.AddAsync(metadata);
    }

    public async Task UpdateAsync(FileMetadata metadata)
    {
        _context.FileMetadata.Update(metadata);
    }

    public async Task DeleteAsync(FileMetadata metadata)
    {
        _context.FileMetadata.Remove(metadata);
    }

    public async Task<FileMetadata?> FindByHashAsync(string hash)
    {
        return await _context.FileMetadata.FirstOrDefaultAsync(f => f.Hash == hash);
    }

    public async Task<PaginatedResult<FileMetadata>> GetPaginatedFilesAsync(
        Expression<Func<FileMetadata, bool>>? filter = null,
        Expression<Func<FileMetadata, object>>? orderBy = null,
        bool descending = false,
        int pageNumber = 1,
        int pageSize = 10)
    {
        IQueryable<FileMetadata> query = _context.FileMetadata;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (orderBy != null)
        {
            query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        }
        else
        {
            query = query.OrderBy(f => f.UploadedAt); // Default order
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<FileMetadata>(items, totalCount, pageNumber, pageSize);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
    
    public Task<PaginatedResult<FileMetadata>> GetPaginatedAsync(Expression<Func<FileMetadata, bool>> filter, Expression<Func<FileMetadata, object>> orderBy, string sortDescending, int page, int pageSize)
    {
        throw new NotImplementedException();
    }
}
