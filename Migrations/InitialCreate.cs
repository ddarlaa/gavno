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
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("AvatarFileId").AsGuid().Nullable(); // Добавлено: FK к FileMetadata для аватара

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
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("RoleId").AsGuid().NotNullable()
            .WithColumn("AssignedAt").AsDateTime().NotNullable()
            .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("AssignedBy").AsGuid().Nullable();
        
        Create.PrimaryKey("PK_UserRoles")
            .OnTable("UserRoles")
            .Columns("UserId", "RoleId");


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
            .WithColumn("UpdatedAt").AsDateTime().NotNullable()
            .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("ExpiresAt").AsDateTime().Nullable()
            .WithColumn("DeviceInfo").AsString(500).Nullable()
            .WithColumn("IpAddress").AsString(45).Nullable()
            .WithColumn("IsRevoked").AsBoolean().NotNullable().WithDefaultValue(false);

        // FileMetadata
        Create.Table("FileMetadata")
            .WithColumn("Id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("FileName").AsString(255).NotNullable()
            .WithColumn("OriginalFileName").AsString(255).NotNullable()
            .WithColumn("ContentType").AsString(100).NotNullable()
            .WithColumn("Size").AsInt64().NotNullable()
            .WithColumn("UploadedById").AsGuid().NotNullable()
            .WithColumn("UploadedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("Path").AsString(255).NotNullable()
            .WithColumn("Hash").AsString(64).NotNullable()  // SHA256 = 64 hex chars
            .WithColumn("IsPublic").AsBoolean().NotNullable()
            .WithColumn("ExpiresAt").AsDateTime().Nullable()
            .WithColumn("DownloadCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("Width").AsInt32().Nullable()
            .WithColumn("Height").AsInt32().Nullable()
            .WithColumn("CameraModel").AsString(100).Nullable()
            .WithColumn("DateTaken").AsDateTime().Nullable()
            .WithColumn("Latitude").AsDouble().Nullable()
            .WithColumn("Longitude").AsDouble().Nullable()
            .WithColumn("Orientation").AsInt32().Nullable()
            .WithColumn("SmallThumbnailPath").AsString(255).Nullable()
            .WithColumn("MediumThumbnailPath").AsString(255).Nullable()
            .WithColumn("IsAvatar").AsBoolean().NotNullable().WithDefaultValue(false) // Добавлено: для определения, является ли файл аватаром
            .WithColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false); // Добавлено: для мягкого удаления

        // UploadSessions
        Create.Table("ChunkUploadSessions")
            .WithColumn("UploadId").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("FileName").AsString(255).NotNullable()
            .WithColumn("ContentType").AsString(100).NotNullable()
            .WithColumn("UploadedBytes").AsInt64().NotNullable().WithDefaultValue(0)
            .WithColumn("TotalChunks").AsInt32().NotNullable()
            .WithColumn("UploadedChunks").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("ExpiresAt").AsDateTime().Nullable()
            .WithColumn("IsPublic").AsBoolean().Nullable().WithDefaultValue(false)
            .WithColumn("FileId").AsGuid().Nullable() // Добавлено: FK к FileMetadata
            .WithColumn("UploadedChunkIndexes").AsString().NotNullable().WithDefaultValue("");

        // Topics
        Create.Table("Topics")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Description").AsString(1000).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable()
                .WithDefaultValue(true)
            .WithColumn("ImageId").AsGuid().Nullable(); // Добавлено: FK к FileMetadata для изображения темы

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
                .WithDefaultValue(true)
            .WithColumn("ImageId").AsGuid().Nullable(); // Добавлено: FK к FileMetadata для изображения вопроса

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
        Create.Index("IX_FileMetadata_Hash").OnTable("FileMetadata").OnColumn("Hash");
        Create.Index("IX_FileMetadata_UploadedById").OnTable("FileMetadata").OnColumn("UploadedById");
        Create.Index("IX_FileMetadata_ContentType").OnTable("FileMetadata").OnColumn("ContentType");
        Create.Index("IX_ChunkUploadSessions_UserId").OnTable("ChunkUploadSessions").OnColumn("UserId");

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

        // Новые внешние ключи для FileMetadata
        Create.ForeignKey("FK_Users_FileMetadata_AvatarFileId")
            .FromTable("Users").ForeignColumn("AvatarFileId")
            .ToTable("FileMetadata").PrimaryColumn("Id")
            .OnDelete(Rule.SetNull);

        Create.ForeignKey("FK_Questions_FileMetadata_ImageId")
            .FromTable("Questions").ForeignColumn("ImageId")
            .ToTable("FileMetadata").PrimaryColumn("Id")
            .OnDelete(Rule.SetDefault);

        Create.ForeignKey("FK_Topics_FileMetadata_ImageId")
            .FromTable("Topics").ForeignColumn("ImageId")
            .ToTable("FileMetadata").PrimaryColumn("Id")
            .OnDelete(Rule.SetDefault);

        Create.ForeignKey("FK_ChunkUploadSessions_UserId")
            .FromTable("ChunkUploadSessions").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);
    }

    // Откат: удаляет все таблицы в порядке, обратном созданию (сначала зависящие, потом базовые).
    public override void Down()
    {
        Delete.Table("ChunkUploadSessions");
        Delete.Table("FileMetadata");
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