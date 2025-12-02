using FluentMigrator;

namespace Migrations;

[Migration(20240101000000)]
public class InitialCreate : Migration
{
    public override void Up()
    {
        // Таблица Users
        Create.Table("Users")
            .WithColumn("Id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("Username").AsString(100).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("PasswordHash").AsString(255).NotNullable()
            .WithColumn("DisplayName").AsString(100).Nullable()
            .WithColumn("Bio").AsString(1000).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

        // Таблица Topics
        Create.Table("Topics")
            .WithColumn("Id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Description").AsString(1000).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

        // Таблица Questions
        Create.Table("Questions")
            .WithColumn("Id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("TopicId").AsGuid().NotNullable()
            .WithColumn("Title").AsString(500).NotNullable()
            .WithColumn("Content").AsString().NotNullable()
            .WithColumn("ViewCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("LikeCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("AnswerCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

        // Таблица QuestionAnswers
        Create.Table("QuestionAnswers")
            .WithColumn("Id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("QuestionId").AsGuid().NotNullable()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("Content").AsString().NotNullable()
            .WithColumn("ViewCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("IsAccepted").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

        // Таблица QuestionLikes
        Create.Table("QuestionLikes")
            .WithColumn("Id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("QuestionId").AsGuid().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);
    }

    public override void Down()
    {
        Delete.Table("QuestionLikes");
        Delete.Table("QuestionAnswers");
        Delete.Table("Questions");
        Delete.Table("Topics");
        Delete.Table("Users");
    }
}