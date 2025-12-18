using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IServices;

namespace IceBreakerApp.Application.Services;

// Временная заглушка для фокуса на JWT
public class MockQuestionLikeService : IQuestionLikeService
{
    public Task<bool> AddLikeAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> RemoveLikeAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<int> GetLikeCountAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public Task<int> GetUserLikeCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public Task<bool> HasUserLikedAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<IEnumerable<Guid>> GetUserLikedQuestionIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Guid>>(new List<Guid>());
    }
}