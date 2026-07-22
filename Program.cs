using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ============================================================
// 本地 SQLite（主数据库）
// ============================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ============================================================
// Session
// ============================================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ============================================================
// 服务注册
// ============================================================
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<BrevoEmailService>();
builder.Services.AddScoped<SvgCaptchaService>();
builder.Services.AddScoped<RateLimitService>();
builder.Services.AddScoped<DataSyncService>();
builder.Services.AddScoped<TursoService>();

builder.Services.AddSignalR();

var app = builder.Build();

// ============================================================
// 初始化数据库
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var tursoService = scope.ServiceProvider.GetRequiredService<TursoService>();
    var dataSync = scope.ServiceProvider.GetRequiredService<DataSyncService>();

    // 1. 创建本地 SQLite
    db.Database.EnsureCreated();
    Console.WriteLine("✅ 本地 SQLite 数据库已就绪");

    // 2. 创建 Turso 表（自动）
    await EnsureTursoTablesAsync(tursoService);

    // 3. 确保管理员账号存在
    await dataSync.EnsureAdminExistsAsync();
}

// ============================================================
// 中间件
// ============================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<MessageHub>("/messageHub");

app.Run();

// ============================================================
// Turso 自动建表方法
// ============================================================
async Task EnsureTursoTablesAsync(TursoService tursoService)
{
    Console.WriteLine("📦 检查 Turso 数据表...");

    var tables = new[]
    {
        // ===== 用户表 =====
        @"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY,
            Username TEXT NOT NULL,
            Email TEXT NOT NULL,
            PasswordHash TEXT NOT NULL,
            IsEmailVerified INTEGER DEFAULT 0,
            IsAdmin INTEGER DEFAULT 0,
            CreatedAt TEXT,
            LastLoginAt TEXT,
            IsBanned INTEGER DEFAULT 0,
            BanExpiry TEXT,
            BanReason TEXT,
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
            IsUsernameChangeApproved INTEGER DEFAULT 0,
            VerificationCode TEXT,
            VerificationCodeExpiry TEXT
        )",

        // ===== 博客表 =====
        @"
        CREATE TABLE IF NOT EXISTS Blogs (
            Id INTEGER PRIMARY KEY,
            Title TEXT NOT NULL,
            Content TEXT NOT NULL,
            Summary TEXT,
            PublishDate TEXT,
            CoverImageUrl TEXT,
            LikeCount INTEGER DEFAULT 0
        )",

        // ===== 留言表 =====
        @"
        CREATE TABLE IF NOT EXISTS Messages (
            Id INTEGER PRIMARY KEY,
            UserId INTEGER,
            VisitorName TEXT,
            Email TEXT,
            Content TEXT,
            CreateTime TEXT,
            IsApproved INTEGER DEFAULT 0,
            LikeCount INTEGER DEFAULT 0,
            AdminReply TEXT,
            AdminReplyTime TEXT,
            ReportCount INTEGER DEFAULT 0,
            IsReported INTEGER DEFAULT 0
        )",

        // ===== 项目表 =====
        @"
        CREATE TABLE IF NOT EXISTS Projects (
            Id INTEGER PRIMARY KEY,
            Name TEXT,
            Description TEXT,
            ImageUrl TEXT,
            ProjectUrl TEXT,
            TechStack TEXT
        )",

        // ===== 授权码申请表 =====
        @"
        CREATE TABLE IF NOT EXISTS ContactRequests (
            Id INTEGER PRIMARY KEY,
            Platform TEXT,
            AuthorizationCode TEXT,
            HowKnowMe TEXT,
            Identity TEXT,
            Relationship TEXT,
            Remarks TEXT,
            UserId INTEGER,
            Username TEXT,
            UserEmail TEXT,
            RequestTime TEXT,
            IsApproved INTEGER DEFAULT 0,
            ViewTime TEXT,
            IsUsed INTEGER DEFAULT 0,
            UsedTime TEXT,
            UsedBy TEXT
        )",

        // ===== 关于我表 =====
        @"
        CREATE TABLE IF NOT EXISTS AboutMeContents (
            Id INTEGER PRIMARY KEY,
            SectionKey TEXT,
            Title TEXT,
            Content TEXT,
            Icon TEXT,
            SortOrder INTEGER DEFAULT 0,
            UpdatedAt TEXT
        )",

        // ===== 密码重置表 =====
        @"
        CREATE TABLE IF NOT EXISTS PasswordResets (
            Id INTEGER PRIMARY KEY,
            UserId INTEGER,
            Token TEXT,
            Email TEXT,
            CreatedAt TEXT,
            ExpiresAt TEXT,
            IsUsed INTEGER DEFAULT 0
        )",

        // ===== 博客点赞表 =====
        @"
        CREATE TABLE IF NOT EXISTS BlogLikes (
            Id INTEGER PRIMARY KEY,
            BlogId INTEGER,
            UserId INTEGER,
            CreateTime TEXT
        )",

        // ===== 邮件日志表 =====
        @"
        CREATE TABLE IF NOT EXISTS EmailLogs (
            Id INTEGER PRIMARY KEY,
            UserId INTEGER,
            Email TEXT,
            Type TEXT,
            SentAt TEXT,
            IsSuccess INTEGER DEFAULT 0,
            ErrorMessage TEXT
        )"
    };

    int successCount = 0;
    int failCount = 0;

    foreach (var sql in tables)
    {
        try
        {
            var result = await tursoService.ExecuteSqlAsync(sql);
            if (result)
            {
                successCount++;
                Console.WriteLine($"✅ Turso 表创建成功");
            }
            else
            {
                failCount++;
                Console.WriteLine($"⚠️ Turso 表创建失败（可能已存在）");
            }
        }
        catch (Exception ex)
        {
            failCount++;
            Console.WriteLine($"⚠️ Turso 表创建异常: {ex.Message}");
        }
    }

    Console.WriteLine($"📊 Turso 表检查完成: 成功 {successCount}, 失败 {failCount}");

    // ===== 检查是否有 AboutMe 数据，没有则插入默认数据 =====
    try
    {
        var checkResult = await tursoService.QueryAsync("SELECT COUNT(*) FROM AboutMeContents");
        if (checkResult.Contains("\"rows\":[]") || checkResult.Contains("\"count\":0"))
        {
            Console.WriteLine("📝 Turso 中无 AboutMe 数据，插入默认数据...");
            
            var defaultAboutMe = new[]
            {
                @"INSERT OR IGNORE INTO AboutMeContents (Id, SectionKey, Title, Content, Icon, SortOrder, UpdatedAt)
                  VALUES (1, 'bio', '🧑‍💻 关于我', '你好！我是 Chris Hopper，一个热爱技术的全栈开发者。\n目前专注于 ASP.NET Core 和现代 Web 开发。', '🧑‍💻', 1, datetime('now'))",
                @"INSERT OR IGNORE INTO AboutMeContents (Id, SectionKey, Title, Content, Icon, SortOrder, UpdatedAt)
                  VALUES (2, 'journey', '🚀 学习之路', '从高中开始接触编程，在技术的道路上不断探索和成长。\n我相信持续学习是保持竞争力的关键。', '🚀', 2, datetime('now'))",
                @"INSERT OR IGNORE INTO AboutMeContents (Id, SectionKey, Title, Content, Icon, SortOrder, UpdatedAt)
                  VALUES (3, 'goal', '🎯 愿景', '用技术解决问题，创造有价值的工具和内容。\n希望我的作品能对他人有所帮助。', '🎯', 3, datetime('now'))",
                @"INSERT OR IGNORE INTO AboutMeContents (Id, SectionKey, Title, Content, Icon, SortOrder, UpdatedAt)
                  VALUES (4, 'social', '🔗 社交链接', 'github:https://github.com|twitter:https://twitter.com|linkedin:https://linkedin.com', '🔗', 4, datetime('now'))"
            };

            foreach (var sql in defaultAboutMe)
            {
                await tursoService.ExecuteSqlAsync(sql);
            }
            Console.WriteLine("✅ AboutMe 默认数据已插入 Turso");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ AboutMe 数据检查失败: {ex.Message}");
    }
}
