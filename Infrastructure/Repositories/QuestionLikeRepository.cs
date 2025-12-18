using IceBreakerApp.Application.IRepositories;
using Microsoft.EntityFrameworkCore;
using IceBreakerApp.Domain;
using IceBreakerApp.Domain.IRepositories;
using IceBreakerApp.Domain.Models;

namespace Infrastructure.Repositories
{
    public class QuestionLikeRepository(ApplicationDbContext context) : IQuestionLikeRepository
    {
        public async Task<QuestionLike?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await context.QuestionLikes
                .FirstOrDefaultAsync(like => like.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<QuestionLike>> GetByQuestionIdAsync(Guid questionId, CancellationToken cancellationToken)
        {
            return await context.QuestionLikes
                .Where(like => like.QuestionId == questionId)
                .OrderByDescending(like => like.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<QuestionLike>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await context.QuestionLikes
                .Where(like => like.UserId == userId)
                .OrderByDescending(like => like.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid questionId, Guid userId, CancellationToken cancellationToken)
        {
            return await context.QuestionLikes
                .AnyAsync(like => like.QuestionId == questionId && like.UserId == userId, cancellationToken);
        }

        public async Task<QuestionLike> AddAsync(QuestionLike like, CancellationToken cancellationToken)
        {
            // Генерируем ID если не установлен
            if (like.Id == Guid.Empty)
                like.Id = Guid.NewGuid();
                
            // Устанавливаем время создания
            like.CreatedAt = DateTime.UtcNow;
            
            context.QuestionLikes.Add(like);
            await context.SaveChangesAsync(cancellationToken);
            return like;
        }

        public async Task DeleteByQuestionAndUserAsync(Guid questionId, Guid userId, CancellationToken cancellationToken)
        {
            var likeToRemove = await context.QuestionLikes
                .FirstOrDefaultAsync(like => like.QuestionId == questionId && like.UserId == userId, cancellationToken);
                
            if (likeToRemove != null)
            {
                context.QuestionLikes.Remove(likeToRemove);
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<int> GetCountByQuestionIdAsync(Guid questionId, CancellationToken cancellationToken)
        {
            return await context.QuestionLikes
                .CountAsync(like => like.QuestionId == questionId, cancellationToken);
        }

        public async Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await context.QuestionLikes
                .CountAsync(like => like.UserId == userId, cancellationToken);
        }

        public async Task<QuestionLike?> GetByQuestionAndUserAsync(Guid questionId, Guid userId, CancellationToken cancellationToken)
        {
            return await context.QuestionLikes
                .FirstOrDefaultAsync(like => like.QuestionId == questionId && like.UserId == userId, cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var likeToRemove = await GetByIdAsync(id, cancellationToken);
            
            if (likeToRemove != null)
            {
                context.QuestionLikes.Remove(likeToRemove);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}