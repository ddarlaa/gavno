using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain;
using IceBreakerApp.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application.Services;

public class ChunkedFileService(
    IFileMetadataRepository fileMetadataRepository,
    IUploadSessionRepository uploadSessionRepository,
    IFileService fileService,
    IFileStorageSettings storageSettings,
    ILogger<ChunkedFileService> logger)
    : IChunkedFileService
{
    private readonly IFileMetadataRepository _fileMetadataRepository = fileMetadataRepository;

    public async Task<UploadProgressResponse> UploadChunkAsync(ChunkUploadRequest request, Guid userId)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // 1. Сохраняем chunk на диск
        var tempDir = Path.Combine(storageSettings.TempPath, request.UploadId);
        Directory.CreateDirectory(tempDir);

        var chunkPath = Path.Combine(tempDir, $"chunk_{request.ChunkIndex}");

        await using (var stream = new FileStream(chunkPath, FileMode.Create))
        {
            await request.Chunk.CopyToAsync(stream);
        }

        // 2. Получаем или создаем сессию
        var session = await uploadSessionRepository.GetByUploadIdAsync(request.UploadId);

        if (session == null)
        {
            session = new UploadSession
            {
                UploadId = request.UploadId,
                FileName = request.FileName,
                ContentType = request.ContentType,
                TotalChunks = request.TotalChunks,
                UploadedChunks = 0,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                UserId = userId
            };
            await uploadSessionRepository.AddAsync(session);
        }

        // 3. Обновляем счетчик (только если chunk еще не был загружен)
        if (!File.Exists(Path.Combine(tempDir, $"chunk_{request.ChunkIndex}")))
        {
            session.UploadedChunks++;
        }
        
        session.LastActivity = DateTime.UtcNow;
        await uploadSessionRepository.UpdateAsync(session);
        await uploadSessionRepository.SaveChangesAsync();

        // 4. Проверяем завершение
        var isComplete = session.UploadedChunks == session.TotalChunks;

        return new UploadProgressResponse
        {
            UploadId = request.UploadId,
            UploadedChunks = session.UploadedChunks,
            TotalChunks = session.TotalChunks,
            Percentage = (double)session.UploadedChunks / session.TotalChunks * 100,
            IsComplete = isComplete,
            File = isComplete ? await FinalizeUploadAsync(request.UploadId) : null
        };
    }

    public async Task<bool> IsUploadComplete(string uploadId)
    {
        var session = await uploadSessionRepository.GetByUploadIdAsync(uploadId);
        return session?.UploadedChunks == session?.TotalChunks;
    }

    public async Task<FileMetadata> FinalizeUploadAsync(string uploadId)
    {
        var session = await uploadSessionRepository.GetByUploadIdAsync(uploadId);

        if (session == null || session.UploadedChunks != session.TotalChunks)
            throw new InvalidOperationException("Upload not complete");

        var tempDir = Path.Combine(storageSettings.TempPath, uploadId);
        var finalPath = Path.Combine(tempDir, "final.bin");

        // 1. Проверяем что все chunks существуют
        for (int i = 0; i < session.TotalChunks; i++)
        {
            var chunkPath = Path.Combine(tempDir, $"chunk_{i}");
            if (!File.Exists(chunkPath))
                throw new FileNotFoundException($"Chunk {i} not found");
        }

        // 2. Склеиваем chunks
        await using (var finalStream = new FileStream(finalPath, FileMode.Create))
        {
            for (int i = 0; i < session.TotalChunks; i++)
            {
                var chunkPath = Path.Combine(tempDir, $"chunk_{i}");
                await using (var chunkStream = new FileStream(chunkPath, FileMode.Open))
                {
                    await chunkStream.CopyToAsync(finalStream);
                }
            }
        }

        // 3. Создаем IFormFile
        await using var fileStream = new FileStream(finalPath, FileMode.Open);
        var fileInfo = new FileInfo(finalPath);
        
        var formFile = new FormFile(
            baseStream: fileStream,
            baseStreamOffset: 0,
            length: fileInfo.Length,
            name: "file",
            fileName: session.FileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = session.ContentType
        };

        // 4. Загружаем через FileService
        var fileMetadata = await fileService.UploadAsync(formFile, session.UserId);

        // 5. Обновляем сессию
        session.FileId = fileMetadata.Id;
        await uploadSessionRepository.UpdateAsync(session);
        await uploadSessionRepository.SaveChangesAsync();

        // 6. Очищаем временные файлы
        try
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, $"Failed to cleanup temp directory: {tempDir}");
        }

        return fileMetadata;
    }

    public async Task<UploadProgressResponse> GetProgressAsync(string uploadId)
    {
        var session = await uploadSessionRepository.GetByUploadIdAsync(uploadId);
        
        if (session == null)
            throw new KeyNotFoundException($"Upload session {uploadId} not found");

        return new UploadProgressResponse
        {
            UploadId = uploadId,
            UploadedChunks = session.UploadedChunks,
            TotalChunks = session.TotalChunks,
            Percentage = (double)session.UploadedChunks / session.TotalChunks * 100,
            IsComplete = session.UploadedChunks == session.TotalChunks,
            FileId = session.FileId
        };
    }
}