using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class QuestionAnswerRepository(ApplicationDbContext context) : IQuestionAnswerRepository
    {
        public async Task<QuestionAnswer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await context.QuestionAnswers
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken);
        }

        public async Task<IEnumerable<QuestionAnswer>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await context.QuestionAnswers
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<PaginatedResult<QuestionAnswer>> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            Guid? questionId = null,
            Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            var query = context.QuestionAnswers
                .Where(a => a.IsActive)
                .AsQueryable();

            // Фильтрация
            if (questionId.HasValue)
                query = query.Where(a => a.QuestionId == questionId.Value);

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);

            // Сортировка
            query = query.OrderByDescending(a => a.CreatedAt);

            // Пагинация
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedResult<QuestionAnswer>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<IEnumerable<QuestionAnswer>> GetByQuestionIdAsync(Guid questionId,
            CancellationToken cancellationToken)
        {
            return await context.QuestionAnswers
                .Where(a => a.QuestionId == questionId && a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<QuestionAnswer>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await context.QuestionAnswers
                .Where(a => a.UserId == userId && a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<QuestionAnswer?> GetAcceptedAnswerAsync(Guid questionId, CancellationToken cancellationToken)
        {
            return await context.QuestionAnswers
                .FirstOrDefaultAsync(a => a.QuestionId == questionId && a.IsAccepted && a.IsActive, cancellationToken);
        }

        public async Task<QuestionAnswer> AddAsync(QuestionAnswer entity, CancellationToken cancellationToken)
        {
            context.QuestionAnswers.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<List<QuestionAnswer>> AddBulkAsync(List<QuestionAnswer> entities,
            CancellationToken cancellationToken)
        {
            context.QuestionAnswers.AddRange(entities);
            await context.SaveChangesAsync(cancellationToken);
            return entities;
        }

        public async Task UpdateAsync(QuestionAnswer entity, CancellationToken cancellationToken)
        {
            context.QuestionAnswers.Update(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkAsAcceptedAsync(Guid answerId, CancellationToken cancellationToken)
        {
            var answer = await GetByIdAsync(answerId, cancellationToken);
            if (answer != null)
            {
                // Снимаем отметку со всех ответов на этот вопрос
                var answersForQuestion = await context.QuestionAnswers
                    .Where(a => a.QuestionId == answer.QuestionId)
                    .ToListAsync(cancellationToken);

                foreach (var a in answersForQuestion)
                {
                    a.IsAccepted = false;
                }

                // Устанавливаем отметку для выбранного ответа
                answer.IsAccepted = true;

                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}