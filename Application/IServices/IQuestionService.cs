using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Create;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.DTOs.Update;
using Microsoft.AspNetCore.JsonPatch;

namespace IceBreakerApp.Application.IServices;

public interface IQuestionService
{
    Task<QuestionResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaginatedResult<QuestionResponseDto>> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortOrder,
        string? search,
        Guid? topicId,
        CancellationToken ct = default);
    Task<QuestionResponseDto> CreateAsync(CreateQuestionDTO dto, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpdateQuestionDTO dto, CancellationToken ct = default);
    Task PatchAsync(Guid id, JsonPatchDocument<UpdateQuestionDTO> patchDoc, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<BulkOperationResult<QuestionResponseDto>> BulkCreateAsync(
        List<CreateQuestionDTO> dtos, 
        CancellationToken ct = default);
}
