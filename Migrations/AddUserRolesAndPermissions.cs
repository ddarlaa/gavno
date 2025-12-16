using FluentMigrator;

namespace Migrations;

[Migration(20251216000001)]
public class AddUserRolesAndPermissions : Migration
{
    public override void Up()
    {
        // Таблица UserSessions
        Create.Table("UserSessions")
            .WithColumn("Id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("RefreshTokenHash").AsString(512).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("ExpiresAt").AsDateTime().NotNullable()
            .WithColumn("DeviceInfo").AsString(500).Nullable()
            .WithColumn("IpAddress").AsString(45).Nullable()
            .WithColumn("IsRevoked").AsBoolean().NotNullable().WithDefaultValue(false);

        // Таблица Roles
        Create.Table("Roles")
            .WithColumn("Id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Description").AsString(500).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);

        // Таблица Permissions
        Create.Table("Permissions")
            .WithColumn("Id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Description").AsString(500).Nullable()
            .WithColumn("Category").AsString(50).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);

        // Таблица UserRoles (Many-to-Many)
        Create.Table("UserRoles")
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("RoleId").AsGuid().NotNullable()
            .WithColumn("AssignedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("AssignedBy").AsGuid().Nullable();

        // Таблица RolePermissions (Many-to-Many)
        Create.Table("RolePermissions")
            .WithColumn("RoleId").AsGuid().NotNullable()
            .WithColumn("PermissionId").AsGuid().NotNullable();

        // Таблица UserClaims
        Create.Table("UserClaims")
            .WithColumn("Id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("ClaimType").AsString(100).NotNullable()
            .WithColumn("ClaimValue").AsString(500).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);

        // Внешние ключи
        Create.ForeignKey("FK_UserSessions_Users")
            .FromTable("UserSessions").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_UserRoles_Users")
            .FromTable("UserRoles").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_UserRoles_Roles")
            .FromTable("UserRoles").ForeignColumn("RoleId")
            .ToTable("Roles").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_UserRoles_AssignedBy")
            .FromTable("UserRoles").ForeignColumn("AssignedBy")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.SetNull);

        Create.ForeignKey("FK_RolePermissions_Roles")
            .FromTable("RolePermissions").ForeignColumn("RoleId")
            .ToTable("Roles").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_RolePermissions_Permissions")
            .FromTable("RolePermissions").ForeignColumn("PermissionId")
            .ToTable("Permissions").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_UserClaims_Users")
            .FromTable("UserClaims").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // Уникальные индексы
        Create.UniqueConstraint("UQ_UserSessions_RefreshTokenHash")
            .OnTable("UserSessions").Column("RefreshTokenHash");

        Create.UniqueConstraint("UQ_Roles_Name")
            .OnTable("Roles").Column("Name");

        Create.UniqueConstraint("UQ_Permissions_Name")
            .OnTable("Permissions").Column("Name");

        // Составные уникальные ограничения
        Create.UniqueConstraint("UQ_UserRoles_UserId_RoleId")
            .OnTable("UserRoles").Columns("UserId", "RoleId");

        Create.UniqueConstraint("UQ_RolePermissions_RoleId_PermissionId")
            .OnTable("RolePermissions").Columns("RoleId", "PermissionId");

        // Обычные индексы
        Create.Index("IX_UserSessions_UserId").OnTable("UserSessions").OnColumn("UserId");
        Create.Index("IX_UserSessions_ExpiresAt").OnTable("UserSessions").OnColumn("ExpiresAt");
        Create.Index("IX_UserSessions_IsRevoked").OnTable("UserSessions").OnColumn("IsRevoked");

        Create.Index("IX_Permissions_Category").OnTable("Permissions").OnColumn("Category");
        
        Create.Index("IX_UserClaims_UserId").OnTable("UserClaims").OnColumn("UserId");
        Create.Index("IX_UserClaims_ClaimType").OnTable("UserClaims").OnColumn("ClaimType");
        Create.Index("IX_UserClaims_ClaimType_ClaimValue").OnTable("UserClaims").OnColumns("ClaimType", "ClaimValue");

        // Добавление колонок в существующую таблицу Users
        Alter.Table("Users")
            .AddColumn("PasswordSalt").AsString(128).NotNullable().WithDefaultValue("")
            .AddColumn("FirstName").AsString(100).Nullable()
            .AddColumn("LastName").AsString(100).Nullable()
            .AddColumn("DateOfBirth").AsDateTime().Nullable()
            .AddColumn("PhoneNumber").AsString(20).Nullable()
            .AddColumn("LastLoginAt").AsDateTime().Nullable()
            .AddColumn("IsEmailConfirmed").AsBoolean().NotNullable().WithDefaultValue(false)
            .AddColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false);

        // Добавление данных по умолчанию для ролей
        var adminRoleId = Guid.NewGuid();
        var moderatorRoleId = Guid.NewGuid();
        var userRoleId = Guid.NewGuid();
        var premiumUserRoleId = Guid.NewGuid();
        var contentCreatorRoleId = Guid.NewGuid();

        Insert.IntoTable("Roles").Row(new { Id = adminRoleId, Name = "Administrator", Description = "Полный доступ к системе", CreatedAt = DateTime.UtcNow });
        Insert.IntoTable("Roles").Row(new { Id = moderatorRoleId, Name = "Moderator", Description = "Модератор контента и пользователей", CreatedAt = DateTime.UtcNow });
        Insert.IntoTable("Roles").Row(new { Id = userRoleId, Name = "User", Description = "Обычный пользователь", CreatedAt = DateTime.UtcNow });
        Insert.IntoTable("Roles").Row(new { Id = premiumUserRoleId, Name = "PremiumUser", Description = "Премиум пользователь с расширенными возможностями", CreatedAt = DateTime.UtcNow });
        Insert.IntoTable("Roles").Row(new { Id = contentCreatorRoleId, Name = "ContentCreator", Description = "Создатель контента", CreatedAt = DateTime.UtcNow });

        // Добавление разрешений
        var permissions = new[]
        {
            new { Id = Guid.NewGuid(), Name = "CanCreateUser", Description = "Создание новых пользователей", Category = "UserManagement" },
            new { Id = Guid.NewGuid(), Name = "CanEditUser", Description = "Редактирование данных пользователей", Category = "UserManagement" },
            new { Id = Guid.NewGuid(), Name = "CanDeleteUser", Description = "Удаление пользователей", Category = "UserManagement" },
            new { Id = Guid.NewGuid(), Name = "CanViewUserProfile", Description = "Просмотр профилей пользователей", Category = "UserManagement" },
            new { Id = Guid.NewGuid(), Name = "CanCreateQuestion", Description = "Создание вопросов", Category = "ContentManagement" },
            new { Id = Guid.NewGuid(), Name = "CanEditQuestion", Description = "Редактирование вопросов", Category = "ContentManagement" },
            new { Id = Guid.NewGuid(), Name = "CanDeleteQuestion", Description = "Удаление вопросов", Category = "ContentManagement" },
            new { Id = Guid.NewGuid(), Name = "CanCreateAnswer", Description = "Создание ответов", Category = "ContentManagement" },
            new { Id = Guid.NewGuid(), Name = "CanEditAnswer", Description = "Редактирование ответов", Category = "ContentManagement" },
            new { Id = Guid.NewGuid(), Name = "CanDeleteAnswer", Description = "Удаление ответов", Category = "ContentManagement" },
            new { Id = Guid.NewGuid(), Name = "CanModerateContent", Description = "Модерация контента", Category = "ContentManagement" },
            new { Id = Guid.NewGuid(), Name = "CanCreateTopic", Description = "Создание новых тем", Category = "TopicManagement" },
            new { Id = Guid.NewGuid(), Name = "CanEditTopic", Description = "Редактирование тем", Category = "TopicManagement" },
            new { Id = Guid.NewGuid(), Name = "CanDeleteTopic", Description = "Удаление тем", Category = "TopicManagement" },
            new { Id = Guid.NewGuid(), Name = "CanViewReports", Description = "Просмотр отчетов и аналитики", Category = "System" },
            new { Id = Guid.NewGuid(), Name = "CanManageRoles", Description = "Управление ролями пользователей", Category = "System" },
            new { Id = Guid.NewGuid(), Name = "CanManagePermissions", Description = "Управление разрешениями", Category = "System" },
            new { Id = Guid.NewGuid(), Name = "CanAccessSystemSettings", Description = "Доступ к системным настройкам", Category = "System" },
            new { Id = Guid.NewGuid(), Name = "CanPinQuestion", Description = "Закрепление вопросов", Category = "Special" },
            new { Id = Guid.NewGuid(), Name = "CanFeatureContent", Description = "Выделение контента", Category = "Special" },
            new { Id = Guid.NewGuid(), Name = "CanBulkOperations", Description = "Массовые операции", Category = "Special" }
        };

        foreach (var permission in permissions)
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
        // Удаление таблиц в обратном порядке
        Delete.Table("UserClaims");
        Delete.Table("RolePermissions");
        Delete.Table("UserRoles");
        Delete.Table("Permissions");
        Delete.Table("Roles");
        Delete.Table("UserSessions");

        // Удаление добавленных колонок из Users
        Alter.Table("Users")
            .RemoveColumn("PasswordSalt")
            .RemoveColumn("FirstName")
            .RemoveColumn("LastName")
            .RemoveColumn("DateOfBirth")
            .RemoveColumn("PhoneNumber")
            .RemoveColumn("LastLoginAt")
            .RemoveColumn("IsEmailConfirmed")
            .RemoveColumn("IsDeleted");
    }
}