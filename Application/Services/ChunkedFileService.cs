using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain;
using IceBreakerApp.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore; // Добавлено для DbUpdateConcurrencyException
using System.Text.Json; // Добавлено для сериализации/десериализации

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

    private const int MaxRetries = 5; // Максимальное количество повторных попыток при конфликте параллелизма

    public async Task<UploadProgressResponse> UploadChunkAsync(ChunkUploadRequest request, Guid userId)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

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
            UploadSession? session = null; // Инициализируем session как null здесь
            try
            {
                // 2. Получаем или создаем сессию
                session = await uploadSessionRepository.GetByUploadIdAsync(request.UploadId);

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
                        UserId = userId,
                        UploadedChunkIndexes = JsonSerializer.Serialize(new HashSet<int>()) // Инициализация
                    };
                    await uploadSessionRepository.AddAsync(session);
                    await uploadSessionRepository.SaveChangesAsync(); // Сохраняем новую сессию
                }

                // Десериализуем индексы загруженных чанков
                var uploadedChunkIndexes = JsonSerializer.Deserialize<HashSet<int>>(session.UploadedChunkIndexes) ??
                                           new HashSet<int>();

                // 3. Обновляем счетчик (только если chunk еще не был загружен)
                if (uploadedChunkIndexes.Add(request
                        .ChunkIndex)) // Add возвращает true, если элемент был добавлен (т.е. его не было)
                {
                    session.UploadedChunks++;
                    session.UploadedChunkIndexes =
                        JsonSerializer.Serialize(uploadedChunkIndexes); // Сериализуем обратно
                }
                else
                {
                    // Чанк уже был загружен, возможно, повторный запрос. Просто возвращаем текущий прогресс.
                    logger.LogInformation(
                        $"Chunk {request.ChunkIndex} for upload {request.UploadId} already processed.");
                    // Возвращаем текущий прогресс, так как сессия не изменилась
                    return new UploadProgressResponse
                    {
                        UploadId = request.UploadId,
                        UploadedChunks = session.UploadedChunks,
                        TotalChunks = session.TotalChunks,
                        Percentage = (double)session.UploadedChunks / session.TotalChunks * 100,
                        IsComplete = session.UploadedChunks == session.TotalChunks,
                        File = session.UploadedChunks == session.TotalChunks
                            ? await FinalizeUploadAsync(request.UploadId)
                            : null
                    };
                }

                await uploadSessionRepository.UpdateAsync(session);
                await uploadSessionRepository.SaveChangesAsync();

                // 4. Проверяем завершение и возвращаем ответ
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
                logger.LogWarning(ex,
                    $"Concurrency conflict for upload session {request.UploadId}. Retrying... (Attempt {retries + 1}/{MaxRetries})");
                retries++;
                if (retries >= MaxRetries)
                {
                    logger.LogError(ex,
                        $"Failed to update upload session {request.UploadId} after {MaxRetries} retries due to concurrency conflict.");
                    throw; // Перебрасываем исключение после исчерпания попыток
                }

                // Перезагружаем сессию из базы данных, чтобы получить актуальные данные
                if (session != null)
                {
                    await uploadSessionRepository.ReloadSessionAsync(session);
                }
                else
                {
                    logger.LogError(
                        "Session was null during concurrency conflict retry. This indicates a logic error.");
                    throw;
                }
            }
        }
    }

    public async Task<bool> IsUploadComplete(Guid uploadId)
    {
        var session = await uploadSessionRepository.GetByUploadIdAsync(uploadId);
        return session?.UploadedChunks == session?.TotalChunks;
    }


    public async Task<FileMetadata> FinalizeUploadAsync(Guid uploadId)
    {
        var session = await uploadSessionRepository.GetByUploadIdAsync(uploadId);

        if (session == null || session.UploadedChunks != session.TotalChunks)
            throw new InvalidOperationException("Upload not complete");

        var tempDir = Path.Combine(storageSettings.TempPath, uploadId.ToString());
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



    public async Task<UploadProgressResponse> GetProgressAsync(Guid uploadId)
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