using Microsoft.EntityFrameworkCore;
using System;

namespace MyPersonalWebsite.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ContactRequest> ContactRequests { get; set; }
        public DbSet<BlogLike> BlogLikes { get; set; }
        public DbSet<MessageLike> MessageLikes { get; set; }
        public DbSet<ReportRecord> ReportRecords { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ===== 关系配置 =====
            // 博客点赞关系
            modelBuilder.Entity<BlogLike>()
                .HasOne(l => l.Blog)
                .WithMany(b => b.Likes)
                .HasForeignKey(l => l.BlogId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BlogLike>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 唯一约束：一个用户对一篇博客只能点一次赞
            modelBuilder.Entity<BlogLike>()
                .HasIndex(l => new { l.BlogId, l.UserId })
                .IsUnique();
            modelBuilder.Entity<Message>()
                .HasOne(m => m.User)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessageLike>()
                .HasOne(l => l.Message)
                .WithMany(m => m.Likes)
                .HasForeignKey(l => l.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessageLike>()
                .HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReportRecord>()
                .HasOne(r => r.Message)
                .WithMany()
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReportRecord>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== 唯一约束 =====
            modelBuilder.Entity<MessageLike>()
                .HasIndex(l => new { l.MessageId, l.UserId })
                .IsUnique();

            modelBuilder.Entity<ReportRecord>()
                .HasIndex(r => new { r.MessageId, r.UserId })
                .IsUnique();

            // ===== 种子数据：管理员 =====
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "2908685235@qq.com",
                    PasswordHash = "AQAAAAIAAYagAAAAEJ4Zj6zVqZMjSx5k5r5WYg==",
                    IsEmailVerified = true,
                    IsAdmin = true,
                    IsBanned = false,
                    CreatedAt = new DateTime(2026, 7, 19, 0, 0, 0)
                }
            );

            // ===== 种子数据：博客 =====
            modelBuilder.Entity<Blog>().HasData(
                new Blog
                {
                    Id = 1,
                    Title = "欢迎来到 Chris Hopper 的技术空间",
                    Content = "<p>这是我的第一篇博客，欢迎大家！</p>",
                    Summary = "开篇之作",
                    PublishDate = new DateTime(2026, 7, 19, 10, 0, 0)
                }
            );

            // ===== 种子数据：作品 =====
            modelBuilder.Entity<Project>().HasData(
                new Project
                {
                    Id = 1,
                    Name = "个人网站项目",
                    Description = "使用 ASP.NET Core 10.0 构建",
                    ImageUrl = "/images/project1.jpg",
                    ProjectUrl = "#",
                    TechStack = "ASP.NET Core 10.0, SQLite, Bootstrap 5"
                }
            );
        }
    }
}