using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.DTOs.Update;
using IceBreakerApp.Application.IServices;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application.Services
{
    // Временная заглушка для фокуса на JWT
    public class QuestionAnswerService : IQuestionAnswerService
    {
        private readonly ILogger<QuestionAnswerService> _logger;

        public QuestionAnswerService(ILogger<QuestionAnswerService> logger)
        {
            _logger = logger;
        }

        public Task<QuestionAnswerResponseDTO?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            _logger.LogWarning("QuestionAnswerService is temporarily disabled for JWT focus");
            return Task.FromResult<QuestionAnswerResponseDTO?>(null);
        }

        public Task<PaginatedResult<QuestionAnswerResponseDTO>> GetAllAsync(
            int pageNumber,
            int pageSize,
            Guid? questionId = null,
            Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("QuestionAnswerService is temporarily disabled for JWT focus");
            return Task.FromResult(new PaginatedResult<QuestionAnswerResponseDTO>(
                new List<QuestionAnswerResponseDTO>(), 0, pageNumber, pageSize));
        }

        public Task<QuestionAnswerResponseDTO> CreateAsync(CreateQuestionAnswerDTO dto,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("QuestionAnswerService is temporarily disabled for JWT focus");
            return Task.FromResult(new QuestionAnswerResponseDTO());
        }

        public Task<List<QuestionAnswerResponseDTO>> BulkCreateAsync(IEnumerable<CreateQuestionAnswerDTO> dtos,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("QuestionAnswerService is temporarily disabled for JWT focus");
            return Task.FromResult(new List<QuestionAnswerResponseDTO>());
        }

        public Task UpdateAsync(Guid id, UpdateQuestionAnswerDTO dto, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("QuestionAnswerService is temporarily disabled for JWT focus");
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("QuestionAnswerService is temporarily disabled for JWT focus");
            return Task.CompletedTask;
        }

        public Task AcceptAsync(Guid answerId, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("QuestionAnswerService is temporarily disabled for JWT focus");
            return Task.CompletedTask;
        }

        public Task<QuestionAnswerResponseDTO> GetAcceptedAsync(Guid questionId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("QuestionAnswerService is temporarily disabled for JWT focus");
            return Task.FromResult(new QuestionAnswerResponseDTO());
        }
    }
}