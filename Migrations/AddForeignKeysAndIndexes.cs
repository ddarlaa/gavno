// using FluentMigrator;
//
// namespace Migrations;
//
// [Migration(20240101000001)]
// public class AddForeignKeysAndIndexes : Migration
// {
//     public override void Up()
//     {
//         // Внешние ключи для Questions
//         Create.ForeignKey("FK_Questions_Users")
//             .FromTable("Questions").ForeignColumn("UserId")
//             .ToTable("Users").PrimaryColumn("Id")
//             .OnDelete(System.Data.Rule.Cascade);
//
//         Create.ForeignKey("FK_Questions_Topics")
//             .FromTable("Questions").ForeignColumn("TopicId")
//             .ToTable("Topics").PrimaryColumn("Id")
//             .OnDelete(System.Data.Rule.None);
//
//         // Индексы для Users
//         Create.Index("IX_Users_Username")
//             .OnTable("Users")
//             .OnColumn("Username").Ascending()
//             .WithOptions().Unique();
//
//         Create.Index("IX_Users_Email")
//             .OnTable("Users")
//             .OnColumn("Email").Ascending()
//             .WithOptions().Unique();
//
//         Create.Index("IX_Users_IsActive")
//             .OnTable("Users")
//             .OnColumn("IsActive").Ascending();
//
//         // Индексы для Topics
//         Create.Index("IX_Topics_Name")
//             .OnTable("Topics")
//             .OnColumn("Name").Ascending()
//             .WithOptions().Unique();
//
//         Create.Index("IX_Topics_IsActive")
//             .OnTable("Topics")
//             .OnColumn("IsActive").Ascending();
//
//         // Индексы для Questions
//         Create.Index("IX_Questions_UserId")
//             .OnTable("Questions")
//             .OnColumn("UserId").Ascending();
//
//         Create.Index("IX_Questions_TopicId")
//             .OnTable("Questions")
//             .OnColumn("TopicId").Ascending();
//
//         Create.Index("IX_Questions_CreatedAt")
//             .OnTable("Questions")
//             .OnColumn("CreatedAt").Descending();
//
//         Create.Index("IX_Questions_IsActive")
//             .OnTable("Questions")
//             .OnColumn("IsActive").Ascending();
//     }
//
//     public override void Down()
//     {
//         Delete.ForeignKey("FK_Questions_Users").OnTable("Questions");
//         Delete.ForeignKey("FK_Questions_Topics").OnTable("Questions");
//         
//         Delete.Index("IX_Users_Username").OnTable("Users");
//         Delete.Index("IX_Users_Email").OnTable("Users");
//         Delete.Index("IX_Users_IsActive").OnTable("Users");
//         
//         Delete.Index("IX_Topics_Name").OnTable("Topics");
//         Delete.Index("IX_Topics_IsActive").OnTable("Topics");
//         
//         Delete.Index("IX_Questions_UserId").OnTable("Questions");
//         Delete.Index("IX_Questions_TopicId").OnTable("Questions");
//         Delete.Index("IX_Questions_CreatedAt").OnTable("Questions");
//         Delete.Index("IX_Questions_IsActive").OnTable("Questions");
//     }
// }