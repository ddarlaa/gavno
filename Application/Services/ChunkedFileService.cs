using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain;
using IceBreakerApp.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
    private const int MaxRetries = 5;

    public async Task<UploadProgressResponse> UploadChunkAsync(ChunkUploadRequest request, Guid userId)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        // 1. Сохраняем chunk на диск
        var tempDir = Path.Combine(storageSettings.TempPath, request.UploadId.ToString());
        Directory.CreateDirectory(tempDir);
        var chunkPath = Path.Combine(tempDir, $"chunk_{request.ChunkIndex}");

        await using (var stream = new FileStream(chunkPath, FileMode.Create))
        {
            await request.Chunk.CopyToAsync(stream);
        }

        int retries = 0;
        while (true)
        {
            UploadSession? session = null;
            try
            {
                session = await uploadSessionRepository.GetByUploadIdAsync(request.UploadId);

                if (session == null)
                {
                    // ВАЖНО: Сохраняем настройки приватности из первого пришедшего чанка
                    // Убедитесь, что ChunkUploadRequest содержит IsPublic и ExpiresAt
                    session = new UploadSession
                    {
                        UploadId = request.UploadId,
                        FileName = request.FileName,
                        ContentType = request.ContentType,
                        TotalChunks = request.TotalChunks,
                        UploadedChunks = 0,
                        CreatedAt = DateTime.UtcNow,
                        UserId = userId,
                        UploadedChunkIndexes = JsonSerializer.Serialize(new HashSet<int>()),
                        
                        // !!! Добавляем сохранение метаданных !!!
                        IsPublic = request.IsPublic, 
                        ExpiresAt = request.ExpiresAt
                    };
                    await uploadSessionRepository.AddAsync(session);
                    await uploadSessionRepository.SaveChangesAsync();
                }

                var uploadedChunkIndexes = JsonSerializer.Deserialize<HashSet<int>>(session.UploadedChunkIndexes) ?? new HashSet<int>();

                if (uploadedChunkIndexes.Add(request.ChunkIndex))
                {
                    session.UploadedChunks++;
                    session.UploadedChunkIndexes = JsonSerializer.Serialize(uploadedChunkIndexes);
                }
                else
                {
                    logger.LogInformation($"Chunk {request.ChunkIndex} for upload {request.UploadId} already processed.");
                    // Если завершено - финализируем, иначе просто возвращаем статус
                    return new UploadProgressResponse
                    {
                        UploadId = request.UploadId,
                        UploadedChunks = session.UploadedChunks,
                        TotalChunks = session.TotalChunks,
                        Percentage = (double)session.UploadedChunks / session.TotalChunks * 100,
                        IsComplete = session.UploadedChunks == session.TotalChunks,
                        // Если это был последний чанк (даже повторный), пробуем финализировать
                        File = session.UploadedChunks == session.TotalChunks 
                            ? await FinalizeUploadAsync(request.UploadId) 
                            : null
                    };
                }

                await uploadSessionRepository.UpdateAsync(session);
                await uploadSessionRepository.SaveChangesAsync();

                var complete = session.UploadedChunks == session.TotalChunks;
                return new UploadProgressResponse
                {
                    UploadId = request.UploadId,
                    UploadedChunks = session.UploadedChunks,
                    TotalChunks = session.TotalChunks,
                    Percentage = (double)session.UploadedChunks / session.TotalChunks * 100,
                    IsComplete = complete,
                    File = complete ? await FinalizeUploadAsync(request.UploadId) : null
                };
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, $"Concurrency conflict for session {request.UploadId}. Retry {retries + 1}/{MaxRetries}");
                retries++;
                if (retries >= MaxRetries) throw;
                if (session != null) await uploadSessionRepository.ReloadSessionAsync(session);
            }
        }
    }

    public async Task<FileMetadata> FinalizeUploadAsync(Guid uploadId)
    {
        var session = await uploadSessionRepository.GetByUploadIdAsync(uploadId);
        if (session == null || session.UploadedChunks != session.TotalChunks)
            throw new InvalidOperationException("Upload not complete");

        var tempDir = Path.Combine(storageSettings.TempPath, uploadId.ToString());
        var finalPath = Path.Combine(tempDir, "final.bin");
        FileMetadata fileMetadata;

        try 
        {
            // 1. Склеиваем chunks
            // Используем FileMode.Create, чтобы перезаписать, если вдруг файл уже есть (например, повтор операции)
            await using (var finalStream = new FileStream(finalPath, FileMode.Create))
            {
                for (int i = 0; i < session.TotalChunks; i++)
                {
                    var chunkPath = Path.Combine(tempDir, $"chunk_{i}");
                    if (!File.Exists(chunkPath)) throw new FileNotFoundException($"Chunk {i} not found");

                    await using (var chunkStream = new FileStream(chunkPath, FileMode.Open))
                    {
                        await chunkStream.CopyToAsync(finalStream);
                    }
                }
            } // !!! finalStream закрывается здесь !!!

            // 2. Загружаем через FileService
            // Открываем поток снова только для чтения
            await using (var readStream = new FileStream(finalPath, FileMode.Open))
            {
                var formFile = new FormFile(
                    baseStream: readStream,
                    baseStreamOffset: 0,
                    length: readStream.Length,
                    name: "file",
                    fileName: session.FileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = session.ContentType
                };

                // !!! Передаем сохраненные IsPublic и ExpiresAt !!!
                fileMetadata = await fileService.UploadAsync(
                    formFile, 
                    session.UserId, 
                    session.IsPublic, // Добавлено в модель сессии
                    session.ExpiresAt // Добавлено в модель сессии
                );
            } // !!! readStream закрывается здесь !!!

            // 3. Обновляем сессию
            session.FileId = fileMetadata.Id;
            await uploadSessionRepository.UpdateAsync(session);
            await uploadSessionRepository.SaveChangesAsync();
        }
        catch (Exception)
        {
            // Если что-то пошло не так, можно логгировать
            throw;
        }

        // 4. Очищаем временные файлы
        // Теперь это безопасно, так как все потоки (streams) закрыты
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
    
    // ... GetProgressAsync и IsUploadComplete без изменений ...
    public async Task<bool> IsUploadComplete(Guid uploadId)
    {
        var session = await uploadSessionRepository.GetByUploadIdAsync(uploadId);
        return session?.UploadedChunks == session?.TotalChunks;
    }

    public async Task<UploadProgressResponse> GetProgressAsync(Guid uploadId)
    {
        var session = await uploadSessionRepository.GetByUploadIdAsync(uploadId);
        if (session == null) throw new KeyNotFoundException($"Upload session {uploadId} not found");

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