using FluentMigrator;
using System.Security.Cryptography;
using System.Text;

namespace Migrations;

[Migration(20251217000001)]
/// <summary>
/// Миграция для создания начальных данных приложения (обновленная версия)
/// Включает исправление схемы Users для корректной работы с датами
/// </summary>
public class SeedInitialData : Migration
{
    public override void Up()
    {
        var now = DateTime.UtcNow;

        // ====================================================================
        // СНАЧАЛА ИСПРАВЛЯЕМ СХЕМУ (ДО СОЗДАНИЯ ДАННЫХ)
        // ====================================================================
        
        // Делаем колонку UpdatedAt обязательной в таблице Users
        Alter.Table("Users")
            .AlterColumn("UpdatedAt").AsDateTime().NotNullable()
            .WithDefault(SystemMethods.CurrentUTCDateTime);
            
        // Обновляем любые существующие записи (если есть)
        Execute.Sql(@"
            UPDATE ""Users"" 
            SET ""UpdatedAt"" = ""CreatedAt"" 
            WHERE ""UpdatedAt"" IS NULL;
        ");

        // ====================================================================
        // БАЗОВЫЕ РОЛИ
        // ====================================================================
        
        Insert.IntoTable("Roles")
            .Row(new
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Admin",
                Description = "Administrator with full access",
                CreatedAt = now
            })
            .Row(new
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Moderator",
                Description = "Content moderator",
                CreatedAt = now
            })
            .Row(new
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "User",
                Description = "Regular user",
                CreatedAt = now
            });

        // ====================================================================
        // БАЗОВЫЕ РАЗРЕШЕНИЯ
        // ====================================================================
        
        Insert.IntoTable("Permissions")
            .Row(new
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Name = "ManageUsers",
                Description = "Create, update, delete users",
                Category = "UserManagement",
                CreatedAt = now
            })
            .Row(new
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab"),
                Name = "ManageContent",
                Description = "Manage questions and answers",
                Category = "ContentManagement",
                CreatedAt = now
            })
            .Row(new
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaac"),
                Name = "ViewReports",
                Description = "View system reports",
                Category = "Reports",
                CreatedAt = now
            })
            .Row(new
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaad"),
                Name = "CreateQuestion",
                Description = "Create questions",
                Category = "ContentCreation",
                CreatedAt = now
            })
            .Row(new
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaae"),
                Name = "CreateAnswer",
                Description = "Create answers",
                Category = "ContentCreation",
                CreatedAt = now
            });

        // ====================================================================
        // НАЗНАЧЕНИЕ РАЗРЕШЕНИЙ РОЛЯМ
        // ====================================================================
        
        Insert.IntoTable("RolePermissions")
            .Row(new
            {
                RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                CreatedAt = now
            })
            .Row(new
            {
                RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab"),
                CreatedAt = now
            })
            .Row(new
            {
                RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaac"),
                CreatedAt = now
            })
            .Row(new
            {
                RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaad"),
                CreatedAt = now
            })
            .Row(new
            {
                RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaae"),
                CreatedAt = now
            })
            .Row(new
            {
                RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab"),
                CreatedAt = now
            })
            .Row(new
            {
                RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaac"),
                CreatedAt = now
            })
            .Row(new
            {
                RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaad"),
                CreatedAt = now
            })
            .Row(new
            {
                RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaae"),
                CreatedAt = now
            });

        // ====================================================================
        // ТЕСТОВЫЕ ПОЛЬЗОВАТЕЛИ
        // ====================================================================
        
        Insert.IntoTable("Users")
            .Row(new
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Username = "admin",
                Email = "admin@icebreaker.com", 
                PasswordHash = HashPassword("Admin123!"),
                FirstName = "Администратор",
                LastName = "Системы",
                DisplayName = "Admin",
                Bio = "System administrator",
                IsEmailConfirmed = true,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now,  // Теперь UpdatedAt обязателен

            })
            .Row(new
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Username = "user1",
                Email = "user1@icebreaker.com", 
                PasswordHash = HashPassword("User123!"),
                FirstName = "Иван",
                LastName = "Петров",
                DisplayName = "Иван Петров",
                Bio = "Regular test user",
                IsEmailConfirmed = true,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1),  // Обновляем UpdatedAt для согласованности

            });

        // ====================================================================
        // НАЗНАЧЕНИЕ РОЛЕЙ ПОЛЬЗОВАТЕЛЯМ 
        // ====================================================================
        
        Insert.IntoTable("UserRoles")
            .Row(new
            {
                UserId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                AssignedAt = now
            })
            .Row(new
            {
                UserId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                AssignedAt = now
            });

        // ====================================================================
        // БАЗОВЫЕ ТЕМЫ
        // ====================================================================
        
        var topic1Id = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var topic2Id = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var topic3Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaadd"); 
        
        Insert.IntoTable("Topics")
            .Row(new
            {
                Id = topic1Id,
                Name = "Общие вопросы",
                Description = "Общие вопросы по использованию платформы",
                CreatedAt = now,
                IsActive = true,
            })
            .Row(new
            {
                Id = topic2Id,
                Name = "Техническая поддержка",
                Description = "Вопросы технического характера",
                CreatedAt = now,
                IsActive = true
            })
            .Row(new
            {
                Id = topic3Id,
                Name = "Предложения",
                Description = "Предложения по улучшению платформы",
                CreatedAt = now,
                IsActive = true,
            });

        // ====================================================================
        // ПРИМЕРНЫЕ ВОПРОСЫ
        // ====================================================================
        
        Insert.IntoTable("Questions")
            .Row(new
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
                UserId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                TopicId = topic1Id,
                Title = "Добро пожаловать в IceBreaker!",
                Content = "Это тестовый вопрос для демонстрации работы платформы. Здесь можно задавать вопросы и получать ответы от сообщества.",
                ViewCount = 25,
                LikeCount = 5,
                AnswerCount = 1,
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            })
            .Row(new
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
                UserId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                TopicId = topic2Id,
                Title = "Как начать работу с платформой?",
                Content = "Подскажите, пожалуйста, с чего начать новичку? Какие основные возможности доступны?",
                ViewCount = 12,
                LikeCount = 3,
                AnswerCount = 0,
                CreatedAt = now.AddHours(-2),
                UpdatedAt = now.AddHours(-2),
                IsActive = true,
            });

        // ====================================================================
        // ПРИМЕРНЫЕ ОТВЕТЫ
        // ====================================================================
        
        Insert.IntoTable("QuestionAnswers")
            .Row(new
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"),
                QuestionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
                UserId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Content = "Отличный вопрос! IceBreaker - это платформа для общения и обмена знаниями. Вы можете создавать вопросы, отвечать на них, ставить лайки. Рекомендую начать с изучения интерфейса и создания своего первого вопроса!",
                ViewCount = 8,
                CreatedAt = now.AddMinutes(-30),
                IsAccepted = true,
                IsActive = true
            });

        // ====================================================================
        // ПРИМЕРНЫЕ СЕАНСЫ ПОЛЬЗОВАТЕЛЕЙ
        // ====================================================================
        
        Insert.IntoTable("UserSessions")
            .Row(new
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1"),
                UserId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                RefreshTokenHash = HashPassword("refresh_token_admin_123"),
                CreatedAt = now,
                ExpiresAt = now.AddDays(7),
                DeviceInfo = "Test Admin Browser",
                IpAddress = "127.0.0.1",
                IsRevoked = false
            });
    }

    public override void Down()
    {
        // Удаляем добавленные данные в обратном порядке
        Execute.Sql(@"DELETE FROM ""UserSessions"" WHERE ""Id"" = 'dddddddd-dddd-dddd-dddd-dddddddddddd1'");
        Execute.Sql(@"DELETE FROM ""QuestionAnswers"" WHERE ""Id"" = 'cccccccc-cccc-cccc-cccc-cccccccccccc1'");
        Execute.Sql(@"DELETE FROM ""Questions"" WHERE ""Id"" IN ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2')");
        Execute.Sql(@"DELETE FROM ""Topics"" WHERE ""Id"" IN ('88888888-8888-8888-8888-888888888888', '99999999-9999-9999-9999-999999999999', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaadd')");
        
        // Удаляем UserRoles по составному ключу
        Execute.Sql(@"DELETE FROM ""UserRoles"" WHERE ""UserId"" = '44444444-4444-4444-4444-444444444444' AND ""RoleId"" = '11111111-1111-1111-1111-111111111111'");
        Execute.Sql(@"DELETE FROM ""UserRoles"" WHERE ""UserId"" = '55555555-5555-5555-5555-555555555555' AND ""RoleId"" = '33333333-3333-3333-3333-333333333333'");
        
        Execute.Sql(@"DELETE FROM ""Users"" WHERE ""Id"" IN ('44444444-4444-4444-4444-444444444444', '55555555-5555-5555-5555-555555555555')");
        
        // Удаляем RolePermissions по составному ключу
        Execute.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""RoleId"" = '11111111-1111-1111-1111-111111111111' AND ""PermissionId"" = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'");
        Execute.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""RoleId"" = '11111111-1111-1111-1111-111111111111' AND ""PermissionId"" = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab'");
        Execute.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""RoleId"" = '11111111-1111-1111-1111-111111111111' AND ""PermissionId"" = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaac'");
        Execute.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""RoleId"" = '11111111-1111-1111-1111-111111111111' AND ""PermissionId"" = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaad'");
        Execute.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""RoleId"" = '11111111-1111-1111-1111-111111111111' AND ""PermissionId"" = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaae'");
        Execute.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""RoleId"" = '22222222-2222-2222-2222-222222222222' AND ""PermissionId"" = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab'");
        Execute.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""RoleId"" = '22222222-2222-2222-2222-222222222222' AND ""PermissionId"" = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaac'");
        Execute.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""RoleId"" = '33333333-3333-3333-3333-333333333333' AND ""PermissionId"" = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaad'");
        Execute.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""RoleId"" = '33333333-3333-3333-3333-333333333333' AND ""PermissionId"" = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaae'");
        
        Execute.Sql(@"DELETE FROM ""Permissions"" WHERE ""Id"" IN ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaac', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaad', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaae')");
        Execute.Sql(@"DELETE FROM ""Roles"" WHERE ""Id"" IN ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', '33333333-3333-3333-3333-333333333333')");
    }

    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}