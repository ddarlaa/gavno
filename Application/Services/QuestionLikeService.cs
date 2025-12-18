using IceBreakerApp.Application.IServices;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application.Services
{
    // Временная заглушка для фокуса на JWT
    public class QuestionLikeService : IQuestionLikeService
    {
        private readonly ILogger<QuestionLikeService> _logger;

        public QuestionLikeService(ILogger<QuestionLikeService> logger)
        {
            _logger = logger;
        }

        public Task<bool> AddLikeAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("QuestionLikeService is temporarily disabled for JWT focus");
            return Task.FromResult(true);
        }

        public Task<bool> RemoveLikeAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("QuestionLikeService is temporarily disabled for JWT focus");
            return Task.FromResult(true);
        }

        public Task<int> GetLikeCountAsync(Guid questionId, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("QuestionLikeService is temporarily disabled for JWT focus");
            return Task.FromResult(0);
        }

        public Task<int> GetUserLikeCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("QuestionLikeService is temporarily disabled for JWT focus");
            return Task.FromResult(0);
        }

        public Task<bool> HasUserLikedAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("QuestionLikeService is temporarily disabled for JWT focus");
            return Task.FromResult(false);
        }

        public Task<IEnumerable<Guid>> GetUserLikedQuestionIdsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("QuestionLikeService is temporarily disabled for JWT focus");
            return Task.FromResult<IEnumerable<Guid>>(new List<Guid>());
        }
    }
}