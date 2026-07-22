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
        public DbSet<User> Users { get; set; }
        public DbSet<AboutMe> AboutMeContents { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }
        public DbSet<BlogLike> BlogLikes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlogLike>()
                .HasIndex(l => new { l.BlogId, l.UserId })
                .IsUnique();

            // ============================================================
            // 管理员种子数据
            // ============================================================
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
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 19, 0, 0, 0)
                }
            );

            // ============================================================
            // 示例博客数据
            // ============================================================
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

            // ============================================================
            // 示例项目数据
            // ============================================================
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

            // ============================================================
            // AboutMe 种子数据
            // ============================================================
            modelBuilder.Entity<AboutMe>().HasData(
                new AboutMe 
                { 
                    Id = 1, 
                    SectionKey = "bio", 
                    Title = "🧑‍💻 关于我", 
                    Content = "你好！我是 Chris Hopper，一个热爱技术的全栈开发者。\n目前专注于 ASP.NET Core 和现代 Web 开发。", 
                    Icon = "🧑‍💻", 
                    SortOrder = 1, 
                    UpdatedAt = new DateTime(2026, 7, 19, 0, 0, 0) 
                },
                new AboutMe 
                { 
                    Id = 2, 
                    SectionKey = "journey", 
                    Title = "🚀 学习之路", 
                    Content = "从高中开始接触编程，在技术的道路上不断探索和成长。\n我相信持续学习是保持竞争力的关键。", 
                    Icon = "🚀", 
                    SortOrder = 2, 
                    UpdatedAt = new DateTime(2026, 7, 19, 0, 0, 0) 
                },
                new AboutMe 
                { 
                    Id = 3, 
                    SectionKey = "goal", 
                    Title = "🎯 愿景", 
                    Content = "用技术解决问题，创造有价值的工具和内容。\n希望我的作品能对他人有所帮助。", 
                    Icon = "🎯", 
                    SortOrder = 3, 
                    UpdatedAt = new DateTime(2026, 7, 19, 0, 0, 0) 
                },
                new AboutMe 
                { 
                    Id = 4, 
                    SectionKey = "social", 
                    Title = "🔗 社交链接", 
                    Content = "github:https://github.com|twitter:https://twitter.com|linkedin:https://linkedin.com", 
                    Icon = "🔗", 
                    SortOrder = 4, 
                    UpdatedAt = new DateTime(2026, 7, 19, 0, 0, 0) 
                }
            );
        }
    }
}
