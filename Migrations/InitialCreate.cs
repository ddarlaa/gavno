using FluentMigrator;
using System.Data;

namespace Migrations;

[Migration(202512090000)]
public class InitialCreate : Migration
{
    // Создание таблиц и колонок, здесь только каркас таблиц
    public override void Up()
    {
        // Users
        Create.Table("Users")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Username").AsString(50).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("PasswordHash").AsString(512).NotNullable()
            .WithColumn("FirstName").AsString(100).Nullable()
            .WithColumn("LastName").AsString(100).Nullable()
            .WithColumn("DisplayName").AsString(255).Nullable()
            .WithColumn("Bio").AsString(1000).Nullable()
            .WithColumn("DateOfBirth").AsDateTime().Nullable()
            .WithColumn("PhoneNumber").AsString(20).Nullable()
            .WithColumn("LastLoginAt").AsDateTime().Nullable()
            .WithColumn("IsEmailConfirmed").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime);

        // Roles
        Create.Table("Roles")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString(50).NotNullable()
            .WithColumn("Description").AsString(255).Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable();

        // Permissions
        Create.Table("Permissions")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Description").AsString(500).Nullable()
            .WithColumn("Category").AsString(50).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime);

        // RolePermissions
        Create.Table("RolePermissions")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("RoleId").AsGuid().NotNullable()
            .WithColumn("PermissionId").AsGuid().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime);

        // UserRoles
        Create.Table("UserRoles")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("RoleId").AsGuid().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("AssignedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("AssignedBy").AsGuid().Nullable();

        // UserClaims
        Create.Table("UserClaims")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("ClaimType").AsString(100).NotNullable()
            .WithColumn("ClaimValue").AsString(500).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime().NotNullable()  //ДОБАВЛЕНО
                .WithDefault(SystemMethods.CurrentUTCDateTime);

        // UserSessions
        Create.Table("UserSessions")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("RefreshTokenHash").AsString(512).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("ExpiresAt").AsDateTime().NotNullable()
            .WithColumn("DeviceInfo").AsString(500).Nullable()
            .WithColumn("IpAddress").AsString(45).Nullable()
            .WithColumn("IsRevoked").AsBoolean().NotNullable().WithDefaultValue(false);

        // Topics
        Create.Table("Topics")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Description").AsString(1000).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable()
                .WithDefaultValue(true);

        // Questions
        Create.Table("Questions")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("TopicId").AsGuid().NotNullable()
            .WithColumn("Title").AsString(500).NotNullable()
            .WithColumn("Content").AsString().NotNullable()
            .WithColumn("ViewCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("LikeCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("AnswerCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable()
                .WithDefaultValue(true);

        // QuestionAnswers
        Create.Table("QuestionAnswers")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("QuestionId").AsGuid().NotNullable()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("Content").AsString().NotNullable()
            .WithColumn("ViewCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("IsAccepted").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable()
                .WithDefaultValue(true);

        // QuestionLikes
        Create.Table("QuestionLikes")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("QuestionId").AsGuid().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable()
                .WithDefaultValue(true);

        // Внешние ключи не поддерживаются FluentMigrator в SQLite
        // Они будут управляться на уровне приложения через EF Core
        // Execute.Sql("PRAGMA foreign_keys = ON;");

        // Создание уникальных ограничений
        Create.UniqueConstraint("UQ_Users_Username")
            .OnTable("Users").Column("Username");

        Create.UniqueConstraint("UQ_Users_Email")
            .OnTable("Users").Column("Email");

        Create.UniqueConstraint("UQ_Roles_Name")
            .OnTable("Roles").Column("Name");

        Create.UniqueConstraint("UQ_Permissions_Name")
            .OnTable("Permissions").Column("Name");

        Create.UniqueConstraint("UQ_RolePermissions_RolePermission")
            .OnTable("RolePermissions").Columns("RoleId", "PermissionId");

        Create.UniqueConstraint("UQ_UserRoles_UserRole")
            .OnTable("UserRoles").Columns("UserId", "RoleId");

        Create.UniqueConstraint("UQ_QuestionLikes_UserQuestion")
            .OnTable("QuestionLikes").Columns("UserId", "QuestionId");

        // Создание индексов для производительности
        Create.Index("IX_Users_Username").OnTable("Users").OnColumn("Username");
        Create.Index("IX_Users_Email").OnTable("Users").OnColumn("Email");
        Create.Index("IX_UserRoles_UserId").OnTable("UserRoles").OnColumn("UserId");
        Create.Index("IX_UserRoles_RoleId").OnTable("UserRoles").OnColumn("RoleId");
        Create.Index("IX_Questions_UserId").OnTable("Questions").OnColumn("UserId");
        Create.Index("IX_Questions_TopicId").OnTable("Questions").OnColumn("TopicId");
        Create.Index("IX_QuestionAnswers_QuestionId").OnTable("QuestionAnswers").OnColumn("QuestionId");
        Create.Index("IX_QuestionAnswers_UserId").OnTable("QuestionAnswers").OnColumn("UserId");
        Create.Index("IX_QuestionLikes_QuestionId").OnTable("QuestionLikes").OnColumn("QuestionId");
        Create.Index("IX_QuestionLikes_UserId").OnTable("QuestionLikes").OnColumn("UserId");

        // Создание внешних ключей (PostgreSQL поддерживает все типы FK) - ПОСЛЕ создания таблиц!
        Create.ForeignKey("FK_UserRoles_RoleId")
            .FromTable("UserRoles").ForeignColumn("RoleId")
            .ToTable("Roles").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_UserRoles_AssignedBy")
            .FromTable("UserRoles").ForeignColumn("AssignedBy")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.SetNull);

        Create.ForeignKey("FK_UserRoles_UserId")
            .FromTable("UserRoles").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_RolePermissions_RoleId")
            .FromTable("RolePermissions").ForeignColumn("RoleId")
            .ToTable("Roles").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_RolePermissions_PermissionId")
            .FromTable("RolePermissions").ForeignColumn("PermissionId")
            .ToTable("Permissions").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_UserClaims_UserId")
            .FromTable("UserClaims").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_UserSessions_UserId")
            .FromTable("UserSessions").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_Questions_UserId")
            .FromTable("Questions").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_Questions_TopicId")
            .FromTable("Questions").ForeignColumn("TopicId")
            .ToTable("Topics").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_QuestionAnswers_QuestionId")
            .FromTable("QuestionAnswers").ForeignColumn("QuestionId")
            .ToTable("Questions").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_QuestionAnswers_UserId")
            .FromTable("QuestionAnswers").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_QuestionLikes_QuestionId")
            .FromTable("QuestionLikes").ForeignColumn("QuestionId")
            .ToTable("Questions").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_QuestionLikes_UserId")
            .FromTable("QuestionLikes").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);
    }

    // Откат: удаляет все таблицы в порядке, обратном созданию (сначала зависящие, потом базовые).
    public override void Down()
    {
        Delete.Table("QuestionLikes");
        Delete.Table("QuestionAnswers");
        Delete.Table("Questions");
        Delete.Table("Topics");
        Delete.Table("UserSessions");
        Delete.Table("UserClaims");
        Delete.Table("RolePermissions");
        Delete.Table("UserRoles");
        Delete.Table("Permissions");
        Delete.Table("Roles");
        Delete.Table("Users");
    }
}