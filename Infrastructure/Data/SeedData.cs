using IceBreakerApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public static class SeedData
    {
        public static void SeedRolesAndPermissions(ModelBuilder modelBuilder)
        {
            // Создание ролей с предопределенными ID для стабильности
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var moderatorRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var userRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var premiumUserRoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            var contentCreatorRoleId = Guid.Parse("55555555-5555-5555-5555-555555555555");

            var roles = new[]
            {
                new Role { Id = adminRoleId, Name = "Administrator", Description = "Полный доступ к системе", CreatedAt = DateTime.UtcNow },
                new Role { Id = moderatorRoleId, Name = "Moderator", Description = "Модератор контента и пользователей", CreatedAt = DateTime.UtcNow },
                new Role { Id = userRoleId, Name = "User", Description = "Обычный пользователь", CreatedAt = DateTime.UtcNow },
                new Role { Id = premiumUserRoleId, Name = "PremiumUser", Description = "Премиум пользователь с расширенными возможностями", CreatedAt = DateTime.UtcNow },
                new Role { Id = contentCreatorRoleId, Name = "ContentCreator", Description = "Создатель контента", CreatedAt = DateTime.UtcNow }
            };

            modelBuilder.Entity<Role>().HasData(roles);

            // Создание разрешений
            var permissions = new[]
            {
                // Управление пользователями
                new Permission { Id = Guid.NewGuid(), Name = "CanCreateUser", Description = "Создание новых пользователей", Category = "UserManagement", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanEditUser", Description = "Редактирование данных пользователей", Category = "UserManagement", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanDeleteUser", Description = "Удаление пользователей", Category = "UserManagement", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanViewUserProfile", Description = "Просмотр профилей пользователей", Category = "UserManagement", CreatedAt = DateTime.UtcNow },
                
                // Управление контентом
                new Permission { Id = Guid.NewGuid(), Name = "CanCreateQuestion", Description = "Создание вопросов", Category = "ContentManagement", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanEditQuestion", Description = "Редактирование вопросов", Category = "ContentManagement", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanDeleteQuestion", Description = "Удаление вопросов", Category = "ContentManagement", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanCreateAnswer", Description = "Создание ответов", Category = "ContentManagement", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanEditAnswer", Description = "Редактирование ответов", Category = "ContentManagement", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanDeleteAnswer", Description = "Удаление ответов", Category = "ContentManagement", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanModerateContent", Description = "Модерация контента", Category = "ContentManagement", CreatedAt = DateTime.UtcNow },
                
                // Управление темами
                new Permission { Id = Guid.NewGuid(), Name = "CanCreateTopic", Description = "Создание новых тем", Category = "TopicManagement", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanEditTopic", Description = "Редактирование тем", Category = "TopicManagement", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanDeleteTopic", Description = "Удаление тем", Category = "TopicManagement", CreatedAt = DateTime.UtcNow },
                
                // Системные права
                new Permission { Id = Guid.NewGuid(), Name = "CanViewReports", Description = "Просмотр отчетов и аналитики", Category = "System", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanManageRoles", Description = "Управление ролями пользователей", Category = "System", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanManagePermissions", Description = "Управление разрешениями", Category = "System", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanAccessSystemSettings", Description = "Доступ к системным настройкам", Category = "System", CreatedAt = DateTime.UtcNow },
                
                // Специальные возможности
                new Permission { Id = Guid.NewGuid(), Name = "CanPinQuestion", Description = "Закрепление вопросов", Category = "Special", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanFeatureContent", Description = "Выделение контента", Category = "Special", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "CanBulkOperations", Description = "Массовые операции", Category = "Special", CreatedAt = DateTime.UtcNow }
            };

            modelBuilder.Entity<Permission>().HasData(permissions);

            // Связывание ролей с разрешениями
            var rolePermissions = new List<RolePermission>();
            var permissionIdCounter = 1; 
            
            // Administrator - все разрешения
            var adminRole = roles.First(r => r.Name == "Administrator");
            foreach (var permission in permissions)
            {
                rolePermissions.Add(new RolePermission
                {
                    Id = permissionIdCounter++, // ЯВНО задаем Id
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // Moderator - модерация и управление контентом
            var moderatorRole = roles.First(r => r.Name == "Moderator");
            var moderatorPermissions = permissions.Where(p => 
                p.Category == "ContentManagement" || 
                p.Name == "CanViewUserProfile" ||
                p.Name == "CanModerateContent" ||
                p.Name == "CanPinQuestion" ||
                p.Name == "CanFeatureContent").ToList();
            
            foreach (var permission in moderatorPermissions)
            {
                rolePermissions.Add(new RolePermission
                {
                    Id = permissionIdCounter++,
                    RoleId = moderatorRole.Id,
                    PermissionId = permission.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // ContentCreator - создание и редактирование контента
            var contentCreatorRole = roles.First(r => r.Name == "ContentCreator");
            var contentCreatorPermissions = permissions.Where(p => 
                p.Category == "ContentManagement" || 
                p.Name == "CanCreateQuestion" ||
                p.Name == "CanCreateAnswer").ToList();
            
            foreach (var permission in contentCreatorPermissions)
            {
                rolePermissions.Add(new RolePermission
                {
                    Id = permissionIdCounter++,
                    RoleId = contentCreatorRole.Id,
                    PermissionId = permission.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // PremiumUser - расширенные возможности
            var premiumUserRole = roles.First(r => r.Name == "PremiumUser");
            var premiumUserPermissions = permissions.Where(p => 
                p.Name == "CanCreateQuestion" ||
                p.Name == "CanCreateAnswer" ||
                p.Name == "CanPinQuestion").ToList();
            
            foreach (var permission in premiumUserPermissions)
            {
                rolePermissions.Add(new RolePermission
                {
                    Id = permissionIdCounter++,
                    RoleId = premiumUserRole.Id,
                    PermissionId = permission.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // User - базовые возможности
            var userRole = roles.First(r => r.Name == "User");
            var userPermissions = permissions.Where(p => 
                p.Name == "CanCreateQuestion" ||
                p.Name == "CanCreateAnswer").ToList();
            
            foreach (var permission in userPermissions)
            {
                rolePermissions.Add(new RolePermission
                {
                    Id = permissionIdCounter++,
                    RoleId = userRole.Id,
                    PermissionId = permission.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            modelBuilder.Entity<RolePermission>().HasData(rolePermissions);
        }
    }
}