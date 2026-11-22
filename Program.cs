using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Hubs;
using MyPersonalWebsite.Services.BoardGame;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// ⭐ 只保留控制台日志：禁用 EventLog 提供程序
//    （否则非管理员环境写事件日志 source ".NET Runtime" 会因"拒绝访问"而崩溃，并掩盖真实错误）
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddSimpleConsole();

// 全局兜底：捕获未处理异常，避免 .NET 运行时再尝试写事件日志
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
    Console.WriteLine($"[FATAL] 未处理异常: {e.ExceptionObject}");
TaskScheduler.UnobservedTaskException += (s, e) =>
{
    Console.WriteLine($"[WARN] 未观察任务异常: {e.Exception?.Message}");
    e.SetObserved();
};

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
builder.Services.AddScoped<GomokuEngine>();
builder.Services.AddScoped<XiangqiEngine>();
builder.Services.AddScoped<ChessEngine>();
builder.Services.AddScoped<BoardGameService>();
builder.Services.AddScoped<BoardGameAiService>();
builder.Services.AddScoped<ChatModerationService>();
builder.Services.AddScoped<QrCodeService>();
builder.Services.AddScoped<CasualGameService>();

// ⭐ 平台验证服务（新增）
builder.Services.AddScoped<PlatformVerifyService>();

// ⭐ HttpClient 配置
builder.Services.AddHttpClient<DeepSeekService>();
builder.Services.AddHttpClient<TrainService>();
builder.Services.AddHttpClient<ReCaptchaService>();

builder.Services.AddSingleton<HxdaDb>();

// ⭐ TrainTicketWeb 抢票模拟中心（整合）
builder.Services.AddSingleton<TrainSystem>();
builder.Services.AddSingleton<GrabGameService>();
builder.Services.AddSingleton<TicketTaskService>();
builder.Services.AddSingleton<LeaderboardService>();

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
}).AddJsonProtocol(o =>
{
    o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
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
app.MapHub<BoardGameHub>("/boardGameHub");
app.MapHub<TicketHub>("/ticketHub");
app.MapHub<SpyGameHub>("/spyGameHub");

// ============================================================
// ⭐ TrainTicketWeb 抢票模拟中心（整合）
// ============================================================
var trainSystem = app.Services.GetRequiredService<TrainSystem>();
var ticketHubContext = app.Services.GetRequiredService<IHubContext<TicketHub>>();
var taskService = app.Services.GetRequiredService<TicketTaskService>();
var leaderboardService = app.Services.GetRequiredService<LeaderboardService>();

trainSystem.InitializeDefaultTrains();

// ---------- 事件 → SignalR ----------
trainSystem.OnOrderCreated += o =>
{
    _ = ticketHubContext.Clients.All.SendAsync("OrderCreated", new
    {
        orderId = o.OrderId,
        trainNumber = o.TrainNumber,
        trainName = o.TrainName,
        fromStation = o.FromStation,
        toStation = o.ToStation,
        departureTime = o.DepartureTime,
        arrivalTime = o.ArrivalTime,
        status = o.Status.ToString(),
        createTime = o.CreateTime,
        source = o.Source,
        passengers = o.Passengers.Select(p => new
        {
            p.Name, p.SeatClass, p.PreferredSeat,
            passengerType = p.TypeName, p.Age, p.Gender, p.IdNumber
        }).ToList()
    });
};
trainSystem.OnOrderStatusChanged += o =>
{
    _ = ticketHubContext.Clients.All.SendAsync("OrderUpdated", new
    {
        orderId = o.OrderId,
        status = o.Status.ToString(),
        assignedSeats = o.AssignedSeats,
        failReason = o.FailReason,
        totalPrice = o.TotalPrice,
        attemptCount = o.AttemptCount,
        finishTime = o.FinishTime
    });
};

// ---------- 实时统计广播 ----------
var ttLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
_ = Task.Run(async () =>
{
    while (!ttLifetime.ApplicationStopping.IsCancellationRequested)
    {
        try { await ticketHubContext.Clients.All.SendAsync("StatsUpdated", trainSystem.GetStats()); }
        catch { /* 忽略发送异常 */ }
        try { await Task.Delay(1000, ttLifetime.ApplicationStopping); } catch { break; }
    }
});

// ---------- 列车 ----------
app.MapGet("/api/trains", () => Results.Ok(trainSystem.Trains.Select(t => new
{
    trainNumber = t.TrainNumber,
    trainName = t.TrainName,
    maxSpeed = t.MaxSpeed,
    departureTime = t.DepartureTime.ToString("HH:mm"),
    totalSeats = t.TotalSeats,
    stations = t.Stations.Select(s => new
    {
        name = s.StationName, code = s.StationCode, distance = s.Distance,
        arrive = s.ArriveMinutes, depart = s.DepartMinutes, stop = s.StopMinutes
    }).ToList(),
    carriages = t.Carriages.Select(c => new
    {
        number = c.CarriageNumber,
        type = c.Type.ToString(),
        seatClass = Carriage.TypeName(c.Type),
        seats = c.Seats.Count
    }).ToList()
})));

app.MapGet("/api/seats", (string trainNumber, string station) =>
{
    var t = trainSystem.Trains.FirstOrDefault(x => x.TrainNumber == trainNumber);
    if (t == null) return Results.NotFound(new { error = "列车不存在" });
    int idx = t.Stations.FindIndex(s => s.StationName == station);
    return Results.Ok(t.Carriages.Select(c => new
    {
        carriageNumber = c.CarriageNumber,
        type = c.Type.ToString(),
        seatClass = Carriage.TypeName(c.Type),
        seats = c.Seats.Select(s => new
        {
            row = s.Row,
            column = s.Column,
            seatNumber = s.SeatNumber,
            status = trainSystem.GetSeatStatus(s, t, idx),
            window = s.IsWindow
        }).ToList()
    }));
});

// ---------- 订单 ----------
app.MapPost("/api/buy", (BuyRequest req) =>
{
    var tr = trainSystem.Trains.FirstOrDefault(x => x.TrainNumber == req.Train);
    if (tr == null) return Results.Json(new { success = false, msg = "列车不存在" });

    int fi = tr.Stations.FindIndex(s => s.StationName == req.From);
    int ti = tr.Stations.FindIndex(s => s.StationName == req.To);
    if (fi < 0 || ti < 0 || fi >= ti)
        return Results.Json(new { success = false, msg = "站点选择无效" });

    var o = new Order
    {
        TrainNumber = req.Train,
        TrainName = tr.TrainName,
        FromStation = req.From,
        ToStation = req.To,
        DepartureTime = tr.DepartureTime.AddMinutes(tr.Stations[fi].DepartMinutes).ToString("HH:mm"),
        ArrivalTime = tr.DepartureTime.AddMinutes(tr.Stations[ti].ArriveMinutes).ToString("HH:mm"),
        Source = "user"
    };
    for (int i = fi; i <= ti; i++) o.PassedStations.Add(tr.Stations[i].StationName);

    var names = (req.Name ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var seats = (req.PreferredSeat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var classes = (req.SeatClass ?? "二等座").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    for (int i = 0; i < names.Length; i++)
    {
        var seatPref = i < seats.Length ? seats[i] : "";
        var cls = i < classes.Length ? classes[i] : (classes.Length > 0 ? classes[0] : "二等座");
        var p = new PassengerInfo(names[i], seatPref, cls)
        {
            Age = Random.Shared.Next(18, 60),
            Gender = Random.Shared.Next(2) == 0 ? "男" : "女"
        };
        o.Passengers.Add(p);
    }

    if (o.Passengers.Count == 0)
        return Results.Json(new { success = false, msg = "请至少输入一位乘客" });

    trainSystem.EnqueueOrder(o);
    return Results.Json(new { success = true, orderId = o.OrderId });
});

app.MapGet("/api/orders", (int count = 200) => Results.Ok(trainSystem.RecentOrders(Math.Clamp(count, 1, 1000)).Select(ToOrderDto)));

app.MapGet("/api/order/{id}", (string id) =>
{
    var o = trainSystem.AllOrdersSnapshot().FirstOrDefault(x => x.OrderId == id);
    return o == null ? Results.NotFound(new { error = "订单不存在" }) : Results.Ok(ToOrderDto(o));
});

app.MapPost("/api/pay", (string orderId) =>
{
    var o = trainSystem.AllOrdersSnapshot().FirstOrDefault(x => x.OrderId == orderId);
    if (o == null) return Results.Json(new { success = false, msg = "订单不存在" });
    if (o.Status != OrderStatus.Success) return Results.Json(new { success = false, msg = "订单未出票，无法支付" });
    o.Status = OrderStatus.Paid;
    trainSystem.RaiseOrderStatusChanged(o);
    return Results.Json(new { success = true, msg = "支付成功" });
});

// ---------- 系统控制 ----------
app.MapPost("/api/start", () => { if (IsTaskLocked()) return TaskLockedResponse(); trainSystem.StartSystem(); return Results.Ok(new { running = true }); });
app.MapPost("/api/stop", () => { if (IsTaskLocked()) return TaskLockedResponse(); trainSystem.StopSystem(); return Results.Ok(new { running = false }); });
app.MapPost("/api/traffic", (int rate) => { if (IsTaskLocked()) return TaskLockedResponse(); trainSystem.TrafficRate = Math.Clamp(rate, 0, 1000); return Results.Ok(); });
app.MapPost("/api/server", (int count) => { if (IsTaskLocked()) return TaskLockedResponse(); trainSystem.ServerCount = Math.Clamp(count, 1, 100); trainSystem.AdjustServers(); return Results.Ok(); });
app.MapPost("/api/mode", (string mode) =>
{
    if (IsTaskLocked()) return TaskLockedResponse();
    trainSystem.CurrentMode = mode switch
    {
        "spring" => TrainSystem.TicketingMode.SpringFestival,
        "national" => TrainSystem.TicketingMode.NationalDay,
        "mayday" => TrainSystem.TicketingMode.MayDay,
        _ => TrainSystem.TicketingMode.Normal
    };
    return Results.Ok(new { mode = trainSystem.CurrentMode.ToString() });
});
app.MapPost("/api/addtrain", () => { if (IsTaskLocked()) return TaskLockedResponse(); trainSystem.AddTrain(); return Results.Ok(); });
app.MapPost("/api/removetrain", () => { if (IsTaskLocked()) return TaskLockedResponse(); trainSystem.RemoveTrain(); return Results.Ok(); });
app.MapPost("/api/reset", () =>
{
    if (IsTaskLocked()) return TaskLockedResponse();
    trainSystem.StopSystem();
    trainSystem.OrderQueue.Clear();
    foreach (var t in trainSystem.Trains)
        foreach (var c in t.Carriages)
            foreach (var s in c.Seats)
                s.SectionOccupied.Clear();
    trainSystem.InitializeDefaultTrains();
    return Results.Ok(new { msg = "已重置" });
});

// ---------- 统计 ----------
app.MapGet("/api/stats", () => Results.Ok(trainSystem.GetStats()));

// ---------- 真实抢票任务 ----------
app.MapPost("/api/task/start", (TaskStartRequest req) => Results.Ok(taskService.Start(req.Difficulty ?? "normal")));
app.MapGet("/api/task/current", () => Results.Ok(taskService.CurrentDto()));
app.MapPost("/api/task/stop", () => Results.Ok(taskService.Stop()));
app.MapPost("/api/task/submit", (TaskSubmitRequest req) => Results.Ok(taskService.Submit(req)));
app.MapPost("/api/task/pay", (TaskPayRequest req) => Results.Ok(taskService.Pay(req)));

// ---------- 多人实时抢票排行榜 ----------
app.MapGet("/api/leaderboard", () => Results.Ok(leaderboardService.Get()));
app.MapPost("/api/leaderboard/submit", (LeaderboardSubmitRequest req) => Results.Ok(leaderboardService.Submit(req.Name, req.Score, req.Rank, req.Coins, req.Difficulty, req.TimeSec)));

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
        { "Users", @"CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY, Username TEXT NOT NULL, Email TEXT NOT NULL, PasswordHash TEXT NOT NULL, IsEmailVerified INTEGER DEFAULT 0, IsAdmin INTEGER DEFAULT 0, CreatedAt TEXT, LastLoginAt TEXT, IsBanned INTEGER DEFAULT 0, BanExpiry TEXT, BanReason TEXT, IsDeleted INTEGER DEFAULT 0, DeletedAt TEXT, DeleteReason TEXT, DeleteNote TEXT, AvatarUrl TEXT, IsAvatarApproved INTEGER DEFAULT 0, AvatarSubmittedAt TEXT, PendingEmail TEXT, PendingUsername TEXT, IsEmailChangeApproved INTEGER DEFAULT 0, IsUsernameChangeApproved INTEGER DEFAULT 0, VerificationCode TEXT, VerificationCodeExpiry TEXT, IsApproved INTEGER DEFAULT 0, LoginToken TEXT, LoginTokenExpiry TEXT, IsRestricted INTEGER DEFAULT 0, RestrictionReason TEXT)" },
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
        { "VerifyGameStats", @"CREATE TABLE IF NOT EXISTS VerifyGameStats (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL UNIQUE, TotalScore INTEGER DEFAULT 0, MaxCombo INTEGER DEFAULT 0, MaxLevel INTEGER DEFAULT 0, GamesPlayed INTEGER DEFAULT 0, UpdatedAt TEXT)" },
        { "CasualGameScores", @"CREATE TABLE IF NOT EXISTS CasualGameScores (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL, GameKey TEXT NOT NULL, BestScore INTEGER DEFAULT 0, MaxCombo INTEGER DEFAULT 0, GamesPlayed INTEGER DEFAULT 0, UpdatedAt TEXT, UNIQUE(UserId, GameKey))" },
        { "BoardGameRooms", @"CREATE TABLE IF NOT EXISTS BoardGameRooms (Id INTEGER PRIMARY KEY AUTOINCREMENT, RoomCode TEXT NOT NULL UNIQUE, GameType TEXT NOT NULL, Mode TEXT NOT NULL, Status TEXT NOT NULL DEFAULT 'waiting', HostUserId INTEGER, HostName TEXT, HostAvatar TEXT, HostIsGuest INTEGER DEFAULT 0, HostGuestKey TEXT, GuestUserId INTEGER, GuestName TEXT, GuestAvatar TEXT, GuestIsGuest INTEGER DEFAULT 0, GuestGuestKey TEXT, GuestConnectionId TEXT, AiLevel TEXT, AiSide TEXT, CurrentTurn TEXT NOT NULL DEFAULT 'black', BoardState TEXT NOT NULL, MoveCount INTEGER DEFAULT 0, Winner TEXT, ResultReason TEXT, ChatEnabled INTEGER DEFAULT 1, CreatedAt TEXT, StartedAt TEXT, FinishedAt TEXT, HostToken TEXT, GuestToken TEXT, HostReady INTEGER DEFAULT 0, GuestReady INTEGER DEFAULT 0)" },
        { "BoardGameMoves", @"CREATE TABLE IF NOT EXISTS BoardGameMoves (Id INTEGER PRIMARY KEY AUTOINCREMENT, RoomId INTEGER NOT NULL, MoveIndex INTEGER NOT NULL, Side TEXT NOT NULL, FromPos TEXT, ToPos TEXT, MoveText TEXT, BoardStateAfter TEXT, CreatedAt TEXT)" },
        { "BoardGameChats", @"CREATE TABLE IF NOT EXISTS BoardGameChats (Id INTEGER PRIMARY KEY AUTOINCREMENT, RoomId INTEGER NOT NULL, SenderName TEXT, SenderIsGuest INTEGER DEFAULT 0, SenderSide TEXT, Content TEXT, HarmonyScore INTEGER DEFAULT 100, IsFlagged INTEGER DEFAULT 0, CreatedAt TEXT)" },
        { "BoardGamePrivacyAgreements", @"CREATE TABLE IF NOT EXISTS BoardGamePrivacyAgreements (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER, GuestKey TEXT, Version TEXT NOT NULL, AgreedAt TEXT)" }
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

    // 令牌方案新增列：旧表自动补列（幂等；列已存在时 Turso 返回错误被忽略）
    try { await dataSync.ExecuteSqlAsync("ALTER TABLE BoardGameRooms ADD COLUMN HostToken TEXT"); } catch { }
    try { await dataSync.ExecuteSqlAsync("ALTER TABLE BoardGameRooms ADD COLUMN GuestToken TEXT"); } catch { }
    try { await dataSync.ExecuteSqlAsync("ALTER TABLE BoardGameRooms ADD COLUMN HostReady INTEGER DEFAULT 0"); } catch { }
    try { await dataSync.ExecuteSqlAsync("ALTER TABLE BoardGameRooms ADD COLUMN GuestReady INTEGER DEFAULT 0"); } catch { }

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

// ============================================================
// ⭐ TrainTicketWeb 抢票模拟中心（辅助）
// ============================================================
static object ToOrderDto(Order o) => new
{
    orderId = o.OrderId,
    trainNumber = o.TrainNumber,
    trainName = o.TrainName,
    fromStation = o.FromStation,
    toStation = o.ToStation,
    departureTime = o.DepartureTime,
    arrivalTime = o.ArrivalTime,
    passedStations = o.PassedStations,
    status = o.Status.ToString(),
    failReason = o.FailReason,
    createTime = o.CreateTime,
    finishTime = o.FinishTime,
    assignedSeats = o.AssignedSeats,
    totalPrice = o.TotalPrice,
    source = o.Source,
    attemptCount = o.AttemptCount,
    passengers = o.Passengers.Select(p => new
    {
        p.Name, p.SeatClass, p.PreferredSeat,
        passengerType = p.TypeName, p.Age, p.Gender, p.IdNumber
    }).ToList()
};

bool IsTaskLocked() => taskService.Current is { Active: true };
IResult TaskLockedResponse() => Results.Json(new { ok = false, locked = true, msg = "任务进行中，禁止修改系统参数" });

record BuyRequest(string Train, string From, string To, string Name, string SeatClass, string PreferredSeat);
record TaskStartRequest(string Difficulty);
record LeaderboardSubmitRequest(string Name, int Score, string Rank, int Coins, string Difficulty, int TimeSec);
