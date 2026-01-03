using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain.Models;

namespace IceBreakerApp.Application.IServices
{
    public interface IRoleService
    {
        Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default);
        Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default);
        Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AssignRoleToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
        Task AssignRoleToUserAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
        Task RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
        Task<List<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> UserHasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
        Task<bool> UserHasPermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default);
    }
}