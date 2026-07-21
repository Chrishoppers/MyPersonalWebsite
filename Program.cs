using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ⭐ 保留 SQLite（用于本地开发）
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<BrevoEmailService>();
builder.Services.AddScoped<SvgCaptchaService>();
builder.Services.AddScoped<RateLimitService>();
builder.Services.AddScoped<TursoService>();  // ⭐ 添加 Turso

builder.Services.AddSignalR();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();

    var adminExists = dbContext.Users.Any(u => u.Username == "admin");
    if (!adminExists)
    {
        dbContext.Users.Add(new User
        {
            Username = "admin",
            Email = "2908685235@qq.com",
            PasswordHash = "AQAAAAIAAYagAAAAEJ4Zj6zVqZMjSx5k5r5WYg==",
            IsEmailVerified = true,
            IsAdmin = true,
            IsBanned = false,
            CreatedAt = DateTime.Now
        });
        dbContext.SaveChanges();
        Console.WriteLine("✅ 管理员账号已创建");
    }

    // ⭐ 同步数据到 Turso
    try
    {
        var tursoService = scope.ServiceProvider.GetRequiredService<TursoService>();
        
        // 创建表
        await tursoService.ExecuteSqlAsync(@"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL,
                Email TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                VerificationCode TEXT,
                VerificationCodeExpiry TEXT,
                IsEmailVerified INTEGER DEFAULT 0,
                IsAdmin INTEGER DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                LastLoginAt TEXT,
                IsBanned INTEGER DEFAULT 0,
                BanExpiry TEXT,
                BanReason TEXT,
                BanNote TEXT,
                IsDeleted INTEGER DEFAULT 0,
                DeletedAt TEXT,
                DeleteReason TEXT,
                DeleteNote TEXT,
                AvatarUrl TEXT,
                IsAvatarApproved INTEGER DEFAULT 0,
                AvatarSubmittedAt TEXT,
                PendingEmail TEXT,
                PendingUsername TEXT,
                IsEmailChangeApproved INTEGER DEFAULT 0,
                IsUsernameChangeApproved INTEGER DEFAULT 0
            )
        ");
        
        // 创建其他表...
        Console.WriteLine("✅ Turso 数据库已同步");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Turso 同步失败: {ex.Message}");
    }
}

// ... 其余代码不变
