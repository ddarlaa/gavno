using FluentMigrator;

namespace Migrations;

[Migration(20251216000001)]
public class AddUserRolesAndPermissions : Migration
{
    public override void Up()
    {
        // Добавление колонки PasswordSalt в существующую таблицу Users
        Alter.Table("Users")
            .AddColumn("PasswordSalt").AsString(128).NotNullable().WithDefaultValue("");

        // Добавление дополнительных разрешений для расширенной системы ролей
        var additionalPermissions = new[]
        {
            new { Id = Guid.NewGuid(), Name = "CanCreateUser", Description = "Создание новых пользователей", Category = "UserManagement" },
            new { Id = Guid.NewGuid(), Name = "CanEditUser", Description = "Редактирование данных пользователей", Category = "UserManagement" },
            new { Id = Guid.NewGuid(), Name = "CanDeleteUser", Description = "Удаление пользователей", Category = "UserManagement" },
            new { Id = Guid.NewGuid(), Name = "CanViewUserProfile", Description = "Просмотр профилей пользователей", Category = "UserManagement" },
            new { Id = Guid.NewGuid(), Name = "CanModerateContent", Description = "Модерация контента", Category = "ContentManagement" },
            new { Id = Guid.NewGuid(), Name = "CanManageRoles", Description = "Управление ролями пользователей", Category = "System" },
            new { Id = Guid.NewGuid(), Name = "CanManagePermissions", Description = "Управление разрешениями", Category = "System" },
            new { Id = Guid.NewGuid(), Name = "CanAccessSystemSettings", Description = "Доступ к системным настройкам", Category = "System" },
            new { Id = Guid.NewGuid(), Name = "CanPinQuestion", Description = "Закрепление вопросов", Category = "Special" },
            new { Id = Guid.NewGuid(), Name = "CanFeatureContent", Description = "Выделение контента", Category = "Special" },
            new { Id = Guid.NewGuid(), Name = "CanBulkOperations", Description = "Массовые операции", Category = "Special" }
        };

        foreach (var permission in additionalPermissions)
        {
            Insert.IntoTable("Permissions").Row(new { 
                Id = permission.Id, 
                Name = permission.Name, 
                Description = permission.Description, 
                Category = permission.Category, 
                CreatedAt = DateTime.UtcNow 
            });
        }
    }

    public override void Down()
    {
        // Удаление добавленного столбца из Users
        Delete.Column("PasswordSalt").FromTable("Users");
    }
}