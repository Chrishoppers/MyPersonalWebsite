using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Hubs;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 设置时区为中国时区（北京时间 UTC+8）
// ============================================================
var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("zh-CN");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("zh-CN");
Console.WriteLine($"✅ 时区已设置为: {TimeZoneInfo.Local.DisplayName}");

// ============================================================
// 添加 MVC 服务 + JSON 中文不乱码配置
// ============================================================
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        options.JsonSerializerOptions.WriteIndented = true;
    });

// ============================================================
// 本地 SQLite（仅作为缓存/备用，不用于主数据）
// ============================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ============================================================
// DataProtection 使用文件存储（每次部署不会丢失 Session）
// ============================================================
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("MyPersonalWebsite")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(30));

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
    var dataSync = scope.ServiceProvider.GetRequiredService<DataSyncService>();

    db.Database.EnsureCreated();
    Console.WriteLine("✅ 本地 SQLite 缓存已就绪");

    // ⭐ 创建所有 Turso 表（先检查再创建）
    await EnsureTursoTablesAsync(dataSync);

    await dataSync.EnsureAdminExistsAsync();
    await EnsureAboutMeDataAsync(dataSync);
}

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
// ⭐ 确保所有 Turso 表存在（先检查再创建）
// ============================================================
async Task EnsureTursoTablesAsync(DataSyncService dataSync)
{
    Console.WriteLine("📦 检查 Turso 数据表...");

    var tables = new Dictionary<string, string>
    {
        { "Users", @"
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
                VerificationCodeExpiry TEXT,
                IsApproved INTEGER DEFAULT 0
            )"
        },
        { "Blogs", @"
            CREATE TABLE IF NOT EXISTS Blogs (
                Id INTEGER PRIMARY KEY,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL,
                Summary TEXT,
                PublishDate TEXT,
                CoverImageUrl TEXT,
                LikeCount INTEGER DEFAULT 0
            )"
        },
        { "Messages", @"
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
            )"
        },
        { "Projects", @"
            CREATE TABLE IF NOT EXISTS Projects (
                Id INTEGER PRIMARY KEY,
                Name TEXT,
                Description TEXT,
                ImageUrl TEXT,
                ProjectUrl TEXT,
                TechStack TEXT
            )"
        },
        { "ContactRequests", @"
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
            )"
        },
        { "AboutMeContents", @"
            CREATE TABLE IF NOT EXISTS AboutMeContents (
                Id INTEGER PRIMARY KEY,
                SectionKey TEXT,
                Title TEXT,
                Content TEXT,
                Icon TEXT,
                SortOrder INTEGER DEFAULT 0,
                UpdatedAt TEXT
            )"
        },
        { "PasswordResets", @"
            CREATE TABLE IF NOT EXISTS PasswordResets (
                Id INTEGER PRIMARY KEY,
                UserId INTEGER,
                Token TEXT,
                Email TEXT,
                CreatedAt TEXT,
                ExpiresAt TEXT,
                IsUsed INTEGER DEFAULT 0
            )"
        },
        { "BlogLikes", @"
            CREATE TABLE IF NOT EXISTS BlogLikes (
                Id INTEGER PRIMARY KEY,
                BlogId INTEGER,
                UserId INTEGER,
                CreateTime TEXT
            )"
        },
        { "EmailLogs", @"
            CREATE TABLE IF NOT EXISTS EmailLogs (
                Id INTEGER PRIMARY KEY,
                UserId INTEGER,
                Email TEXT,
                Type TEXT,
                SentAt TEXT,
                IsSuccess INTEGER DEFAULT 0,
                ErrorMessage TEXT
            )"
        },
        { "Notifications", @"
            CREATE TABLE IF NOT EXISTS Notifications (
                Id INTEGER PRIMARY KEY,
                UserId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Message TEXT NOT NULL,
                Type TEXT DEFAULT 'info',
                IsRead INTEGER DEFAULT 0,
                CreatedAt TEXT
            )"
        }
    };

    int successCount = 0;
    int failCount = 0;

    foreach (var table in tables)
    {
        try
        {
            // ⭐ 先检查表是否存在
            var checkResult = await dataSync.QueryAsync($"SELECT name FROM sqlite_master WHERE type='table' AND name='{table.Key}'");
            if (checkResult.Contains($"\"{table.Key}\""))
            {
                Console.WriteLine($"✅ 表 {table.Key} 已存在，跳过创建");
                successCount++;
                continue;
            }

            // 表不存在，创建
            var result = await dataSync.ExecuteSqlAsync(table.Value);
            if (result)
            {
                successCount++;
                Console.WriteLine($"✅ 表 {table.Key} 创建成功");
            }
            else
            {
                failCount++;
                Console.WriteLine($"⚠️ 表 {table.Key} 创建失败");
            }
        }
        catch (Exception ex)
        {
            failCount++;
            Console.WriteLine($"⚠️ 表 {table.Key} 创建异常: {ex.Message}");
        }
    }

    Console.WriteLine($"📊 Turso 表检查完成: 成功 {successCount}, 失败 {failCount}");
}

// ============================================================
// ⭐ EnsureAboutMeDataAsync 方法
// ============================================================
async Task EnsureAboutMeDataAsync(DataSyncService dataSync)
{
    Console.WriteLine("📦 检查 AboutMe 数据...");

    try
    {
        var sections = await dataSync.GetAboutMeAsync();

        if (sections == null || !sections.Any())
        {
            Console.WriteLine("📝 AboutMe 数据为空，正在插入默认数据...");

            var defaultSections = new[]
            {
                new AboutMe
                {
                    Id = 1,
                    SectionKey = "bio",
                    Title = "🧑‍💻 关于我",
                    Content = "你好！我是 Chris hopper，一个热爱技术的全栈开发者。\n目前专注于 ASP.NET Core 和现代 Web 开发。",
                    Icon = "🧑‍💻",
                    SortOrder = 1,
                    UpdatedAt = DateTime.Now
                },
                new AboutMe
                {
                    Id = 2,
                    SectionKey = "journey",
                    Title = "🚀 学习之路",
                    Content = "从高中开始接触编程，在技术的道路上不断探索和成长。\n我相信持续学习是保持竞争力的关键。",
                    Icon = "🚀",
                    SortOrder = 2,
                    UpdatedAt = DateTime.Now
                },
                new AboutMe
                {
                    Id = 3,
                    SectionKey = "goal",
                    Title = "🎯 愿景",
                    Content = "用技术解决问题，创造有价值的工具和内容。\n希望我的作品能对他人有所帮助。",
                    Icon = "🎯",
                    SortOrder = 3,
                    UpdatedAt = DateTime.Now
                },
                new AboutMe
                {
                    Id = 4,
                    SectionKey = "social",
                    Title = "🔗 社交链接",
                    Content = "github:https://github.com|twitter:https://twitter.com|linkedin:https://linkedin.com",
                    Icon = "🔗",
                    SortOrder = 4,
                    UpdatedAt = DateTime.Now
                }
            };

            foreach (var section in defaultSections)
            {
                await dataSync.AddAboutMeAsync(section);
            }

            Console.WriteLine("✅ AboutMe 默认数据已插入 Turso");
        }
        else
        {
            Console.WriteLine($"✅ AboutMe 数据已存在 ({sections.Count} 条)");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ AboutMe 数据检查失败: {ex.Message}");
    }
}
