using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        
        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, cancellationToken);
        }

        public async Task<User?> GetByIdWithTrackingAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, cancellationToken);
        }

        public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => 
                    u.Email.ToLower() == email.ToLower() && u.IsActive, cancellationToken);
        }

        public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => 
                    u.Username.ToLower() == username.ToLower() && u.IsActive, cancellationToken);
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive, cancellationToken);
        }

        public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Username.ToLower() == username.ToLower() && u.IsActive, cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdWithTrackingAsync(id, cancellationToken);
            if (user == null) return;
            
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            
            await UpdateAsync(user, cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Username)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetPageAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(u => 
                    u.Username.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    (u.DisplayName != null && u.DisplayName.ToLower().Contains(search)) ||
                    (u.Bio != null && u.Bio.ToLower().Contains(search)));
            }

            return await query
                .OrderBy(u => u.Username)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string? search = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(u => 
                    u.Username.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    (u.DisplayName != null && u.DisplayName.ToLower().Contains(search)) ||
                    (u.Bio != null && u.Bio.ToLower().Contains(search)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            
            var items = await query
                .OrderBy(u => u.Username)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<IReadOnlyCollection<User>> GetByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
        {
            var idSet = userIds.ToHashSet();
            
            return await _context.Users
                .AsNoTracking()
                .Where(u => idSet.Contains(u.Id) && u.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .CountAsync(u => u.IsActive, cancellationToken);
        }

        public async Task<int> GetCountBySearchAsync(string? search, CancellationToken cancellationToken = default)
        {
            var query = _context.Users
                .Where(u => u.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(u => 
                    u.Username.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    (u.DisplayName != null && u.DisplayName.ToLower().Contains(search)) ||
                    (u.Bio != null && u.Bio.ToLower().Contains(search)));
            }

            return await query.CountAsync(cancellationToken);
        }

        public async Task<User?> AuthenticateAsync(string usernameOrEmail, string passwordHash, CancellationToken cancellationToken = default)
        {
            var normalizedInput = usernameOrEmail.ToLower();
            
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => 
                    (u.Username.ToLower() == normalizedInput || u.Email.ToLower() == normalizedInput) &&
                    u.PasswordHash == passwordHash &&
                    u.IsActive, cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Id == id && u.IsActive, cancellationToken);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive, cancellationToken);
        }

        public async Task<bool> IsUsernameUniqueAsync(string username, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Username.ToLower() == username.ToLower() && u.IsActive, cancellationToken);
        }
    }
}