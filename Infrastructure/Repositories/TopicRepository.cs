using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TopicRepository(ApplicationDbContext context) : ITopicRepository
    {
        public async Task<Topic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await context.Topics
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive, cancellationToken);
        }

        public async Task<PaginatedResult<Topic>> GetPaginatedAsync(
            int pageNumber, 
            int pageSize, 
            string? search = null, 
            CancellationToken cancellationToken = default)
        {
            var query = context.Topics
                .Where(t => t.IsActive)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t =>
                    t.Name.Contains(search) ||
                    (t.Description.Contains(search)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(t => t.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedResult<Topic>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<Topic?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await context.Topics
                .FirstOrDefaultAsync(t => 
                    t.Name.ToLower() == name.ToLower() && t.IsActive, cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await context.Topics
                .AnyAsync(t => 
                    t.Name.ToLower() == name.ToLower() && t.IsActive, cancellationToken);
        }

        public async Task<IEnumerable<Topic>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await context.Topics
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Topic> AddAsync(Topic item, CancellationToken cancellationToken = default)
        {
            context.Topics.Add(item);
            await context.SaveChangesAsync(cancellationToken);
            return item;
        }

        public async Task UpdateAsync(Topic item, CancellationToken cancellationToken = default)
        {
            context.Topics.Update(item);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var topic = await GetByIdAsync(id, cancellationToken);
            
            if (topic != null)
            {
                // Mark topic as inactive (soft delete)
                topic.IsActive = false;
                topic.UpdatedAt = DateTime.UtcNow;
                
                await UpdateAsync(topic, cancellationToken);
            }
        }

        public async Task<IReadOnlyCollection<Topic>> GetByIdsAsync(IEnumerable<Guid> topicIds, CancellationToken ct)
        {
            var idSet = topicIds as HashSet<Guid> ?? topicIds.ToHashSet();
        
            return await context.Topics
                .Where(t => idSet.Contains(t.Id) && t.IsActive)
                .ToListAsync(ct);
        }
    }
}