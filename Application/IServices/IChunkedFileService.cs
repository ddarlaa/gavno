using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Domain;

namespace IceBreakerApp.Application.IServices;

public interface IChunkedFileService
{
    Task<UploadProgressResponse> UploadChunkAsync(ChunkUploadRequest request, Guid userId);
    Task<bool> IsUploadComplete(string uploadId);
    Task<FileMetadata> FinalizeUploadAsync(string uploadId);
    Task<UploadProgressResponse> GetProgressAsync(string uploadId);
}