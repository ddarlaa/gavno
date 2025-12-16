using IceBreakerApp.Domain.Models;

namespace IceBreakerApp.Application.IRepositories
{
    public interface IUserRoleRepository
    {
        Task<UserRole?> GetByUserAndRoleIdAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserRole>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserRole>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid userRoleId, CancellationToken cancellationToken = default);
        Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    }
}