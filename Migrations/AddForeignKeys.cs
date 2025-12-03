using FluentMigrator;

[Migration(20240101000001)]
public class AddForeignKeys : Migration
{
    public override void Up()
    {
        Create.ForeignKey("FK_Questions_Users")
            .FromTable("Questions").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_Questions_Topics")
            .FromTable("Questions").ForeignColumn("TopicId")
            .ToTable("Topics").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_QuestionAnswers_Questions")
            .FromTable("QuestionAnswers").ForeignColumn("QuestionId")
            .ToTable("Questions").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_QuestionAnswers_Users")
            .FromTable("QuestionAnswers").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_QuestionLikes_Questions")
            .FromTable("QuestionLikes").ForeignColumn("QuestionId")
            .ToTable("Questions").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_QuestionLikes_Users")
            .FromTable("QuestionLikes").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_QuestionLikes_Users").OnTable("QuestionLikes");
        Delete.ForeignKey("FK_QuestionLikes_Questions").OnTable("QuestionLikes");
        Delete.ForeignKey("FK_QuestionAnswers_Users").OnTable("QuestionAnswers");
        Delete.ForeignKey("FK_QuestionAnswers_Questions").OnTable("QuestionAnswers");
        Delete.ForeignKey("FK_Questions_Topics").OnTable("Questions");
        Delete.ForeignKey("FK_Questions_Users").OnTable("Questions");
    }
}