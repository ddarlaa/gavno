using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

// Добавлено

namespace Infrastructure.Repositories;

public class UploadSessionRepository : IUploadSessionRepository
{
    private readonly ApplicationDbContext _context;

    public UploadSessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UploadSession?> GetByUploadIdAsync(Guid uploadId)
    {
        return await _context.ChunkUploadSessions
            .FirstOrDefaultAsync(s => s.UploadId == uploadId);
    }

    public async Task<UploadSession?> GetByUploadIdWithFileAsync(Guid uploadId)
    {
        return await _context.ChunkUploadSessions
            .Include(s => s.File)
            .FirstOrDefaultAsync(s => s.UploadId == uploadId);
    }

    public async Task AddAsync(UploadSession session)
    {
        await _context.ChunkUploadSessions.AddAsync(session);
    }

    public async Task UpdateAsync(UploadSession session)
    {
        _context.ChunkUploadSessions.Update(session);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid uploadId)
    {
        return await _context.ChunkUploadSessions
            .AnyAsync(s => s.UploadId == uploadId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task ReloadSessionAsync(UploadSession session)
    {
        await _context.Entry(session).ReloadAsync();
    }
}