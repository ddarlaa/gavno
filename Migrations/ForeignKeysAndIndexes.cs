using FluentMigrator;

namespace Migrations;

[Migration(20240101000001)]
public class AddForeignKeysAndIndexes : Migration
{
    public override void Up()
    {
        // Внешние ключи для Questions
        Create.ForeignKey("FK_Questions_Users")
            .FromTable("Questions").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_Questions_Topics")
            .FromTable("Questions").ForeignColumn("TopicId")
            .ToTable("Topics").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // Внешние ключи для QuestionAnswers
        Create.ForeignKey("FK_QuestionAnswers_Questions")
            .FromTable("QuestionAnswers").ForeignColumn("QuestionId")
            .ToTable("Questions").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_QuestionAnswers_Users")
            .FromTable("QuestionAnswers").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // Внешние ключи для QuestionLikes
        Create.ForeignKey("FK_QuestionLikes_Questions")
            .FromTable("QuestionLikes").ForeignColumn("QuestionId")
            .ToTable("Questions").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_QuestionLikes_Users")
            .FromTable("QuestionLikes").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // Индексы для Users
        Create.Index("IX_Users_Username")
            .OnTable("Users")
            .OnColumn("Username").Ascending()
            .WithOptions().Unique();

        Create.Index("IX_Users_Email")
            .OnTable("Users")
            .OnColumn("Email").Ascending()
            .WithOptions().Unique();

        Create.Index("IX_Users_IsActive")
            .OnTable("Users")
            .OnColumn("IsActive").Ascending();

        // Индексы для Topics
        Create.Index("IX_Topics_Name")
            .OnTable("Topics")
            .OnColumn("Name").Ascending()
            .WithOptions().Unique();

        Create.Index("IX_Topics_IsActive")
            .OnTable("Topics")
            .OnColumn("IsActive").Ascending();

        // Индексы для Questions
        Create.Index("IX_Questions_UserId")
            .OnTable("Questions")
            .OnColumn("UserId").Ascending();

        Create.Index("IX_Questions_TopicId")
            .OnTable("Questions")
            .OnColumn("TopicId").Ascending();

        Create.Index("IX_Questions_CreatedAt")
            .OnTable("Questions")
            .OnColumn("CreatedAt").Descending();

        Create.Index("IX_Questions_IsActive")
            .OnTable("Questions")
            .OnColumn("IsActive").Ascending();

        // Индексы для QuestionAnswers
        Create.Index("IX_QuestionAnswers_QuestionId")
            .OnTable("QuestionAnswers")
            .OnColumn("QuestionId").Ascending();

        Create.Index("IX_QuestionAnswers_UserId")
            .OnTable("QuestionAnswers")
            .OnColumn("UserId").Ascending();

        Create.Index("IX_QuestionAnswers_IsAccepted")
            .OnTable("QuestionAnswers")
            .OnColumn("IsAccepted").Ascending();

        Create.Index("IX_QuestionAnswers_IsActive")
            .OnTable("QuestionAnswers")
            .OnColumn("IsActive").Ascending();

        Create.Index("IX_QuestionAnswers_CreatedAt")
            .OnTable("QuestionAnswers")
            .OnColumn("CreatedAt").Descending();

        // Индексы для QuestionLikes
        // Уникальный индекс для предотвращения дублирования лайков
        Create.Index("IX_QuestionLikes_QuestionId_UserId")
            .OnTable("QuestionLikes")
            .OnColumn("QuestionId").Ascending()
            .OnColumn("UserId").Ascending()
            .WithOptions().Unique();

        Create.Index("IX_QuestionLikes_QuestionId")
            .OnTable("QuestionLikes")
            .OnColumn("QuestionId").Ascending();

        Create.Index("IX_QuestionLikes_UserId")
            .OnTable("QuestionLikes")
            .OnColumn("UserId").Ascending();
    }

    public override void Down()
    {
        // Удаляем индексы в обратном порядке
        Delete.Index("IX_QuestionLikes_QuestionId_UserId").OnTable("QuestionLikes");
        Delete.Index("IX_QuestionLikes_QuestionId").OnTable("QuestionLikes");
        Delete.Index("IX_QuestionLikes_UserId").OnTable("QuestionLikes");

        Delete.Index("IX_QuestionAnswers_CreatedAt").OnTable("QuestionAnswers");
        Delete.Index("IX_QuestionAnswers_IsActive").OnTable("QuestionAnswers");
        Delete.Index("IX_QuestionAnswers_IsAccepted").OnTable("QuestionAnswers");
        Delete.Index("IX_QuestionAnswers_UserId").OnTable("QuestionAnswers");
        Delete.Index("IX_QuestionAnswers_QuestionId").OnTable("QuestionAnswers");

        Delete.Index("IX_Questions_IsActive").OnTable("Questions");
        Delete.Index("IX_Questions_CreatedAt").OnTable("Questions");
        Delete.Index("IX_Questions_TopicId").OnTable("Questions");
        Delete.Index("IX_Questions_UserId").OnTable("Questions");

        Delete.Index("IX_Topics_IsActive").OnTable("Topics");
        Delete.Index("IX_Topics_Name").OnTable("Topics");

        Delete.Index("IX_Users_IsActive").OnTable("Users");
        Delete.Index("IX_Users_Email").OnTable("Users");
        Delete.Index("IX_Users_Username").OnTable("Users");

        // Удаляем внешние ключи в обратном порядке
        Delete.ForeignKey("FK_QuestionLikes_Users").OnTable("QuestionLikes");
        Delete.ForeignKey("FK_QuestionLikes_Questions").OnTable("QuestionLikes");

        Delete.ForeignKey("FK_QuestionAnswers_Users").OnTable("QuestionAnswers");
        Delete.ForeignKey("FK_QuestionAnswers_Questions").OnTable("QuestionAnswers");

        Delete.ForeignKey("FK_Questions_Topics").OnTable("Questions");
        Delete.ForeignKey("FK_Questions_Users").OnTable("Questions");
    }
}