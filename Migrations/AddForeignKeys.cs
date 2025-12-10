using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Model;

namespace Migrations;

// Внешние ключи нужны для того, чтобы можно было ссылаться на данные из других таблиц. Обеспечивают целостность данных
// Поддерживает каскадные действия (конкретно у меня только удаление)

/// <summary>
/// Миграция для добавления внешних ключей и связей между таблицами
/// </summary>

[Migration(20251210000001)]
public class AddForeignKeys : Migration
{
    // Создание внешних ключей
    public override void Up()
    {
        // ====================================================================
        // ВНЕШНИЕ КЛЮЧИ ДЛЯ QUESTIONS
        // ====================================================================
        
        // FK: Questions.UserId -> Users.Id
        Create.ForeignKey("FK_Questions_Users_UserId")
            .FromTable("Questions").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // FK: Questions.TopicId -> Topics.Id
        Create.ForeignKey("FK_Questions_Topics_TopicId")
            .FromTable("Questions").ForeignColumn("TopicId")
            .ToTable("Topics").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // ====================================================================
        // ВНЕШНИЕ КЛЮЧИ ДЛЯ QUESTION ANSWERS
        // ====================================================================
        
        // FK: QuestionAnswers.QuestionId -> Questions.Id
        Create.ForeignKey("FK_QuestionAnswers_Questions_QuestionId")
            .FromTable("QuestionAnswers").ForeignColumn("QuestionId")
            .ToTable("Questions").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // FK: QuestionAnswers.UserId -> Users.Id
        Create.ForeignKey("FK_QuestionAnswers_Users_UserId")
            .FromTable("QuestionAnswers").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // ====================================================================
        // ВНЕШНИЕ КЛЮЧИ ДЛЯ QUESTION LIKES
        // ====================================================================
        
        // FK: QuestionLikes.QuestionId -> Questions.Id
        Create.ForeignKey("FK_QuestionLikes_Questions_QuestionId")
            .FromTable("QuestionLikes").ForeignColumn("QuestionId")
            .ToTable("Questions").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // FK: QuestionLikes.UserId -> Users.Id
        Create.ForeignKey("FK_QuestionLikes_Users_UserId")
            .FromTable("QuestionLikes").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);
    }

    public override void Down()
    {
        // ====================================================================
        // УДАЛЕНИЕ ВНЕШНИХ КЛЮЧЕЙ В ОБРАТНОМ ПОРЯДКЕ
        // ====================================================================
        
        // Удаляем FK из QuestionLikes
        Delete.ForeignKey("FK_QuestionLikes_Users_UserId").OnTable("QuestionLikes");
        Delete.ForeignKey("FK_QuestionLikes_Questions_QuestionId").OnTable("QuestionLikes");
        
        // Удаляем FK из QuestionAnswers
        Delete.ForeignKey("FK_QuestionAnswers_Users_UserId").OnTable("QuestionAnswers");
        Delete.ForeignKey("FK_QuestionAnswers_Questions_QuestionId").OnTable("QuestionAnswers");
        
        // Удаляем FK из Questions
        Delete.ForeignKey("FK_Questions_Topics_TopicId").OnTable("Questions");
        Delete.ForeignKey("FK_Questions_Users_UserId").OnTable("Questions");
    }
}