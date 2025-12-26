using FluentMigrator;

namespace Migrations
{
    [Migration(202312261249)]
    public class SeedInitialData : Migration
    {
        public override void Up()
        {
            // Правильно отформатированные GUID
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var userRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var moderatorRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            
            var adminPermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var userPermissionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var readPermissionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            
            var adminUserId = Guid.Parse("deadbeef-dead-beef-dead-beefdeadbeef");
            var testUserId = Guid.Parse("cafebabe-cafe-babe-cafe-bafecafebabe");
            
            var iceBreakerCategoryId = Guid.Parse("12345678-1234-5678-1234-567812345678");
            var conversationStarterId = Guid.Parse("87654321-4321-8765-4321-876543217654");
            
            // Вставка ролей
            Insert.IntoTable("Roles").Row(new
            {
                Id = adminRoleId,
                Name = "Admin",
                Description = "Administrator role with full access",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            
            Insert.IntoTable("Roles").Row(new
            {
                Id = userRoleId,
                Name = "User",
                Description = "Regular user role",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            
            Insert.IntoTable("Roles").Row(new
            {
                Id = moderatorRoleId,
                Name = "Moderator",
                Description = "Moderator role with content management access",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            
            // Вставка разрешений
            Insert.IntoTable("Permissions").Row(new
            {
                Id = adminPermissionId,
                Name = "Admin.All",
                Description = "Full administrative access",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            
            Insert.IntoTable("Permissions").Row(new
            {
                Id = userPermissionId,
                Name = "User.All",
                Description = "User access to personal resources",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            
            Insert.IntoTable("Permissions").Row(new
            {
                Id = readPermissionId,
                Name = "Read.All",
                Description = "Read-only access",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            
            // Вставка пользователей
            Insert.IntoTable("AspNetUsers").Row(new
            {
                Id = adminUserId,
                UserName = "admin@icebreaker.com",
                NormalizedUserName = "ADMIN@ICEBREAKER.COM",
                Email = "admin@icebreaker.com",
                NormalizedEmail = "ADMIN@ICEBREAKER.COM",
                EmailConfirmed = true,
                PhoneNumber = "+1234567890",
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DisplayName = "Administrator",
                Bio = "System Administrator",
                IsActive = true,
                LastLoginAt = DateTime.UtcNow
            });
            
            Insert.IntoTable("AspNetUsers").Row(new
            {
                Id = testUserId,
                UserName = "test@icebreaker.com",
                NormalizedUserName = "TEST@ICEBREAKER.COM",
                Email = "test@icebreaker.com",
                NormalizedEmail = "TEST@ICEBREAKER.COM",
                EmailConfirmed = true,
                PhoneNumber = "+0987654321",
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DisplayName = "Test User",
                Bio = "Test user for development",
                IsActive = true,
                LastLoginAt = DateTime.UtcNow
            });
            
            // Вставка категорий ломающих лед
            Insert.IntoTable("IceBreakerCategories").Row(new
            {
                Id = iceBreakerCategoryId,
                Name = "Getting to Know You",
                Description = "Questions to help people get to know each other better",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            
            // Вставка стартеров разговора
            Insert.IntoTable("ConversationStarters").Row(new
            {
                Id = conversationStarterId,
                CategoryId = iceBreakerCategoryId,
                Question = "What's your favorite way to spend a weekend?",
                CreatedBy = adminUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            });
            
            // Связи ролей и пользователей
            Insert.IntoTable("AspNetUserRoles").Row(new
            {
                UserId = adminUserId,
                RoleId = adminRoleId
            });
            
            Insert.IntoTable("AspNetUserRoles").Row(new
            {
                UserId = testUserId,
                RoleId = userRoleId
            });
            
            // Связи ролей и разрешений
            Insert.IntoTable("RolePermissions").Row(new
            {
                RoleId = adminRoleId,
                PermissionId = adminPermissionId
            });
            
            Insert.IntoTable("RolePermissions").Row(new
            {
                RoleId = userRoleId,
                PermissionId = userPermissionId
            });
            
            Insert.IntoTable("RolePermissions").Row(new
            {
                RoleId = userRoleId,
                PermissionId = readPermissionId
            });
        }

        public override void Down()
        {
            // Удаление связей
            Delete.FromTable("RolePermissions").Row(new { PermissionId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" });
            Delete.FromTable("RolePermissions").Row(new { PermissionId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb" });
            Delete.FromTable("RolePermissions").Row(new { PermissionId = "cccccccc-cccc-cccc-cccc-cccccccccccc" });
            
            Delete.FromTable("AspNetUserRoles").Row(new { RoleId = "11111111-1111-1111-1111-111111111111" });
            Delete.FromTable("AspNetUserRoles").Row(new { RoleId = "22222222-2222-2222-2222-222222222222" });
            
            // Удаление данных
            Delete.FromTable("ConversationStarters").Row(new { Id = "87654321-4321-8765-4321-876543217654" });
            Delete.FromTable("IceBreakerCategories").Row(new { Id = "12345678-1234-5678-1234-567812345678" });
            
            Delete.FromTable("AspNetUsers").Row(new { Id = "deadbeef-dead-beef-dead-beefdeadbeef" });
            Delete.FromTable("AspNetUsers").Row(new { Id = "cafebabe-cafe-babe-cafe-bafecafebabe" });
            
            Delete.FromTable("Permissions").Row(new { Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" });
            Delete.FromTable("Permissions").Row(new { Id = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb" });
            Delete.FromTable("Permissions").Row(new { Id = "cccccccc-cccc-cccc-cccc-cccccccccccc" });
            
            Delete.FromTable("Roles").Row(new { Id = "11111111-1111-1111-1111-111111111111" });
            Delete.FromTable("Roles").Row(new { Id = "22222222-2222-2222-2222-222222222222" });
            Delete.FromTable("Roles").Row(new { Id = "33333333-3333-3333-3333-333333333333" });
        }
    }
}