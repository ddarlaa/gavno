using IceBreakerApp.Domain;
using IceBreakerApp.Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // BaseEntity defaults
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.ClrType.IsSubclassOf(typeof(BaseEntity)))
                {
                    entityType.FindProperty(nameof(BaseEntity.CreatedAt))?.SetDefaultValueSql("CURRENT_TIMESTAMP");
                    entityType.FindProperty(nameof(BaseEntity.UpdatedAt))?.SetDefaultValueSql("CURRENT_TIMESTAMP");
                }
            }

            
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Title).IsRequired().HasMaxLength(500);
                entity.Property(q => q.Content).IsRequired();
                entity.Property(q => q.ViewCount).HasDefaultValue(0);
                entity.Property(q => q.LikeCount).HasDefaultValue(0);
                entity.Property(q => q.AnswerCount).HasDefaultValue(0);

                entity.HasOne(q => q.User)
                    .WithMany(u => u.Questions)
                    .HasForeignKey(q => q.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(q => q.Topic)
                    .WithMany()
                    .HasForeignKey(q => q.TopicId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(q => q.UserId);
                entity.HasIndex(q => q.TopicId);
                entity.HasIndex(q => q.IsActive);
                entity.HasIndex(q => q.CreatedAt);
            });

            
            modelBuilder.Entity<QuestionAnswer>(entity =>
            {
                entity.HasKey(qa => qa.Id);
                entity.Property(qa => qa.Content).IsRequired();
                entity.Property(qa => qa.ViewCount).HasDefaultValue(0);
                entity.Property(qa => qa.IsAccepted).HasDefaultValue(false);

                entity.HasOne(qa => qa.Question)
                    .WithMany()
                    .HasForeignKey(qa => qa.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(qa => qa.User)
                    .WithMany(qa => qa.Answers)
                    .HasForeignKey(qa => qa.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(qa => qa.QuestionId);
                entity.HasIndex(qa => qa.UserId);
                entity.HasIndex(qa => qa.IsActive);
                entity.HasIndex(qa => qa.IsAccepted);
                entity.HasIndex(qa => qa.CreatedAt);
            });

            
            modelBuilder.Entity<QuestionLike>(entity =>
            {
                entity.HasKey(ql => ql.Id);

                entity.HasOne(ql => ql.Question)
                    .WithMany()
                    .HasForeignKey(ql => ql.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);

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

                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.IsActive);
                entity.HasIndex(u => u.IsDeleted);
            });

            // UserSession
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(us => us.Id);
                entity.Property(us => us.RefreshTokenHash).IsRequired().HasMaxLength(512);
                entity.Property(us => us.DeviceInfo).HasMaxLength(500);
                entity.Property(us => us.IpAddress).HasMaxLength(45);

                entity.HasOne<User>()
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

                entity.HasIndex(r => r.Name).IsUnique();
            });

            // Permission
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Description).HasMaxLength(500);
                entity.Property(p => p.Category).HasMaxLength(50);

                entity.HasIndex(p => p.Name).IsUnique();
                entity.HasIndex(p => p.Category);
            });

            // UserRole (составной ключ)
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                entity.HasOne<User>()
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Role>()
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

                entity.HasOne<Role>()
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Permission>()
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

                entity.HasOne<User>()
                    .WithMany(u => u.UserClaims)
                    .HasForeignKey(uc => uc.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(uc => uc.UserId);
                entity.HasIndex(uc => uc.ClaimType);
                entity.HasIndex(uc => new { uc.ClaimType, uc.ClaimValue });
            });

            // Seed данных для ролей и разрешений
            SeedData.SeedRolesAndPermissions(modelBuilder);
        }
    }
}
