using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Hubs;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ⭐ 禁用文件监控
builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = false;
    options.ValidateOnBuild = false;
});

Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");

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
builder.Services.AddControllersWithViews();
// ============================================================
// DataProtection 使用文件存储（每次部署不会丢失 Session）
// ============================================================
var keysDirectory = "/app/keys";
if (!Directory.Exists(keysDirectory))
{
    try { Directory.CreateDirectory(keysDirectory); } catch { }
}

try
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
        .SetApplicationName("MyPersonalWebsite")
        .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
    Console.WriteLine("✅ DataProtection 已配置持久化密钥");
}
catch
{
    builder.Services.AddDataProtection().SetApplicationName("MyPersonalWebsite");
}

// ============================================================
// Session
// ============================================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ============================================================
// HttpClient
// ============================================================
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// ============================================================
// ⭐ 后台服务
// ============================================================
builder.Services.AddHostedService<StreakEmailService>();
builder.Services.AddHostedService<DailyQuestionScheduler>();

// ============================================================
// ⭐ Scoped Services
// ============================================================
builder.Services.AddScoped<BrevoEmailService>();
builder.Services.AddScoped<SvgCaptchaService>();
builder.Services.AddScoped<RateLimitService>();
builder.Services.AddScoped<DataSyncService>();
builder.Services.AddScoped<TursoService>();
builder.Services.AddScoped<DailyQuestionService>();
builder.Services.AddScoped<GameSuggestionService>();
builder.Services.AddScoped<TrainService>();
builder.Services.AddScoped<ReCaptchaService>();
builder.Services.AddScoped<EmailRateLimitService>();
builder.Services.AddScoped<GameAntiCheatService>();
builder.Services.AddScoped<DeepSeekService>();
builder.Services.AddScoped<WerewolfVoiceService>();

// ⭐ 平台验证服务（新增）
builder.Services.AddScoped<PlatformVerifyService>();

// ⭐ HttpClient 配置
builder.Services.AddHttpClient<DeepSeekService>();
builder.Services.AddHttpClient<TrainService>();
builder.Services.AddHttpClient<ReCaptchaService>();

// ============================================================
// OCR / Solver services
// ============================================================
// Cloud OCR HTTP client
builder.Services.AddHttpClient<CloudOcrService>();
// Default IOcrService -> CloudOcrService (will fallback to Tesseract if needed)
builder.Services.AddScoped<IOcrService, CloudOcrService>();
// Register Tesseract implementation (can be used directly if desired)
builder.Services.AddScoped<TesseractOcrService>();
// Solver processing
builder.Services.AddScoped<SolverProcessingService>();

// ⭐ PlatformVerifyService 的 HttpClient（带超时和 User-Agent）
builder.Services.AddHttpClient<PlatformVerifyService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
});

// ============================================================
// ⭐ SignalR（核心）
// ============================================================
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400;
});

// ============================================================
// 构建应用
// ============================================================
var app = builder.Build();

// ============================================================
// 初始化数据库
// ============================================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dataSync = scope.ServiceProvider.GetRequiredService<DataSyncService>();

        db.Database.EnsureCreated();
        Console.WriteLine("✅ 本地 SQLite 缓存已就绪");

        await EnsureTursoTablesAsync(dataSync);
        await dataSync.EnsureAdminExistsAsync();
        await EnsureAboutMeDataAsync(dataSync);
        await SeedDailyQuestionBankAsync(dataSync);
        Console.WriteLine("✅ 每日一问题库已就绪");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ 数据库初始化失败: {ex.Message}");
    }
}

// ============================================================
// 中间件管道
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

// ============================================================
// ⭐ 路由映射（Controller + Hub）
// ============================================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// ⭐ SignalR Hub 映射
app.MapHub<MessageHub>("/messageHub");
app.MapHub<PartyHub>("/partyHub");
app.MapHub<WerewolfHub>("/werewolfHub");
// SolverHub for real-time solver progress
app.MapHub<SolverHub>("/solverHub");

// ============================================================
// 应用生命周期事件
// ============================================================
app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine("✅ 应用已启动！");
});
app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("⏹️ 应用正在停止...");
});

// ============================================================
// 运行应用
// ============================================================
app.Run();

// ============================================================
// ⭐ 辅助方法
// ============================================================
async Task EnsureTursoTablesAsync(DataSyncService dataSync)
{
    Console.WriteLine("📦 检查 Turso 数据表...");
    var tables = new Dictionary<string, string>
    {
        { "GameSuggestions", @"CREATE TABLE IF NOT EXISTS GameSuggestions (Id INTEGER PRIMARY KEY, UserId INTEGER NOT NULL, GameName TEXT NOT NULL, Description TEXT, Votes INTEGER DEFAULT 0)" },
        { "GameSuggestionVotes", @"CREATE TABLE IF NOT EXISTS GameSuggestionVotes (Id INTEGER PRIMARY KEY, SuggestionId INTEGER NOT NULL, UserId INTEGER NOT NULL, VotedAt TEXT, UNIQUE(SuggestionId, UserId))" },
        { "Users", @"CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY, Username TEXT NOT NULL, Email TEXT NOT NULL, PasswordHash TEXT NOT NULL, IsEmailVerified INTEGER DEFAULT 0, IsAdmin INTEGER DEFAULT 0)" },
        { "Blogs", @"CREATE TABLE IF NOT EXISTS Blogs (Id INTEGER PRIMARY KEY, Title TEXT NOT NULL, Content TEXT NOT NULL, Summary TEXT, PublishDate TEXT, CoverImageUrl TEXT, LikeCount INTEGER DEFAULT 0)" },
        { "Messages", @"CREATE TABLE IF NOT EXISTS Messages (Id INTEGER PRIMARY KEY, UserId INTEGER, VisitorName TEXT, Email TEXT, Content TEXT, CreateTime TEXT, IsApproved INTEGER DEFAULT 0)" },
        { "Projects", @"CREATE TABLE IF NOT EXISTS Projects (Id INTEGER PRIMARY KEY, Name TEXT, Description TEXT, ImageUrl TEXT, ProjectUrl TEXT, TechStack TEXT)" },
        { "ContactRequests", @"CREATE TABLE IF NOT EXISTS ContactRequests (Id INTEGER PRIMARY KEY, Platform TEXT, AuthorizationCode TEXT, HowKnowMe TEXT, Identity TEXT, Relationship TEXT, Remarks TEXT)" },
        { "AboutMeContents", @"CREATE TABLE IF NOT EXISTS AboutMeContents (Id INTEGER PRIMARY KEY, SectionKey TEXT, Title TEXT, Content TEXT, Icon TEXT, SortOrder INTEGER DEFAULT 0, UpdatedAt TEXT)" },
        { "PasswordResets", @"CREATE TABLE IF NOT EXISTS PasswordResets (Id INTEGER PRIMARY KEY, UserId INTEGER, Token TEXT, Email TEXT, CreatedAt TEXT, ExpiresAt TEXT, IsUsed INTEGER DEFAULT 0)" },
        { "BlogLikes", @"CREATE TABLE IF NOT EXISTS BlogLikes (Id INTEGER PRIMARY KEY, BlogId INTEGER, UserId INTEGER, CreateTime TEXT)" },
        { "MessageLikes", @"CREATE TABLE IF NOT EXISTS MessageLikes (Id INTEGER PRIMARY KEY, MessageId INTEGER NOT NULL, UserId INTEGER NOT NULL, CreateTime TEXT)" },
        { "EmailLogs", @"CREATE TABLE IF NOT EXISTS EmailLogs (Id INTEGER PRIMARY KEY, UserId INTEGER, Email TEXT, Type TEXT, SentAt TEXT, IsSuccess INTEGER DEFAULT 0, ErrorMessage TEXT)" },
        { "Notifications", @"CREATE TABLE IF NOT EXISTS Notifications (Id INTEGER PRIMARY KEY, UserId INTEGER NOT NULL, Title TEXT NOT NULL, Message TEXT NOT NULL, Type TEXT DEFAULT 'info', IsRead INTEGER DEFAULT 0, CreatedAt TEXT)" },
        { "DailyQuestionBank", @"CREATE TABLE IF NOT EXISTS DailyQuestionBank (Id INTEGER PRIMARY KEY, Question TEXT NOT NULL, Answer TEXT NOT NULL, Pinyin TEXT, Hint TEXT, Difficulty INTEGER DEFAULT 1)" },
        { "DailyQuestions", @"CREATE TABLE IF NOT EXISTS DailyQuestions (Id INTEGER PRIMARY KEY, QuestionId INTEGER NOT NULL, Date TEXT UNIQUE NOT NULL, CreatedAt TEXT)" },
        { "UserDailyAnswers", @"CREATE TABLE IF NOT EXISTS UserDailyAnswers (Id INTEGER PRIMARY KEY, UserId INTEGER NOT NULL, QuestionId INTEGER NOT NULL, Answer TEXT, IsCorrect INTEGER DEFAULT 0, AnsweredAt TEXT)" },
        { "UserGameStats", @"CREATE TABLE IF NOT EXISTS UserGameStats (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL UNIQUE, TotalPoints INTEGER DEFAULT 0, MaxCombo INTEGER DEFAULT 0)" },
        { "GameSessions", @"CREATE TABLE IF NOT EXISTS GameSessions (Id INTEGER PRIMARY KEY, UserId INTEGER NOT NULL, SessionId TEXT NOT NULL UNIQUE, StartTime TEXT NOT NULL, EndTime TEXT, TotalScore INTEGER DEFAULT 0)" },
        { "GameAnswerLogs", @"CREATE TABLE IF NOT EXISTS GameAnswerLogs (Id INTEGER PRIMARY KEY, SessionId TEXT NOT NULL, UserId INTEGER NOT NULL, Level INTEGER NOT NULL, QuestionType TEXT, Answer TEXT, IsCorrect INTEGER DEFAULT 0, CreatedAt TEXT)" },
        { "CheatEvents", @"CREATE TABLE IF NOT EXISTS CheatEvents (Id INTEGER PRIMARY KEY, SessionId TEXT NOT NULL, UserId INTEGER NOT NULL, EventType TEXT NOT NULL, EventDetail TEXT, DetectedAt TEXT)" },
        { "ResourceRequests", @"CREATE TABLE IF NOT EXISTS ResourceRequests (Id INTEGER PRIMARY KEY, UserId INTEGER NOT NULL, UserName TEXT NOT NULL, UserEmail TEXT NOT NULL, PersonName TEXT NOT NULL, CharacterName TEXT, ResourceType TEXT DEFAULT '一人', Status TEXT DEFAULT 'pending', CreatedAt TEXT)" },
        { "VerifyGameStats", @"CREATE TABLE IF NOT EXISTS VerifyGameStats (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL UNIQUE, TotalScore INTEGER DEFAULT 0, MaxCombo INTEGER DEFAULT 0, MaxLevel INTEGER DEFAULT 0, GamesPlayed INTEGER DEFAULT 0, UpdatedAt TEXT)" }
    };

    int successCount = 0;
    foreach (var table in tables)
    {
        try
        {
            var checkResult = await dataSync.QueryAsync($"SELECT name FROM sqlite_master WHERE type='table' AND name='{table.Key}'");
            if (checkResult.Contains($"\"{table.Key}\"")) { successCount++; continue; }
            var result = await dataSync.ExecuteSqlAsync(table.Value);
            if (result) successCount++;
        }
        catch { }
    }
    Console.WriteLine($"📊 Turso 表检查完成: 成功 {successCount}");
}

async Task EnsureAboutMeDataAsync(DataSyncService dataSync)
{
    try
    {
        var sections = await dataSync.GetAboutMeAsync();
        if (sections == null || !sections.Any())
        {
            var defaultSections = new[]
            {
                new AboutMe { Id = 1, SectionKey = "bio", Title = "🧑‍💻 关于我", Content = "你好！我是 Chris hopper，一个热爱技术的全栈开发者。\n目前专注于 ASP.NE[...]" },
                new AboutMe { Id = 2, SectionKey = "journey", Title = "🚀 学习之路", Content = "从高中开始接触编程，在技术的道路上不断探索和成长。\n我相信持续[...]" },
                new AboutMe { Id = 3, SectionKey = "goal", Title = "🎯 愿景", Content = "用技术解决问题，创造有价值的工具和内容。\n希望我的作品能对他人有所帮[...]" },
                new AboutMe { Id = 4, SectionKey = "social", Title = "🔗 社交链接", Content = "github:https://github.com|twitter:https://twitter.com|linkedin:https://linkedin.com", Icon = "[...]" },
            };
            foreach (var section in defaultSections) { await dataSync.AddAboutMeAsync(section); }
            Console.WriteLine("✅ AboutMe 默认数据已插入 Turso");
        }
    }
    catch (Exception ex) { Console.WriteLine($"⚠️ AboutMe 数据检查失败: {ex.Message}"); }
}

async Task SeedDailyQuestionBankAsync(DataSyncService dataSync)
{
    var checkResult = await dataSync.QueryAsync("SELECT COUNT(*) as Count FROM DailyQuestionBank");
    if (!checkResult.Contains("\"rows\":[{\"value\":0}]") && !checkResult.Contains("\"rows\":[]"))
    {
        Console.WriteLine("✅ 题库已存在，跳过初始化");
        return;
    }
    Console.WriteLine("📦 题库为空，将由 DailyQuestionService 自动填充");
}

string EscapeSql(string value)
{
    if (string.IsNullOrEmpty(value)) return "";
    return value.Replace("'", "''");
}
