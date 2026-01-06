using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Domain;

namespace IceBreakerApp.Application.IServices;

public interface IChunkedFileService
{
     public Task<UploadProgressResponse> UploadChunkAsync(ChunkUploadRequest request, Guid userId);
    public Task<bool> IsUploadComplete(Guid uploadId);
    public Task<FileMetadata> FinalizeUploadAsync(Guid uploadId);
    public Task<UploadProgressResponse> GetProgressAsync(Guid uploadId);
}
