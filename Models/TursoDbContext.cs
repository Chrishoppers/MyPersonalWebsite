using Microsoft.EntityFrameworkCore;
using System;

namespace MyPersonalWebsite.Models
{
    public class TursoDbContext : DbContext
    {
        public TursoDbContext(DbContextOptions<TursoDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ContactRequest> ContactRequests { get; set; }
        public DbSet<AboutMe> AboutMeContents { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }
        public DbSet<BlogLike> BlogLikes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlogLike>()
                .HasIndex(l => new { l.BlogId, l.UserId })
                .IsUnique();

            // Turso 中如果管理员不存在，自动创建
            // 但不使用 HasData，避免重复插入
        }
    }
}
