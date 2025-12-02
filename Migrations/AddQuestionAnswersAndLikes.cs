using FluentMigrator;

namespace Migrations;

[Migration(20240101000002)]
public class AddQuestionAnswersAndLikes : Migration
{
    public override void Up()
    {
        // Таблица QuestionAnswers
        Create.Table("QuestionAnswers")
            .WithColumn("Id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("Content").AsString().NotNullable()
            .WithColumn("IsAccepted").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("QuestionId").AsGuid().NotNullable()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

        // Таблица QuestionLikes
        Create.Table("QuestionLikes")
            .WithColumn("Id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("QuestionId").AsGuid().NotNullable()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);

        // Внешние ключи
        Create.ForeignKey("FK_QuestionAnswers_Questions")
            .FromTable("QuestionAnswers").ForeignColumn("QuestionId")
            .ToTable("Questions").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_QuestionAnswers_Users")
            .FromTable("QuestionAnswers").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.None);

        Create.ForeignKey("FK_QuestionLikes_Questions")
            .FromTable("QuestionLikes").ForeignColumn("QuestionId")
            .ToTable("Questions").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_QuestionLikes_Users")
            .FromTable("QuestionLikes").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // Индексы
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
        Delete.Table("QuestionLikes");
        Delete.Table("QuestionAnswers");
    }
}