using FluentMigrator;
using System.Security.Cryptography;
using System.Text;

namespace Migrations;

[Migration(20251210000003)]

/// <summary>
/// Миграция для создания начальных данных приложения (минимальная версия)
/// </summary>

public class SeedInitialData : Migration
{
    public override void Up()
    {
        var now = DateTime.UtcNow;

        // ====================================================================
        // ТЕСТОВЫЕ ПОЛЬЗОВАТЕЛИ
        // ====================================================================
        
        Insert.IntoTable("Users")
            .Row(new
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Username = "admin",
                Email = "admin@test.com", 
                PasswordHash = HashPassword("admin123"),
                DisplayName = "Admin",
                Bio = "Test admin",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            })
            .Row(new
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Username = "user1",
                Email = "user1@test.com", 
                PasswordHash = HashPassword("user123"),
                DisplayName = "User One",
                Bio = "Test user",
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now,
                IsActive = true
            });

        // ====================================================================
        // БАЗОВЫЕ ТЕМЫ
        // ====================================================================
        
        var topics = new[]
        {
            new { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name = "General", Description = "General chat" },
            new { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Name = "Tech", Description = "Tech talk" }
        };

        foreach (var topic in topics)
        {
            Insert.IntoTable("Topics")
                .Row(new
                {
                    Id = topic.Id,
                    Name = topic.Name,
                    Description = topic.Description,
                    CreatedAt = now,
                    IsActive = true
                });
        }

        // ====================================================================
        // ПРИМЕРНЫЕ ВОПРОСЫ
        // ====================================================================
        
        Insert.IntoTable("Questions")
            .Row(new
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1"),
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                TopicId = topics[0].Id,
                Title = "Test question 1",
                Content = "Question content 1",
                ViewCount = 10,
                LikeCount = 5,
                AnswerCount = 2,
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            })
            .Row(new
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2"),
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                TopicId = topics[1].Id,
                Title = "Test question 2",
                Content = "Question content 2",
                ViewCount = 15,
                LikeCount = 8,
                AnswerCount = 3,
                CreatedAt = now.AddHours(-1),
                UpdatedAt = now,
                IsActive = true
            });

        // ====================================================================
        // ПРИМЕРНЫЕ ОТВЕТЫ
        // ====================================================================
        
        Insert.IntoTable("QuestionAnswers")
            .Row(new
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee3"),
                QuestionId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1"),
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Content = "Test answer 1",
                ViewCount = 5,
                CreatedAt = now.AddMinutes(-30),
                IsAccepted = true,
                IsActive = true
            });
    }

    public override void Down()
    {
        // Удаляем добавленные данные
        Execute.Sql(@"DELETE FROM ""QuestionAnswers"" WHERE ""Id"" = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee3'");
        Execute.Sql(@"DELETE FROM ""Questions"" WHERE ""Id"" IN ('aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1', 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2')");
        Execute.Sql(@"DELETE FROM ""Topics"" WHERE ""Id"" IN ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb')");
        Execute.Sql(@"DELETE FROM ""Users"" WHERE ""Id"" IN ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222')");
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