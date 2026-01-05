using IceBreakerApp.Domain.Models;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

public class UploadSessionRepository : IUploadSessionRepository
{
    private readonly ApplicationDbContext _context;

    public UploadSessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UploadSession?> GetByUploadIdAsync(string uploadId)
    {
        return await _context.ChunkUploadSessions
            .FirstOrDefaultAsync(s => s.UploadId == uploadId);
    }

    public async Task<UploadSession?> GetByUploadIdWithFileAsync(string uploadId)
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

    public async Task<bool> ExistsAsync(string uploadId)
    {
        return await _context.ChunkUploadSessions
            .AnyAsync(s => s.UploadId == uploadId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}