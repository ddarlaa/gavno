using IceBreakerApp.Domain.IRepositories;
using IceBreakerApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRepository(ApplicationDbContext context) : IUserRepository
    {
        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, ct);
        }

        public async Task<User?> FindByEmailAsync(string email, CancellationToken ct)
        {
            return await context.Users
                .FirstOrDefaultAsync(u => 
                    u.Email.ToLower() == email.ToLower() && u.IsActive, ct);
        }

        public async Task<User?> FindByUsernameAsync(string username, CancellationToken ct)
        {
            return await context.Users
                .FirstOrDefaultAsync(u => 
                    u.Username.ToLower() == username.ToLower() && u.IsActive, ct);
        }

        public async Task AddAsync(User user, CancellationToken ct)
        {
            context.Users.Add(user);
            await context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(User user, CancellationToken ct)
        {
            context.Users.Update(user);
            await context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var user = await GetByIdAsync(id, ct);
            if (user is null) return;
            
            // Mark user as inactive (soft delete)
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            
            await UpdateAsync(user, ct);
        }

        public async Task<IReadOnlyList<User>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        {
            return await context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyCollection<User>> GetByIdsAsync(IEnumerable<Guid> userIds, CancellationToken ct)
        {
            var idSet = userIds.ToHashSet();

            return await context.Users
                .Where(u => idSet.Contains(u.Id) && u.IsActive)
                .ToListAsync(ct);
        }

        public async Task<int> GetTotalCountAsync(CancellationToken ct)
        {
            return await context.Users
                .CountAsync(u => u.IsActive, ct);
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
        {
            return await context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive, ct);
        }

        public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct)
        {
            return await context.Users
                .AnyAsync(u => u.Username.ToLower() == username.ToLower() && u.IsActive, ct);
        }
    }
}