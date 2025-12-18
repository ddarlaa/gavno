using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain;
using IceBreakerApp.Domain.IRepositories;
using IceBreakerApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class QuestionRepository(ApplicationDbContext context) : IQuestionRepository
    {
        public async Task<Question?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await context.Questions
                .FirstOrDefaultAsync(q => q.Id == id && q.IsActive, ct);
        }

        public async Task<IEnumerable<Question>> GetAllAsync(CancellationToken ct = default)
        {
            return await context.Questions
                .Where(q => q.IsActive)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<PaginatedResult<Question>> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            string? sortBy,
            string? sortOrder,
            string? search,
            Guid? topicId,
            CancellationToken ct = default)
        {
            var query = context.Questions
                .Where(q => q.IsActive)
                .AsQueryable();

            // Apply filtering
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(q =>
                    q.Title.Contains(search) ||
                    q.Content.Contains(search));
            }

            if (topicId.HasValue)
            {
                query = query.Where(q => q.TopicId == topicId.Value);
            }

            // Apply sorting
            query = ApplySorting(query, sortBy, sortOrder);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PaginatedResult<Question>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<Question> AddAsync(Question question, CancellationToken ct = default)
        {
            context.Questions.Add(question);
            await context.SaveChangesAsync(ct);
            return question;
        }

        public async Task UpdateAsync(Question question, CancellationToken ct = default)
        {
            context.Questions.Update(question);
            await context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var question = await GetByIdAsync(id, ct);
            if (question != null)
            {
                question.Delete();
                await UpdateAsync(question, ct);
            }
        }

        private static IQueryable<Question> ApplySorting(IQueryable<Question> questions, string? sortBy, string? sortOrder)
        {
            var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            return (sortBy?.ToLower()) switch
            {
                "title" => isDescending ? questions.OrderByDescending(q => q.Title) : questions.OrderBy(q => q.Title),
                "createdat" => isDescending ? questions.OrderByDescending(q => q.CreatedAt) : questions.OrderBy(q => q.CreatedAt),
                "likecount" => isDescending ? questions.OrderByDescending(q => q.LikeCount) : questions.OrderBy(q => q.LikeCount),
                "viewcount" => isDescending ? questions.OrderByDescending(q => q.ViewCount) : questions.OrderBy(q => q.ViewCount),
                _ => questions.OrderByDescending(q => q.CreatedAt)
            };
        }
    }
}