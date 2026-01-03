// using FluentMigrator;
//
// namespace Migrations;
//
// [Migration(20251216000002)]
// /// <summary>
// /// Миграция для добавления составных индексов и дополнительных оптимизаций производительности
// /// </summary>
// public class AddPerformanceIndexes : Migration
// {
//     public override void Up()
//     {
//         // ====================================================================
//         // СОСТАВНЫЕ ИНДЕКСЫ ДЛЯ ЧАСТО ВЫПОЛНЯЕМЫХ ЗАПРОСОВ
//         // ====================================================================
//         
//         // Индекс для получения активных вопросов по теме
//         Create.Index("IX_Questions_TopicId_IsActive_CreatedAt")
//             .OnTable("Questions")
//             .OnColumn("TopicId").Ascending()
//             .OnColumn("IsActive").Ascending()
//             .OnColumn("CreatedAt").Descending();
//
//         // Индекс для получения популярных вопросов пользователя
//         Create.Index("IX_Questions_UserId_IsActive_LikeCount")
//             .OnTable("Questions")
//             .OnColumn("UserId").Ascending()
//             .OnColumn("IsActive").Ascending()
//             .OnColumn("LikeCount").Descending();
//
//         // Индекс для поиска вопросов по тексту (для будущего полнотекстового поиска)
//         Create.Index("IX_Questions_Title_Content")
//             .OnTable("Questions")
//             .OnColumn("Title").Ascending()
//             .OnColumn("Content").Ascending();
//
//         // ====================================================================
//         // ИНДЕКСЫ ДЛЯ ОТВЕТОВ
//         // ====================================================================
//         
//         // Индекс для получения ответов на вопрос, отсортированных по принятию
//         Create.Index("IX_QuestionAnswers_QuestionId_IsAccepted_CreatedAt")
//             .OnTable("QuestionAnswers")
//             .OnColumn("QuestionId").Ascending()
//             .OnColumn("IsAccepted").Ascending()
//             .OnColumn("CreatedAt").Ascending();
//
//         // Индекс для получения ответов пользователя
//         Create.Index("IX_QuestionAnswers_UserId_IsActive_CreatedAt")
//             .OnTable("QuestionAnswers")
//             .OnColumn("UserId").Ascending()
//             .OnColumn("IsActive").Ascending()
//             .OnColumn("CreatedAt").Descending();
//
//         // ====================================================================
//         // СПЕЦИАЛЬНЫЕ ОГРАНИЧЕНИЯ
//         // ====================================================================
//         
//         // Добавляем ограничение на минимальную длину заголовка вопроса
//         Execute.Sql("ALTER TABLE Questions ADD CONSTRAINT CK_Questions_Title_MinLength CHECK (LENGTH(TRIM(Title)) >= 5)");
//
//         // Ограничение на минимальную длину контента вопроса
//         Execute.Sql("ALTER TABLE Questions ADD CONSTRAINT CK_Questions_Content_MinLength CHECK (LENGTH(TRIM(Content)) >= 10)");
//
//         // Ограничение на минимальную длину имени пользователя
//         Execute.Sql("ALTER TABLE Users ADD CONSTRAINT CK_Users_Username_MinLength CHECK (LENGTH(TRIM(Username)) >= 3)");
//
//         // Ограничение на максимальную длину имени пользователя
//         Execute.Sql("ALTER TABLE Users ADD CONSTRAINT CK_Users_Username_MaxLength CHECK (LENGTH(TRIM(Username)) <= 100)");
//
//         // Ограничение на правильность email (базовая проверка)
//         Execute.Sql("ALTER TABLE Users ADD CONSTRAINT CK_Users_Email_Format CHECK (Email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}
//     }
//
//     public override void Down()
//     {
//         // ====================================================================
//         // УДАЛЕНИЕ СОСТАВНЫХ ИНДЕКСОВ
//         // ====================================================================
//         
//         Delete.Index("IX_QuestionAnswers_UserId_IsActive_CreatedAt").OnTable("QuestionAnswers");
//         Delete.Index("IX_QuestionAnswers_QuestionId_IsAccepted_CreatedAt").OnTable("QuestionAnswers");
//         
//         Delete.Index("IX_Questions_Title_Content").OnTable("Questions");
//         Delete.Index("IX_Questions_UserId_IsActive_LikeCount").OnTable("Questions");
//         Delete.Index("IX_Questions_TopicId_IsActive_CreatedAt").OnTable("Questions");
//
//         // ====================================================================
//         // УДАЛЕНИЕ ОГРАНИЧЕНИЙ
//         // ====================================================================
//         
//         Execute.Sql("ALTER TABLE Users DROP CONSTRAINT IF EXISTS CK_Users_Email_Format");
//         Execute.Sql("ALTER TABLE Users DROP CONSTRAINT IF EXISTS CK_Users_Username_MaxLength");
//         Execute.Sql("ALTER TABLE Users DROP CONSTRAINT IF EXISTS CK_Users_Username_MinLength");
//         
//         Execute.Sql("ALTER TABLE Questions DROP CONSTRAINT IF EXISTS CK_Questions_Content_MinLength");
//         Execute.Sql("ALTER TABLE Questions DROP CONSTRAINT IF EXISTS CK_Questions_Title_MinLength");
//     }
// })")
//     }
//
//     public override void Down()
//     {
//         // ====================================================================
//         // УДАЛЕНИЕ СОСТАВНЫХ ИНДЕКСОВ
//         // ====================================================================
//         
//         Delete.Index("IX_QuestionAnswers_UserId_IsActive_CreatedAt").OnTable("QuestionAnswers");
//         Delete.Index("IX_QuestionAnswers_QuestionId_IsAccepted_CreatedAt").OnTable("QuestionAnswers");
//         
//         Delete.Index("IX_Questions_Title_Content").OnTable("Questions");
//         Delete.Index("IX_Questions_UserId_IsActive_LikeCount").OnTable("Questions");
//         Delete.Index("IX_Questions_TopicId_IsActive_CreatedAt").OnTable("Questions");
//
//         // ====================================================================
//         // УДАЛЕНИЕ ОГРАНИЧЕНИЙ
//         // ====================================================================
//         
//         Execute.Sql("ALTER TABLE Users DROP CONSTRAINT IF EXISTS CK_Users_Email_Format");
//         Execute.Sql("ALTER TABLE Users DROP CONSTRAINT IF EXISTS CK_Users_Username_MaxLength");
//         Execute.Sql("ALTER TABLE Users DROP CONSTRAINT IF EXISTS CK_Users_Username_MinLength");
//         
//         Execute.Sql("ALTER TABLE Questions DROP CONSTRAINT IF EXISTS CK_Questions_Content_MinLength");
//         Execute.Sql("ALTER TABLE Questions DROP CONSTRAINT IF EXISTS CK_Questions_Title_MinLength");
//     }
// }