using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Create;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.DTOs.Update;
using IceBreakerApp.Application.IServices;
using Microsoft.AspNetCore.JsonPatch;

namespace IceBreakerApp.Application.Services;

// Временная заглушка для фокуса на JWT
public class MockQuestionService : IQuestionService
{
    public Task<QuestionResponseDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<QuestionResponseDTO?>(null);
    }

    public Task<PaginatedResult<QuestionResponseDTO>> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        string? sortOrder = null,
        string? search = null,
        Guid? topicId = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(new PaginatedResult<QuestionResponseDTO>(
            new List<QuestionResponseDTO>(), 0, pageNumber, pageSize));
    }

    public Task<QuestionResponseDTO> CreateAsync(CreateQuestionDTO dto, CancellationToken ct = default)
    {
        return Task.FromResult(new QuestionResponseDTO());
    }

    public Task UpdateAsync(Guid id, UpdateQuestionDTO dto, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task PatchAsync(Guid id, JsonPatchDocument<UpdateQuestionDTO> patchDoc, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<BulkOperationResult<QuestionResponseDTO>> BulkCreateAsync(
        List<CreateQuestionDTO> dtos, 
        CancellationToken ct = default)
    {
        return Task.FromResult(new BulkOperationResult<QuestionResponseDTO>());
    }
}