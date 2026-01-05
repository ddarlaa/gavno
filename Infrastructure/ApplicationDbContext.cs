using IceBreakerApp.Domain;
using IceBreakerApp.Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionAnswer> QuestionAnswers { get; set; }
        public DbSet<QuestionLike> QuestionLikes { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserClaim> UserClaims { get; set; }
        public DbSet<FileMetadata> FileMetadata { get; set; } // Добавлено
        public DbSet<UploadSession> ChunkUploadSessions { get; set; } // Добавлено
        public DbSet<QuestionAttachment> QuestionAttachments { get; set; } // Добавлено
        public DbSet<TopicAttachment> TopicAttachments { get; set; } // Добавлено


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // FluentMigrator управляет схемой БД, EF Core используется только как ORM
            // Убираем OnModelCreating, чтобы избежать конфликтов
            base.OnModelCreating(modelBuilder);

            // Конвертер для DateTime в UTC для PostgreSQL
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.ToUniversalTime(), // При сохранении всегда делаем UTC
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)); // При чтении всегда делаем UTC

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? v.Value.ToUniversalTime() : v, // При сохранении всегда делаем UTC
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v); // При чтении всегда делаем UTC

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }

            // BaseEntity defaults
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.ClrType.IsSubclassOf(typeof(BaseEntity)))
                {
                    entityType.FindProperty(nameof(BaseEntity.CreatedAt))?.SetDefaultValueSql("CURRENT_TIMESTAMP");
                    entityType.FindProperty(nameof(BaseEntity.UpdatedAt))?.SetDefaultValueSql("CURRENT_TIMESTAMP");
                }
            }

            // Question
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Title).IsRequired().HasMaxLength(500);
                entity.Property(q => q.Content).IsRequired();
                entity.Property(q => q.ViewCount).HasDefaultValue(0);
                entity.Property(q => q.LikeCount).HasDefaultValue(0);
                entity.Property(q => q.AnswerCount).HasDefaultValue(0);
                entity.Property(q => q.IsActive).HasDefaultValue(true);

                // ОДНА связь с User
                entity.HasOne(q => q.User)
                    .WithMany(u => u.Questions)
                    .HasForeignKey(q => q.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ОДНА связь с Topic
                entity.HasOne(q => q.Topic)
                    .WithMany(t => t.Questions) // Добавь обратное свойство в Topic
                    .HasForeignKey(q => q.TopicId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(q => q.UserId);
                entity.HasIndex(q => q.TopicId);
                entity.HasIndex(q => q.IsActive);
                entity.HasIndex(q => q.CreatedAt);
            });

            // QuestionAnswer
            modelBuilder.Entity<QuestionAnswer>(entity =>
            {
                entity.HasKey(qa => qa.Id);
                entity.Property(qa => qa.Content).IsRequired();
                entity.Property(qa => qa.ViewCount).HasDefaultValue(0);
                entity.Property(qa => qa.IsAccepted).HasDefaultValue(false);
                entity.Property(qa => qa.IsActive).HasDefaultValue(true);

                // ОДНА связь с Question
                entity.HasOne(qa => qa.Question)
                    .WithMany(q => q.Answers) // Используй навигационное свойство
                    .HasForeignKey(qa => qa.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ОДНА связь с User
                entity.HasOne(qa => qa.User)
                    .WithMany(u => u.Answers)
                    .HasForeignKey(qa => qa.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(qa => qa.QuestionId);
                entity.HasIndex(qa => qa.UserId);
                entity.HasIndex(qa => qa.IsActive);
                entity.HasIndex(qa => qa.IsAccepted);
                entity.HasIndex(qa => qa.CreatedAt);
            });

            // QuestionLike
            modelBuilder.Entity<QuestionLike>(entity =>
            {
                entity.HasKey(ql => ql.Id);

                // ОДНА связь с Question
                entity.HasOne(ql => ql.Question)
                    .WithMany(q => q.Likes) // Используй навигационное свойство
                    .HasForeignKey(ql => ql.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ОДНА связь с User
                entity.HasOne(ql => ql.User)
                    .WithMany(u => u.Likes)
                    .HasForeignKey(ql => ql.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(ql => new { ql.QuestionId, ql.UserId }).IsUnique();
                entity.HasIndex(ql => ql.QuestionId);
                entity.HasIndex(ql => ql.UserId);
            });

            // Topic
            modelBuilder.Entity<Topic>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Description).HasMaxLength(1000);
                entity.Property(t => t.IsActive).HasDefaultValue(true);
                entity.HasIndex(t => t.Name).IsUnique();
                entity.HasIndex(t => t.IsActive);

                
            });

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.PasswordHash).IsRequired();

                entity.Property(u => u.DisplayName).HasMaxLength(100);
                entity.Property(u => u.Bio).HasMaxLength(1000);
                entity.Property(u => u.FirstName).HasMaxLength(100);
                entity.Property(u => u.LastName).HasMaxLength(100);
                entity.Property(u => u.PhoneNumber).HasMaxLength(20);

                entity.Property(u => u.IsActive).HasDefaultValue(true);
                entity.Property(u => u.IsDeleted).HasDefaultValue(false);
                entity.Property(u => u.IsEmailConfirmed).HasDefaultValue(false);

                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.IsActive);
                entity.HasIndex(u => u.IsDeleted);


                entity.HasMany(u => u.UploadedFiles)
                    .WithOne(f => f.UploadedBy)
                    .HasForeignKey(f => f.UploadedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.ChunkUploadSessions)
                    .WithOne(s => s.User)
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Связь User с FileMetadata для аватара
                entity.HasOne(u => u.AvatarFile)
                    .WithOne()
                    .HasForeignKey<User>(u => u.AvatarFileId)
                    .OnDelete(DeleteBehavior
                        .SetNull); // При удалении файла аватара, AvatarFileId в User становится null
            });

            // UserSession
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(us => us.Id);
                entity.Property(us => us.RefreshTokenHash).IsRequired().HasMaxLength(512);
                entity.Property(us => us.DeviceInfo).HasMaxLength(500);
                entity.Property(us => us.IpAddress).HasMaxLength(45);
                entity.Property(us => us.IsRevoked).HasDefaultValue(false);

                // ОДНА связь с User - используй навигационное свойство
                entity.HasOne(us => us.User)
                    .WithMany(u => u.Sessions)
                    .HasForeignKey(us => us.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(us => us.UserId);
                entity.HasIndex(us => us.RefreshTokenHash).IsUnique();
                entity.HasIndex(us => us.ExpiresAt);
                entity.HasIndex(us => us.IsRevoked);
            });

            // Role
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
                entity.Property(r => r.Description).HasMaxLength(500);
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(r => r.Name).IsUnique();
            });

            // Permission
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Description).HasMaxLength(500);
                entity.Property(p => p.Category).HasMaxLength(50);
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(p => p.Name).IsUnique();
                entity.HasIndex(p => p.Category);
            });

            // UserRole (составной ключ)
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(ur => ur.UserId);
                entity.HasIndex(ur => ur.RoleId);
                entity.HasIndex(ur => ur.AssignedBy);
            });

            // RolePermission (составной ключ)
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

                entity.HasOne(rp => rp.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(rp => rp.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(rp => rp.RoleId);
                entity.HasIndex(rp => rp.PermissionId);
            });

            // UserClaim
            modelBuilder.Entity<UserClaim>(entity =>
            {
                entity.HasKey(uc => uc.Id);
                entity.Property(uc => uc.ClaimType).IsRequired().HasMaxLength(100);
                entity.Property(uc => uc.ClaimValue).IsRequired().HasMaxLength(500);

                // ОДНА связь с User - используй навигационное свойство
                entity.HasOne(uc => uc.User)
                    .WithMany(u => u.UserClaims)
                    .HasForeignKey(uc => uc.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(uc => uc.UserId);
                entity.HasIndex(uc => uc.ClaimType);
                entity.HasIndex(uc => new { uc.ClaimType, uc.ClaimValue });
            });

            // FileMetadata 
            modelBuilder.Entity<FileMetadata>(entity =>
            {
                entity.HasKey(f => f.Id);

                entity.Property(f => f.FileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(f => f.OriginalFileName)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(f => f.ContentType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(f => f.Path)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(f => f.Hash)
                    .IsRequired()
                    .HasMaxLength(64); // SHA256 hex string

                entity.Property(f => f.SmallThumbnailPath)
                    .HasMaxLength(500);

                entity.Property(f => f.MediumThumbnailPath)
                    .HasMaxLength(500);

                entity.Property(f => f.CameraModel)
                    .HasMaxLength(100);

                // Soft delete поддержка
                entity.HasQueryFilter(f => !f.IsDeleted);

                // Связь с User
                entity.HasOne(f => f.UploadedBy)
                    .WithMany(u => u.UploadedFiles)
                    .HasForeignKey(f => f.UploadedById)
                    .OnDelete(DeleteBehavior.Restrict); // Не удалять файлы при удалении пользователя

                // Индексы для производительности
                entity.HasIndex(f => f.UploadedById);
                entity.HasIndex(f => f.Hash);
                entity.HasIndex(f => f.UploadedAt);
                entity.HasIndex(f => f.IsPublic);
                entity.HasIndex(f => f.ExpiresAt);
                entity.HasIndex(f => f.IsDeleted);
                entity.HasIndex(f => f.ContentType);

                // Составные индексы для частых запросов
                entity.HasIndex(f => new { f.IsDeleted, f.IsPublic, f.ExpiresAt });
                entity.HasIndex(f => new { f.UploadedById, f.UploadedAt });
                entity.HasIndex(f => new { f.ContentType, f.Size });

                // Значения по умолчанию
                entity.Property(f => f.DownloadCount)
                    .HasDefaultValue(0);

                entity.Property(f => f.IsDeleted)
                    .HasDefaultValue(false);

                entity.Property(f => f.IsPublic)
                    .HasDefaultValue(false);

                entity.Property(f => f.UploadedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // ========== КОНФИГУРАЦИЯ UploadSession ==========
            modelBuilder.Entity<UploadSession>(entity =>
            {
                entity.HasKey(s => s.UploadId);

                entity.Property(s => s.UploadId)
                    .HasMaxLength(36); // Guid as string

                entity.Property(s => s.FileName)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(s => s.ContentType)
                    .IsRequired()
                    .HasMaxLength(100);

                // Значения по умолчанию
                entity.Property(s => s.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(s => s.LastActivity)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(s => s.UploadedChunks)
                    .HasDefaultValue(0);

                // Связь с User
                entity.HasOne(s => s.User)
                    .WithMany(u => u.ChunkUploadSessions)
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Связь с FileMetadata (опционально)
                entity.HasOne(s => s.File)
                    .WithOne()
                    .HasForeignKey<UploadSession>(s => s.FileId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                // Индексы
                entity.HasIndex(s => s.UserId);
                entity.HasIndex(s => s.CreatedAt);
                entity.HasIndex(s => s.LastActivity);
                entity.HasIndex(s => s.FileId);

                // Составные индексы
                entity.HasIndex(s => new { s.UserId, s.CreatedAt });
                entity.HasIndex(s => new { s.LastActivity, s.FileId });

                // TTL индекс для автоматической очистки старых сессий
                // (можно использовать для фоновой очистки)
                entity.HasIndex(s => s.LastActivity);
            });

            // QuestionAttachment
            modelBuilder.Entity<QuestionAttachment>(entity =>
            {
                entity.HasKey(qa => new { qa.QuestionId, qa.FileId });

                entity.HasOne(qa => qa.Question)
                    .WithMany()
                    .HasForeignKey(qa => qa.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(qa => qa.File)
                    .WithMany()
                    .HasForeignKey(qa => qa.FileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // TopicAttachment
            modelBuilder.Entity<TopicAttachment>(entity =>
            {
                entity.HasKey(ta => new { ta.TopicId, ta.FileId });

                entity.HasOne(ta => ta.Topic)
                    .WithMany()
                    .HasForeignKey(ta => ta.TopicId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ta => ta.File)
                    .WithMany()
                    .HasForeignKey(ta => ta.FileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed данных для ролей и разрешений
            SeedData.SeedRolesAndPermissions(modelBuilder);
        }
    }
}