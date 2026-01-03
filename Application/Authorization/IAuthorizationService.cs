using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Application.IServices;

namespace IceBreakerApp.Application.Authorization
{
    /// <summary>
    /// Сервис для проверки разрешений и авторизации
    /// </summary>
    public interface IAuthorizationService
    {
        Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);
        Task<bool> HasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
        Task<bool> HasAnyRoleAsync(Guid userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default);
        Task<bool> HasAllRolesAsync(Guid userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default);
        Task<bool> IsResourceOwnerAsync(Guid userId, Guid resourceOwnerId, CancellationToken cancellationToken = default);
        Task<bool> IsEmailConfirmedAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> IsAdultAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    public class AuthorizationService : IAuthorizationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleService _roleService;
        private readonly IUserRoleRepository _userRoleRepository;

        public AuthorizationService(
            IUserRepository userRepository,
            IRoleService roleService,
            IUserRoleRepository userRoleRepository)
        {
            _userRepository = userRepository;
            _roleService = roleService;
            _userRoleRepository = userRoleRepository;
        }

        public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
        {
            // Получаем роли пользователя
            var userRoles = await _userRoleRepository.GetUserRolesAsync(userId, cancellationToken);
            
            // Проверяем каждую роль на наличие разрешения
            foreach (var role in userRoles)
            {
                // Здесь можно добавить проверку RolePermissions
                // Пока упрощенная логика
                if (role.Name == "Admin")
                {
                    return true; // Админ имеет все права
                }
            }

            return false;
        }

        public async Task<bool> HasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
        {
            var userRoles = await _userRoleRepository.GetUserRolesAsync(userId, cancellationToken);
            return userRoles.Any(role => role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> HasAnyRoleAsync(Guid userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default)
        {
            var userRoles = await _userRoleRepository.GetUserRolesAsync(userId, cancellationToken);
            var userRoleNames = userRoles.Select(role => role.Name.ToLowerInvariant());
            
            foreach (var roleName in roleNames)
            {
                if (userRoleNames.Contains(roleName.ToLowerInvariant()))
                {
                    return true;
                }
            }
            
            return false;
        }

        public async Task<bool> HasAllRolesAsync(Guid userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default)
        {
            var userRoles = await _userRoleRepository.GetUserRolesAsync(userId, cancellationToken);
            var userRoleNames = userRoles.Select(role => role.Name.ToLowerInvariant());
            
            foreach (var roleName in roleNames)
            {
                if (!userRoleNames.Contains(roleName.ToLowerInvariant()))
                {
                    return false;
                }
            }
            
            return true;
        }

        public async Task<bool> IsResourceOwnerAsync(Guid userId, Guid resourceOwnerId, CancellationToken cancellationToken = default)
        {
            return userId == resourceOwnerId;
        }

        public async Task<bool> IsEmailConfirmedAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            return user?.IsEmailConfirmed ?? false;
        }

        public async Task<bool> IsAdultAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user?.DateOfBirth == null)
            {
                return false;
            }

            var today = DateTime.Today;
            var age = today.Year - user.DateOfBirth.Value.Year;
            
            if (user.DateOfBirth.Value.Date > today.AddYears(-age))
                age--;

            return age >= 18;
        }

        public async Task<List<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userRoles = await _userRoleRepository.GetUserRolesAsync(userId, cancellationToken);
            return userRoles.Select(role => role.Name).ToList();
        }

        public async Task<List<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Упрощенная реализация - возвращаем разрешения на основе ролей
            var userRoles = await _userRoleRepository.GetUserRolesAsync(userId, cancellationToken);
            var permissions = new List<string>();

            foreach (var role in userRoles)
            {
                switch (role.Name.ToLowerInvariant())
                {
                    case "admin":
                        permissions.AddRange(new[]
                        {
                            "users.read", "users.write", "users.delete",
                            "questions.read", "questions.write", "questions.delete",
                            "answers.read", "answers.write", "answers.delete",
                            "topics.read", "topics.write", "topics.delete",
                            "reports.read", "reports.write",
                            "statistics.read"
                        });
                        break;
                    case "moderator":
                        permissions.AddRange(new[]
                        {
                            "users.read",
                            "questions.read", "questions.write", "questions.delete",
                            "answers.read", "answers.write", "answers.delete",
                            "topics.read", "topics.write",
                            "reports.read", "reports.write"
                        });
                        break;
                    case "user":
                        permissions.AddRange(new[]
                        {
                            "users.read", "users.write",
                            "questions.read", "questions.write",
                            "answers.read", "answers.write",
                            "topics.read"
                        });
                        break;
                }
            }

            return permissions.Distinct().ToList();
        }
    }
}