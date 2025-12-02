using IceBreakerApp.Domain;
using IceBreakerApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        // DbSets для всех сущностей
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionAnswer> QuestionAnswers { get; set; }
        public DbSet<QuestionLike> QuestionLikes { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конфигурация для BaseEntity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.ClrType.IsSubclassOf(typeof(BaseEntity)))
                {
                    entityType.FindProperty(nameof(BaseEntity.CreatedAt))?.SetDefaultValueSql("CURRENT_TIMESTAMP");
                    entityType.FindProperty(nameof(BaseEntity.UpdatedAt))?.SetDefaultValueSql("CURRENT_TIMESTAMP");
                }
            }

            // Конфигурация для Question
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Title).IsRequired().HasMaxLength(500);
                entity.Property(q => q.Content).IsRequired();
                entity.Property(q => q.ViewCount).HasDefaultValue(0);
                entity.Property(q => q.LikeCount).HasDefaultValue(0);
                entity.Property(q => q.AnswerCount).HasDefaultValue(0);

                // Связи
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(q => q.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Topic>()
                    .WithMany()
                    .HasForeignKey(q => q.TopicId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Индексы
                entity.HasIndex(q => q.UserId);
                entity.HasIndex(q => q.TopicId);
                entity.HasIndex(q => q.IsActive);
                entity.HasIndex(q => q.CreatedAt);
            });

            // Конфигурация для QuestionAnswer
            modelBuilder.Entity<QuestionAnswer>(entity =>
            {
                entity.HasKey(qa => qa.Id);
                entity.Property(qa => qa.Content).IsRequired();
                entity.Property(qa => qa.ViewCount).HasDefaultValue(0);
                entity.Property(qa => qa.IsAccepted).HasDefaultValue(false);

                // Связи
                entity.HasOne<Question>()
                    .WithMany()
                    .HasForeignKey(qa => qa.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(qa => qa.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Индексы
                entity.HasIndex(qa => qa.QuestionId);
                entity.HasIndex(qa => qa.UserId);
                entity.HasIndex(qa => qa.IsActive);
                entity.HasIndex(qa => qa.IsAccepted);
                entity.HasIndex(qa => qa.CreatedAt);
            });

            // Конфигурация для QuestionLike
            modelBuilder.Entity<QuestionLike>(entity =>
            {
                entity.HasKey(ql => ql.Id);

                // Связи
                entity.HasOne<Question>()
                    .WithMany()
                    .HasForeignKey(ql => ql.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(ql => ql.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Уникальный индекс для предотвращения дублирования лайков
                entity.HasIndex(ql => new { ql.QuestionId, ql.UserId }).IsUnique();
                entity.HasIndex(ql => ql.QuestionId);
                entity.HasIndex(ql => ql.UserId);
            });

            // Конфигурация для Topic
            modelBuilder.Entity<Topic>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Description).HasMaxLength(1000);

                // Уникальный индекс для имени
                entity.HasIndex(t => t.Name).IsUnique();
                entity.HasIndex(t => t.IsActive);
            });

            // Конфигурация для User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.DisplayName).HasMaxLength(100);
                entity.Property(u => u.Bio).HasMaxLength(1000);

                // Уникальные индексы
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.IsActive);
            });
        }
    }
}