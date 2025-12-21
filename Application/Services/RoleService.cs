using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository,
            ILogger<RoleService> logger)
        {
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _logger = logger;
        }

        public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _roleRepository.GetByNameAsync(name, cancellationToken);
        }

        public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _roleRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var roles = await _roleRepository.GetAllAsync(cancellationToken);
            return roles.ToList();
        }

        public async Task AssignRoleToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Assigning role {RoleId} to user {UserId}", roleId, userId);
            
            // Проверяем, не назначена ли уже эта роль
            var existingUserRole = await _userRoleRepository.GetByUserAndRoleIdAsync(userId, roleId, cancellationToken);
            if (existingUserRole != null)
            {
                return; // Роль уже назначена
            }

            // Создаем запись UserRole
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            };

            await _userRoleRepository.AddAsync(userRole, cancellationToken);
        }

        public async Task AssignRoleToUserAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
        {
            var role = await GetByNameAsync(roleName, cancellationToken);
            if (role == null)
            {
                throw new Exception($"Role '{roleName}' not found");
            }

            await AssignRoleToUserAsync(userId, role.Id, cancellationToken);
        }

        public async Task RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            // В реальной реализации здесь будет удаление из базы данных
            _logger.LogInformation("Removing role {RoleId} from user {UserId}", roleId, userId);
            await Task.CompletedTask;
        }

        public async Task<List<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userRoles = await _userRoleRepository.GetUserRolesAsync(userId, cancellationToken);
            return userRoles.ToList();
        }

        public async Task<bool> UserHasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
        {
            var userRoles = await GetUserRolesAsync(userId, cancellationToken);
            return userRoles.Any(role => role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> UserHasPermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default)
        {
            // Простая проверка разрешений на основе ролей
            var userRoles = await GetUserRolesAsync(userId, cancellationToken);
            
            foreach (var role in userRoles)
            {
                switch (role.Name.ToLowerInvariant())
                {
                    case "administrator":
                        return true; // Админ имеет все разрешения
                        
                    case "moderator":
                        if (permissionName.StartsWith("CanCreate", StringComparison.OrdinalIgnoreCase) ||
                            permissionName.StartsWith("CanEdit", StringComparison.OrdinalIgnoreCase) ||
                            permissionName.StartsWith("CanDelete", StringComparison.OrdinalIgnoreCase) ||
                            permissionName == "CanModerateContent" ||
                            permissionName == "CanViewReports")
                            return true;
                        break;
                        
                    case "contentcreator":
                        if (permissionName.StartsWith("CanCreate", StringComparison.OrdinalIgnoreCase) ||
                            permissionName.StartsWith("CanEdit", StringComparison.OrdinalIgnoreCase) ||
                            permissionName.StartsWith("CanDelete", StringComparison.OrdinalIgnoreCase))
                            return true;
                        break;
                        
                    case "premiumuser":
                        if (permissionName.StartsWith("CanCreate", StringComparison.OrdinalIgnoreCase) ||
                            permissionName.StartsWith("CanEdit", StringComparison.OrdinalIgnoreCase) ||
                            permissionName == "CanPinQuestion" ||
                            permissionName == "CanFeatureContent")
                            return true;
                        break;
                        
                    case "user":
                        if (permissionName.StartsWith("CanCreate", StringComparison.OrdinalIgnoreCase) ||
                            permissionName.StartsWith("CanEdit", StringComparison.OrdinalIgnoreCase))
                            return true;
                        break;
                }
            }
            
            return false;
        }
    }
}