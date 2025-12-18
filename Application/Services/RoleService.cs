using IceBreakerApp.Application.IServices;
using IceBreakerApp.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application.Services
{
    // Временная заглушка для фокуса на JWT
    public class RoleService : IRoleService
    {
        private readonly ILogger<RoleService> _logger;

        public RoleService(ILogger<RoleService> logger)
        {
            _logger = logger;
        }

        public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => new Role { Name = name });
        }

        public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => new Role { Id = id });
        }

        public async Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("RoleService is temporarily disabled for JWT focus");
            return new List<Role>();
        }

        public async Task AssignRoleToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("RoleService is temporarily disabled for JWT focus");
        }

        public async Task AssignRoleToUserAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("RoleService is temporarily disabled for JWT focus");
        }

        public async Task RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("RoleService is temporarily disabled for JWT focus");
        }

        public async Task<List<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => new List<Role>());
        }

        public async Task<bool> UserHasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("RoleService is temporarily disabled for JWT focus");
            return false;
        }

        public async Task<bool> UserHasPermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("RoleService is temporarily disabled for JWT focus");
            return false;
        }
    }
}