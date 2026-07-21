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
