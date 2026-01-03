using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserRole?> GetByUserAndRoleIdAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        }

        public async Task<IReadOnlyList<UserRole>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .OrderBy(ur => ur.Role.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UserRole>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.RoleId == roleId)
                .Include(ur => ur.User)
                .OrderBy(ur => ur.User.Username)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role)
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default)
        {
            await _context.UserRoles.AddAsync(userRole, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid userRoleId, CancellationToken cancellationToken = default)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userRoleId, cancellationToken);
            
            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        }
    }
}