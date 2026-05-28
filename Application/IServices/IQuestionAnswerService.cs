using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Create;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.DTOs.Update;

namespace IceBreakerApp.Application.IServices;

public interface IQuestionAnswerService
{
    public Task<QuestionAnswerResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    public Task<PaginatedResult<QuestionAnswerResponseDto>> GetAllAsync(
        int pageNumber,
        int pageSize,
        Guid? questionId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    public Task<QuestionAnswerResponseDto> CreateAsync(CreateQuestionAnswerDTO dto,
        CancellationToken cancellationToken = default);

    public Task<List<QuestionAnswerResponseDto>> BulkCreateAsync(IEnumerable<CreateQuestionAnswerDTO> dtos,
        CancellationToken cancellationToken = default);

    public Task UpdateAsync(Guid id, UpdateQuestionAnswerDTO dto, CancellationToken cancellationToken = default);
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    public Task AcceptAsync(Guid answerId, CancellationToken cancellationToken = default);
    public Task<QuestionAnswerResponseDto> GetAcceptedAsync(Guid questionId,
        CancellationToken cancellationToken = default);
    
    Task<int> GetAnswerCountAsync(Guid questionId, CancellationToken cancellationToken = default);
}
