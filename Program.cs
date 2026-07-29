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

// ⭐ 添加这行：禁用文件监控
builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = false;
    options.ValidateOnBuild = false;
});

// ⭐ 添加这行：设置文件监控为 false
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
builder.Services.AddHostedService<StreakEmailService>();

builder.Services.AddScoped<BrevoEmailService>();
builder.Services.AddScoped<SvgCaptchaService>();
builder.Services.AddScoped<RateLimitService>();
builder.Services.AddScoped<DataSyncService>();
builder.Services.AddScoped<TursoService>();
builder.Services.AddScoped<DailyQuestionService>();
builder.Services.AddScoped<GameSuggestionService>();
//builder.Services.AddScoped<CaptchaGameService>();
builder.Services.AddHttpClient<TrainService>();
builder.Services.AddScoped<TrainService>();
// 在 builder.Services 部分添加
builder.Services.AddHttpClient<TrainService>();
builder.Services.AddScoped<TrainService>();
// 注册后台定时服务
builder.Services.AddHostedService<DailyQuestionScheduler>();

builder.Services.AddHttpClient<ReCaptchaService>();
builder.Services.AddScoped<ReCaptchaService>();
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

    await EnsureTursoTablesAsync(dataSync);
    await dataSync.EnsureAdminExistsAsync();
    await EnsureAboutMeDataAsync(dataSync);
    await SeedDailyQuestionBankAsync(dataSync);
    Console.WriteLine("✅ 每日一问题库已就绪");
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
// ⭐ 确保所有 Turso 表存在
// ============================================================
async Task EnsureTursoTablesAsync(DataSyncService dataSync)
{
    Console.WriteLine("📦 检查 Turso 数据表...");

    var tables = new Dictionary<string, string>
    {
        // 在 tables 字典中添加
{ "GameSuggestions", @"
    CREATE TABLE IF NOT EXISTS GameSuggestions (
        Id INTEGER PRIMARY KEY,
        UserId INTEGER NOT NULL,
        GameName TEXT NOT NULL,
        Description TEXT,
        Votes INTEGER DEFAULT 0,
        Status TEXT DEFAULT 'pending',
        CreatedAt TEXT,
        UpdatedAt TEXT
    )"
},
{ "GameSuggestionVotes", @"
    CREATE TABLE IF NOT EXISTS GameSuggestionVotes (
        Id INTEGER PRIMARY KEY,
        SuggestionId INTEGER NOT NULL,
        UserId INTEGER NOT NULL,
        VotedAt TEXT,
        UNIQUE(SuggestionId, UserId)
    )"
},
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
                IsApproved INTEGER DEFAULT 0,
                LoginToken TEXT,
                LoginTokenExpiry TEXT
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
        { "MessageLikes", @"
            CREATE TABLE IF NOT EXISTS MessageLikes (
                Id INTEGER PRIMARY KEY,
                MessageId INTEGER NOT NULL,
                UserId INTEGER NOT NULL,
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
        },
        // ⭐ 每日一问相关表
        { "DailyQuestionBank", @"
            CREATE TABLE IF NOT EXISTS DailyQuestionBank (
                Id INTEGER PRIMARY KEY,
                Question TEXT NOT NULL,
                Answer TEXT NOT NULL,
                Pinyin TEXT NOT NULL,
                Hint TEXT,
                Difficulty INTEGER DEFAULT 1,
                Category TEXT DEFAULT '综合',
                IsActive INTEGER DEFAULT 1,
                CreatedAt TEXT,
                UsedAt TEXT,
                UseCount INTEGER DEFAULT 0
            )"
        },
        { "DailyQuestions", @"
            CREATE TABLE IF NOT EXISTS DailyQuestions (
                Id INTEGER PRIMARY KEY,
                QuestionId INTEGER NOT NULL,
                Date TEXT UNIQUE NOT NULL,
                CreatedAt TEXT
            )"
        },
        { "UserDailyAnswers", @"
            CREATE TABLE IF NOT EXISTS UserDailyAnswers (
                Id INTEGER PRIMARY KEY,
                UserId INTEGER NOT NULL,
                QuestionId INTEGER NOT NULL,
                Answer TEXT,
                IsCorrect INTEGER DEFAULT 0,
                AnswerDate TEXT NOT NULL,
                UNIQUE(UserId, AnswerDate)
            )"
        },
        { "UserGameStats", @"
            CREATE TABLE IF NOT EXISTS UserGameStats (
                Id INTEGER PRIMARY KEY,
                UserId INTEGER NOT NULL UNIQUE,
                TotalPoints INTEGER DEFAULT 0,
                StreakDays INTEGER DEFAULT 0,
                MaxStreakDays INTEGER DEFAULT 0,
                TotalCorrect INTEGER DEFAULT 0,
                TotalAnswered INTEGER DEFAULT 0,
                LastAnswerDate TEXT,
                UpdatedAt TEXT
            )"
        }
    };

    int successCount = 0;
    int failCount = 0;

    foreach (var table in tables)
    {
        try
        {
            var checkResult = await dataSync.QueryAsync($"SELECT name FROM sqlite_master WHERE type='table' AND name='{table.Key}'");
            if (checkResult.Contains($"\"{table.Key}\""))
            {
                Console.WriteLine($"✅ 表 {table.Key} 已存在，跳过创建");
                successCount++;
                continue;
            }

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
// ⭐ 确保 AboutMe 数据存在
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

// ============================================================
// 📦 初始化 200 道题库
// ============================================================
async Task SeedDailyQuestionBankAsync(DataSyncService dataSync)
{
    // 检查是否已有题目
    var checkResult = await dataSync.QueryAsync("SELECT COUNT(*) as Count FROM DailyQuestionBank");
    if (!checkResult.Contains("\"rows\":[{\"value\":0}]") && !checkResult.Contains("\"rows\":[]"))
    {
        Console.WriteLine("✅ 题库已存在，跳过初始化");
        return;
    }

    Console.WriteLine("📦 正在初始化 200 道题库...");

    var questions = GetDefaultQuestions();
    int successCount = 0;

    foreach (var q in questions)
    {
        var sql = $@"INSERT INTO DailyQuestionBank (
            Question, Answer, Pinyin, Hint, Difficulty, Category, IsActive, CreatedAt
        ) VALUES (
            '{EscapeSql(q.Question)}',
            '{EscapeSql(q.Answer)}',
            '{EscapeSql(q.Pinyin)}',
            {(string.IsNullOrEmpty(q.Hint) ? "NULL" : $"'{EscapeSql(q.Hint)}'")},
            {q.Difficulty},
            '{EscapeSql(q.Category)}',
            1,
            '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
        )";

        var result = await dataSync.ExecuteSqlAsync(sql);
        if (result) successCount++;
    }

    Console.WriteLine($"✅ 题库初始化完成！共 {successCount} 道题");
}

// ============================================================
// 📦 200 道题数据
// ============================================================
List<BankQuestion> GetDefaultQuestions()
{
    var list = new List<BankQuestion>();

    // ==================== 生活常识类（40道） ====================
    list.AddRange(new[]
    {
        new BankQuestion { Question = "什么水果被称为'水果之王'？", Answer = "榴莲", Pinyin = "liulian", Hint = "气味浓郁，有人爱有人恨", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "人体最大的器官是？", Answer = "皮肤", Pinyin = "pifu", Hint = "覆盖全身，起保护作用", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "被称为'沙漠之舟'的动物是？", Answer = "骆驼", Pinyin = "luotuo", Hint = "能长时间不喝水", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "世界上使用人数最多的语言是？", Answer = "中文", Pinyin = "zhongwen", Hint = "母语使用者超过10亿", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "地球的天然卫星是？", Answer = "月球", Pinyin = "yueqiu", Hint = "夜晚最亮的天体", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "世界上最快的陆地动物是？", Answer = "猎豹", Pinyin = "liebao", Hint = "时速可达120公里", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "中国最长的河流是？", Answer = "长江", Pinyin = "changjiang", Hint = "发源于青藏高原", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "世界上面积最大的国家是？", Answer = "俄罗斯", Pinyin = "eluosi", Hint = "横跨欧亚大陆", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "太阳系中最大的行星是？", Answer = "木星", Pinyin = "muxing", Hint = "气态巨行星，有大红斑", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "被称为'东方之珠'的城市是？", Answer = "香港", Pinyin = "xianggang", Hint = "特别行政区，维多利亚港", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "世界上最长的城墙是？", Answer = "万里长城", Pinyin = "wanlichangcheng", Hint = "中国，总长度超过2万公里", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "世界上最小的国家是？", Answer = "梵蒂冈", Pinyin = "fandigang", Hint = "位于罗马城内，天主教中心", Difficulty = 2, Category = "生活" },
        new BankQuestion { Question = "中国最大的淡水湖是？", Answer = "鄱阳湖", Pinyin = "poyanghu", Hint = "位于江西省", Difficulty = 3, Category = "生活" },
        new BankQuestion { Question = "世界上最大的海洋是？", Answer = "太平洋", Pinyin = "taipingyang", Hint = "面积约1.8亿平方公里", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "被称为'天下第一关'的是？", Answer = "山海关", Pinyin = "shanhaiguan", Hint = "明长城的东端起点", Difficulty = 3, Category = "生活" },
        new BankQuestion { Question = "世界上人口最多的国家是？", Answer = "印度", Pinyin = "yindu", Hint = "2023年超过中国", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "被称为'万山之祖'的是哪座山？", Answer = "昆仑山", Pinyin = "kunlunshan", Hint = "神话中的神山", Difficulty = 4, Category = "生活" },
        new BankQuestion { Question = "中国四大发明中，哪一项与航海有关？", Answer = "指南针", Pinyin = "zhinanzhen", Hint = "宋代开始用于航海", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "中国四大发明中，哪一项与书写材料有关？", Answer = "造纸术", Pinyin = "zaozhishu", Hint = "蔡伦改进", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "人类最早的文字是？", Answer = "楔形文字", Pinyin = "xiexingwenzi", Hint = "苏美尔人创造", Difficulty = 4, Category = "生活" },
        new BankQuestion { Question = "中国最大的岛屿是？", Answer = "台湾岛", Pinyin = "taiwandao", Hint = "位于东南沿海", Difficulty = 2, Category = "生活" },
        new BankQuestion { Question = "世界上最高的山峰是？", Answer = "珠穆朗玛峰", Pinyin = "zhumulangmafeng", Hint = "海拔8848.86米", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "中国最早的文字是？", Answer = "甲骨文", Pinyin = "jiaguwen", Hint = "商朝时期刻在龟甲上", Difficulty = 2, Category = "生活" },
        new BankQuestion { Question = "被称为'世界屋脊'的高原是？", Answer = "青藏高原", Pinyin = "qingzanggaoyuan", Hint = "平均海拔4000米以上", Difficulty = 2, Category = "生活" },
        new BankQuestion { Question = "中国四大名楼中，位于武汉的是？", Answer = "黄鹤楼", Pinyin = "huanghelou", Hint = "崔颢在此题诗", Difficulty = 2, Category = "生活" },
        new BankQuestion { Question = "世界上最大的沙漠是？", Answer = "撒哈拉沙漠", Pinyin = "sahala", Hint = "位于非洲北部", Difficulty = 2, Category = "生活" },
        new BankQuestion { Question = "中国最大的沙漠是？", Answer = "塔克拉玛干沙漠", Pinyin = "takelamagan", Hint = "位于新疆", Difficulty = 3, Category = "生活" },
        new BankQuestion { Question = "被称为'天府之国'的是？", Answer = "四川", Pinyin = "sichuan", Hint = "成都平原", Difficulty = 2, Category = "生活" },
        new BankQuestion { Question = "中国四大石窟中，位于甘肃的是？", Answer = "莫高窟", Pinyin = "mogaoku", Hint = "敦煌", Difficulty = 3, Category = "生活" },
        new BankQuestion { Question = "被称为'中国夏威夷'的是？", Answer = "三亚", Pinyin = "sanya", Hint = "海南省", Difficulty = 2, Category = "生活" },
        new BankQuestion { Question = "世界上人口最多的城市是？", Answer = "东京", Pinyin = "dongjing", Hint = "日本首都", Difficulty = 2, Category = "生活" },
        new BankQuestion { Question = "中国最古老的城市是？", Answer = "西安", Pinyin = "xian", Hint = "十三朝古都", Difficulty = 2, Category = "生活" },
        new BankQuestion { Question = "被称为'音乐之都'的是？", Answer = "维也纳", Pinyin = "weiyena", Hint = "奥地利首都", Difficulty = 3, Category = "生活" },
        new BankQuestion { Question = "中国四大发明中，哪一项与爆炸物有关？", Answer = "火药", Pinyin = "huoyao", Hint = "炼丹术士意外发现", Difficulty = 1, Category = "生活" },
        new BankQuestion { Question = "中国四大发明中，哪一项与印刷有关？", Answer = "活字印刷术", Pinyin = "huoziyinshuashu", Hint = "毕昇发明", Difficulty = 2, Category = "生活" },
        new BankQuestion { Question = "世界上最大的瀑布是？", Answer = "尼亚加拉瀑布", Pinyin = "niyajiala", Hint = "位于美国与加拿大边境", Difficulty = 3, Category = "生活" },
        new BankQuestion { Question = "中国最长的桥梁是？", Answer = "丹昆特大桥", Pinyin = "dankuntedaqiao", Hint = "京沪高速铁路", Difficulty = 4, Category = "生活" },
        new BankQuestion { Question = "被称为'海上花园'的是？", Answer = "厦门", Pinyin = "xiamen", Hint = "福建省", Difficulty = 2, Category = "生活" },
        new BankQuestion { Question = "中国最大的半岛是？", Answer = "山东半岛", Pinyin = "shandongbandao", Hint = "位于华东", Difficulty = 2, Category = "生活" },
        new BankQuestion { Question = "世界上最大的岛屿是？", Answer = "格陵兰岛", Pinyin = "gelinglandao", Hint = "位于北美洲东北部", Difficulty = 3, Category = "生活" },
    });

    // ==================== 历史类（40道） ====================
    list.AddRange(new[]
    {
        new BankQuestion { Question = "中国历史上的第一个王朝是？", Answer = "夏朝", Pinyin = "xiachao", Hint = "约公元前2070年建立", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国最后一个封建王朝是？", Answer = "清朝", Pinyin = "qingchao", Hint = "1912年灭亡", Difficulty = 1, Category = "历史" },
        new BankQuestion { Question = "中国历史上最长的朝代是？", Answer = "周朝", Pinyin = "zhouchao", Hint = "约800年", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国历史上唯一的女皇帝是？", Answer = "武则天", Pinyin = "wuzetian", Hint = "唐朝", Difficulty = 1, Category = "历史" },
        new BankQuestion { Question = "中国历史上的'开元盛世'是哪个皇帝在位？", Answer = "唐玄宗", Pinyin = "tangxuanzong", Hint = "原名李隆基", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国历史上的'贞观之治'是哪个皇帝在位？", Answer = "唐太宗", Pinyin = "tangtaizong", Hint = "原名李世民", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "秦朝的都城在哪里？", Answer = "咸阳", Pinyin = "xianyang", Hint = "陕西", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "清朝的都城是？", Answer = "北京", Pinyin = "beijing", Hint = "紫禁城", Difficulty = 1, Category = "历史" },
        new BankQuestion { Question = "中国历史上'三国时期'是哪三国？", Answer = "魏蜀吴", Pinyin = "weishuwu", Hint = "曹操、刘备、孙权", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国历史上的'赤壁之战'发生在哪一年？", Answer = "208年", Pinyin = "erbalingba", Hint = "三国时期", Difficulty = 3, Category = "历史" },
        new BankQuestion { Question = "中国历史中，'五代十国'的'五代'之首是？", Answer = "后梁", Pinyin = "houliang", Hint = "朱温建立", Difficulty = 4, Category = "历史" },
        new BankQuestion { Question = "中国历史中的'宋元明清'，北宋都城是？", Answer = "汴京", Pinyin = "bianjing", Hint = "今河南开封", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'丝绸之路'是哪个朝代开辟的？", Answer = "汉朝", Pinyin = "hanchao", Hint = "张骞出使西域", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'郑和下西洋'是哪个朝代？", Answer = "明朝", Pinyin = "mingchao", Hint = "永乐年间", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'鸦片战争'爆发于哪一年？", Answer = "1840年", Pinyin = "yibasiling", Hint = "清朝", Difficulty = 3, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'辛亥革命'爆发于哪一年？", Answer = "1911年", Pinyin = "yijiuyiyi", Hint = "推翻清朝", Difficulty = 3, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'五四运动'发生在哪一年？", Answer = "1919年", Pinyin = "yijiuyijiu", Hint = "反帝爱国运动", Difficulty = 3, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'长征'途中，红军翻越的第一座雪山是？", Answer = "夹金山", Pinyin = "jiajinshan", Hint = "四川", Difficulty = 4, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'西安事变'发生在哪一年？", Answer = "1936年", Pinyin = "yijiusanliu", Hint = "张学良、杨虎城", Difficulty = 4, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'抗日战争'开始于哪一年？", Answer = "1937年", Pinyin = "yijiusanqi", Hint = "卢沟桥事变", Difficulty = 3, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'渡江战役'是解放哪个城市？", Answer = "南京", Pinyin = "nanjing", Hint = "国民党的首都", Difficulty = 3, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'科举制度'始于哪个朝代？", Answer = "隋朝", Pinyin = "suichao", Hint = "杨坚建立", Difficulty = 3, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'科举制度'废除于哪一年？", Answer = "1905年", Pinyin = "yijiuwuling", Hint = "清朝", Difficulty = 4, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'元朝'由哪个民族建立？", Answer = "蒙古族", Pinyin = "mengguzu", Hint = "成吉思汗", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'明朝'是哪个皇帝建立的？", Answer = "朱元璋", Pinyin = "zhuyuanzhang", Hint = "乞丐皇帝", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'唐朝'的开国皇帝是？", Answer = "李渊", Pinyin = "liyuan", Hint = "晋阳起兵", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'宋太祖'是谁？", Answer = "赵匡胤", Pinyin = "zhaokuangyin", Hint = "陈桥兵变", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'土木堡之变'发生在哪个朝代？", Answer = "明朝", Pinyin = "mingchao", Hint = "明英宗被俘", Difficulty = 4, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'戊戌变法'发生在哪一年？", Answer = "1898年", Pinyin = "yijiubajiu", Hint = "清朝", Difficulty = 4, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'九一八事变'发生在哪一年？", Answer = "1931年", Pinyin = "yijiusanyao", Hint = "日本侵华", Difficulty = 3, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'南京大屠杀'发生在哪一年？", Answer = "1937年", Pinyin = "yijiusanqi", Hint = "日本侵略", Difficulty = 3, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'重庆谈判'发生在哪一年？", Answer = "1945年", Pinyin = "yijiusiwu", Hint = "国共谈判", Difficulty = 4, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'开国大典'发生在哪一年？", Answer = "1949年", Pinyin = "yijiusijiu", Hint = "新中国成立", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'抗美援朝'发生在哪一年？", Answer = "1950年", Pinyin = "yijiuwuling", Hint = "朝鲜战争", Difficulty = 3, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'两弹一星'中的'两弹'是什么？", Answer = "原子弹氢弹", Pinyin = "yuanzidanqingdan", Hint = "核武器", Difficulty = 4, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'文化大革命'持续了几年？", Answer = "10年", Pinyin = "shinian", Hint = "1966-1976", Difficulty = 4, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'改革开放'始于哪一年？", Answer = "1978年", Pinyin = "yijiuqiba", Hint = "十一届三中全会", Difficulty = 3, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'香港回归'发生在哪一年？", Answer = "1997年", Pinyin = "yijiujiuqi", Hint = "英国殖民结束", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'澳门回归'发生在哪一年？", Answer = "1999年", Pinyin = "yijiujiujiu", Hint = "葡萄牙殖民结束", Difficulty = 2, Category = "历史" },
        new BankQuestion { Question = "中国历史上，'神舟五号'发射成功是哪一年？", Answer = "2003年", Pinyin = "erlingsan", Hint = "杨利伟", Difficulty = 3, Category = "历史" },
    });

    // ==================== 文学类（40道） ====================
    list.AddRange(new[]
    {
        new BankQuestion { Question = "'床前明月光'下一句是？", Answer = "疑是地上霜", Pinyin = "yishidishangshuang", Hint = "李白《静夜思》", Difficulty = 1, Category = "文学" },
        new BankQuestion { Question = "'举头望明月'下一句是？", Answer = "低头思故乡", Pinyin = "ditousiguxiang", Hint = "李白《静夜思》", Difficulty = 1, Category = "文学" },
        new BankQuestion { Question = "'锄禾日当午'下一句是？", Answer = "汗滴禾下土", Pinyin = "handihexiatu", Hint = "李绅《悯农》", Difficulty = 1, Category = "文学" },
        new BankQuestion { Question = "'谁知盘中餐'下一句是？", Answer = "粒粒皆辛苦", Pinyin = "lilijiexinku", Hint = "李绅《悯农》", Difficulty = 1, Category = "文学" },
        new BankQuestion { Question = "'春眠不觉晓'下一句是？", Answer = "处处闻啼鸟", Pinyin = "chuchuwentinao", Hint = "孟浩然《春晓》", Difficulty = 1, Category = "文学" },
        new BankQuestion { Question = "'夜来风雨声'下一句是？", Answer = "花落知多少", Pinyin = "hualuozhiduoshao", Hint = "孟浩然《春晓》", Difficulty = 1, Category = "文学" },
        new BankQuestion { Question = "'欲穷千里目'下一句是？", Answer = "更上一层楼", Pinyin = "gengshangyicenglou", Hint = "王之涣《登鹳雀楼》", Difficulty = 1, Category = "文学" },
        new BankQuestion { Question = "'黄河入海流'的上一句是？", Answer = "白日依山尽", Pinyin = "bairiyishanjin", Hint = "王之涣《登鹳雀楼》", Difficulty = 2, Category = "文学" },
        new BankQuestion { Question = "'会当凌绝顶'下一句是？", Answer = "一览众山小", Pinyin = "yilanzhongshanxiao", Hint = "杜甫《望岳》", Difficulty = 2, Category = "文学" },
        new BankQuestion { Question = "'随风潜入夜'下一句是？", Answer = "润物细无声", Pinyin = "runwuxiwusheng", Hint = "杜甫《春夜喜雨》", Difficulty = 2, Category = "文学" },
        new BankQuestion { Question = "'野火烧不尽'下一句是？", Answer = "春风吹又生", Pinyin = "chunfengchuiyousheng", Hint = "白居易《赋得古原草送别》", Difficulty = 2, Category = "文学" },
        new BankQuestion { Question = "'远芳侵古道'下一句是？", Answer = "晴翠接荒城", Pinyin = "qingcuijiehuangcheng", Hint = "白居易《赋得古原草送别》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'孤帆远影碧空尽'下一句是？", Answer = "唯见长江天际流", Pinyin = "weijianchangjiangtianjiliu", Hint = "李白《黄鹤楼送孟浩然之广陵》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'故人西辞黄鹤楼'下一句是？", Answer = "烟花三月下扬州", Pinyin = "yanhuasanyuexiayangzhou", Hint = "李白《黄鹤楼送孟浩然之广陵》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'海内存知己'下一句是？", Answer = "天涯若比邻", Pinyin = "tianyoruobilin", Hint = "王勃《送杜少府之任蜀州》", Difficulty = 2, Category = "文学" },
        new BankQuestion { Question = "'城阙辅三秦'下一句是？", Answer = "风烟望五津", Pinyin = "fengyanwangwujin", Hint = "王勃《送杜少府之任蜀州》", Difficulty = 4, Category = "文学" },
        new BankQuestion { Question = "'月落乌啼霜满天'下一句是？", Answer = "江枫渔火对愁眠", Pinyin = "jiangfengyuhuoduichoumian", Hint = "张继《枫桥夜泊》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'姑苏城外寒山寺'下一句是？", Answer = "夜半钟声到客船", Pinyin = "yebanzhongshengdaokechuan", Hint = "张继《枫桥夜泊》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'千山鸟飞绝'下一句是？", Answer = "万径人踪灭", Pinyin = "wanjingrenzongmie", Hint = "柳宗元《江雪》", Difficulty = 2, Category = "文学" },
        new BankQuestion { Question = "'孤舟蓑笠翁'下一句是？", Answer = "独钓寒江雪", Pinyin = "dudiaohanjiangxue", Hint = "柳宗元《江雪》", Difficulty = 2, Category = "文学" },
        new BankQuestion { Question = "'天生我材必有用'下一句是？", Answer = "千金散尽还复来", Pinyin = "qianjinsanjinhuanfulai", Hint = "李白《将进酒》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'人生得意须尽欢'下一句是？", Answer = "莫使金樽空对月", Pinyin = "moshijinzunkongduiyue", Hint = "李白《将进酒》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'问君能有几多愁'下一句是？", Answer = "恰似一江春水向东流", Pinyin = "qiasi yijiang chunshui xiangdongliu", Hint = "李煜《虞美人》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'春花秋月何时了'下一句是？", Answer = "往事知多少", Pinyin = "wangshizhiduoshao", Hint = "李煜《虞美人》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'大漠沙如雪'下一句是？", Answer = "燕山月似钩", Pinyin = "yanshanyuesigou", Hint = "李贺《马诗》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'何当金络脑'下一句是？", Answer = "快走踏清秋", Pinyin = "kuaizoutaqingqiu", Hint = "李贺《马诗》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'采菊东篱下'下一句是？", Answer = "悠然见南山", Pinyin = "youranjiannanshan", Hint = "陶渊明《饮酒》", Difficulty = 2, Category = "文学" },
        new BankQuestion { Question = "'结庐在人境'下一句是？", Answer = "而无车马喧", Pinyin = "erwuchemaxuan", Hint = "陶渊明《饮酒》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'山重水复疑无路'下一句是？", Answer = "柳暗花明又一村", Pinyin = "liuanhuamingyouyicun", Hint = "陆游《游山西村》", Difficulty = 2, Category = "文学" },
        new BankQuestion { Question = "'莫笑农家腊酒浑'下一句是？", Answer = "丰年留客足鸡豚", Pinyin = "fengnianliukezujitun", Hint = "陆游《游山西村》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'人生自古谁无死'下一句是？", Answer = "留取丹心照汗青", Pinyin = "liuqudanxinzhaohanqing", Hint = "文天祥《过零丁洋》", Difficulty = 2, Category = "文学" },
        new BankQuestion { Question = "'辛苦遭逢起一经'下一句是？", Answer = "干戈寥落四周星", Pinyin = "gangeliaoluosizhouxing", Hint = "文天祥《过零丁洋》", Difficulty = 4, Category = "文学" },
        new BankQuestion { Question = "'先天下之忧而忧'下一句是？", Answer = "后天下之乐而乐", Pinyin = "houtianxiazhi le er le", Hint = "范仲淹《岳阳楼记》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'不以物喜'下一句是？", Answer = "不以己悲", Pinyin = "buyijibei", Hint = "范仲淹《岳阳楼记》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'醉翁之意不在酒'下一句是？", Answer = "在乎山水之间也", Pinyin = "zaihushanshuizhijianye", Hint = "欧阳修《醉翁亭记》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'环滁皆山也'下一句是？", Answer = "其西南诸峰", Pinyin = "qixinan zhufeng", Hint = "欧阳修《醉翁亭记》", Difficulty = 4, Category = "文学" },
        new BankQuestion { Question = "'落霞与孤鹜齐飞'下一句是？", Answer = "秋水共长天一色", Pinyin = "qiushui gongchangtian yise", Hint = "王勃《滕王阁序》", Difficulty = 4, Category = "文学" },
        new BankQuestion { Question = "'豫章故郡'下一句是？", Answer = "洪都新府", Pinyin = "hongdu xinfu", Hint = "王勃《滕王阁序》", Difficulty = 4, Category = "文学" },
        new BankQuestion { Question = "'北冥有鱼'下一句是？", Answer = "其名为鲲", Pinyin = "qimingweikun", Hint = "庄子《逍遥游》", Difficulty = 3, Category = "文学" },
        new BankQuestion { Question = "'鹏之徙于南冥也'下一句是？", Answer = "水击三千里", Pinyin = "shuijisanqianli", Hint = "庄子《逍遥游》", Difficulty = 4, Category = "文学" },
    });

    // ==================== 科技类（40道） ====================
    list.AddRange(new[]
    {
        new BankQuestion { Question = "电脑的CPU主要由什么材料制成？", Answer = "硅", Pinyin = "gui", Hint = "半导体材料", Difficulty = 3, Category = "科技" },
        new BankQuestion { Question = "互联网的发明者是谁？", Answer = "蒂姆伯纳斯李", Pinyin = "dimu bonasi li", Hint = "万维网之父", Difficulty = 3, Category = "科技" },
        new BankQuestion { Question = "苹果公司的创始人是谁？", Answer = "乔布斯", Pinyin = "qiaobusi", Hint = "iPhone之父", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "微软公司的创始人是谁？", Answer = "比尔盖茨", Pinyin = "bier gaici", Hint = "Windows之父", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "特斯拉公司的CEO是谁？", Answer = "马斯克", Pinyin = "masike", Hint = "SpaceX创始人", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "华为公司的创始人是谁？", Answer = "任正非", Pinyin = "renzhengfei", Hint = "中国科技巨头", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "小米公司的创始人是谁？", Answer = "雷军", Pinyin = "leijun", Hint = "Are you OK?", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "阿里巴巴的创始人是谁？", Answer = "马云", Pinyin = "mayun", Hint = "电商巨头", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "腾讯公司的创始人是谁？", Answer = "马化腾", Pinyin = "mahuateng", Hint = "微信之父", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "世界上第一台计算机叫什么？", Answer = "ENIAC", Pinyin = "eniac", Hint = "美国宾夕法尼亚大学", Difficulty = 4, Category = "科技" },
        new BankQuestion { Question = "第一颗人造卫星叫什么？", Answer = "斯普特尼克1号", Pinyin = "siputenike yihao", Hint = "苏联发射", Difficulty = 4, Category = "科技" },
        new BankQuestion { Question = "第一个登上月球的国家是？", Answer = "美国", Pinyin = "meiguo", Hint = "阿波罗计划", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "第一位进入太空的中国人是？", Answer = "杨利伟", Pinyin = "yangliwei", Hint = "神舟五号", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "中国空间站叫什么名字？", Answer = "天宫", Pinyin = "tiangong", Hint = "中国自己的空间站", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "DNA的中文全称是？", Answer = "脱氧核糖核酸", Pinyin = "tuoyanghetanghesuan", Hint = "遗传物质", Difficulty = 4, Category = "科技" },
        new BankQuestion { Question = "克隆羊多莉诞生于哪一年？", Answer = "1996年", Pinyin = "yijiu", Hint = "英国", Difficulty = 4, Category = "科技" },
        new BankQuestion { Question = "人工智能的英文缩写是？", Answer = "AI", Pinyin = "ai", Hint = "Artificial Intelligence", Difficulty = 1, Category = "科技" },
        new BankQuestion { Question = "机器学习的英文缩写是？", Answer = "ML", Pinyin = "ML", Hint = "Machine Learning", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "区块链技术的核心是？", Answer = "去中心化", Pinyin = "quzhongxinhua", Hint = "比特币的基础", Difficulty = 3, Category = "科技" },
        new BankQuestion { Question = "中国第一颗原子弹爆炸成功是？", Answer = "1964年", Pinyin = "yijiu", Hint = "新疆罗布泊", Difficulty = 3, Category = "科技" },
        new BankQuestion { Question = "中国第一颗氢弹爆炸成功是？", Answer = "1967年", Pinyin = "yijiu", Hint = "新疆", Difficulty = 3, Category = "科技" },
        new BankQuestion { Question = "国际空间站由多少个国家共同建造？", Answer = "15国", Pinyin = "shiwuguo", Hint = "包括美国、俄罗斯等", Difficulty = 4, Category = "科技" },
        new BankQuestion { Question = "中国火星探测器叫什么？", Answer = "天问一号", Pinyin = "tianwen yihao", Hint = "2020年发射", Difficulty = 3, Category = "科技" },
        new BankQuestion { Question = "量子计算机使用什么进行计算？", Answer = "量子比特", Pinyin = "liangzi bite", Hint = "叠加态", Difficulty = 4, Category = "科技" },
        new BankQuestion { Question = "5G中的'G'代表什么？", Answer = "代", Pinyin = "dai", Hint = "Generation", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "3D打印技术又称为什么？", Answer = "增材制造", Pinyin = "zengcaizhizao", Hint = "逐层叠加材料", Difficulty = 3, Category = "科技" },
        new BankQuestion { Question = "LED的中文全称是？", Answer = "发光二极管", Pinyin = "faguang erjiguan", Hint = "Light Emitting Diode", Difficulty = 3, Category = "科技" },
        new BankQuestion { Question = "光纤通信使用什么进行数据传输？", Answer = "光信号", Pinyin = "guangxinhao", Hint = "光在玻璃纤维中传输", Difficulty = 3, Category = "科技" },
        new BankQuestion { Question = "VR技术的中文名称是？", Answer = "虚拟现实", Pinyin = "xunixianshi", Hint = "Virtual Reality", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "AR技术的中文名称是？", Answer = "增强现实", Pinyin = "zengqiangxianshi", Hint = "Augmented Reality", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "CPU的中文名称是？", Answer = "中央处理器", Pinyin = "zhongyangchuliqi", Hint = "Central Processing Unit", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "GPU的中文名称是？", Answer = "图形处理器", Pinyin = "tuxingchuliqi", Hint = "Graphics Processing Unit", Difficulty = 2, Category = "科技" },
        new BankQuestion { Question = "RAM的中文名称是？", Answer = "随机存取存储器", Pinyin = "suijicunqu cunchuqi", Hint = "Random Access Memory", Difficulty = 3, Category = "科技" },
        new BankQuestion { Question = "ROM的中文名称是？", Answer = "只读存储器", Pinyin = "zhidu cunchuqi", Hint = "Read-Only Memory", Difficulty = 3, Category = "科技" },
    });

    // ==================== 地理类（40道） ====================
    list.AddRange(new[]
    {
        new BankQuestion { Question = "世界最长的河流是？", Answer = "尼罗河", Pinyin = "niluohe", Hint = "非洲", Difficulty = 1, Category = "地理" },
        new BankQuestion { Question = "世界第二长的河流是？", Answer = "亚马逊河", Pinyin = "yamaxunhe", Hint = "南美洲", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "世界第三长的河流是？", Answer = "长江", Pinyin = "changjiang", Hint = "中国", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "世界最大的湖泊是？", Answer = "里海", Pinyin = "lihai", Hint = "也是咸水湖", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "世界最大的淡水湖是？", Answer = "贝加尔湖", Pinyin = "beijiaerhu", Hint = "西伯利亚", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "世界最深的湖泊是？", Answer = "贝加尔湖", Pinyin = "beijiaerhu", Hint = "1637米", Difficulty = 3, Category = "地理" },
        new BankQuestion { Question = "世界最大的平原是？", Answer = "亚马逊平原", Pinyin = "yamaxun pingyuan", Hint = "南美洲", Difficulty = 3, Category = "地理" },
        new BankQuestion { Question = "世界最大的高原是？", Answer = "青藏高原", Pinyin = "qingzanggaoyuan", Hint = "中国", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "世界最大的沙漠是？", Answer = "撒哈拉沙漠", Pinyin = "sahala", Hint = "非洲北部", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "世界最大的雨林是？", Answer = "亚马逊雨林", Pinyin = "yamaxun yulin", Hint = "南美洲", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "世界最长的山脉是？", Answer = "安第斯山脉", Pinyin = "andisi shanmai", Hint = "南美洲", Difficulty = 3, Category = "地理" },
        new BankQuestion { Question = "世界最高的山脉是？", Answer = "喜马拉雅山脉", Pinyin = "ximalaya shanmai", Hint = "亚洲", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "世界最高的山峰是？", Answer = "珠穆朗玛峰", Pinyin = "zhumulangmafeng", Hint = "8848米", Difficulty = 1, Category = "地理" },
        new BankQuestion { Question = "中国最高的山峰是？", Answer = "珠穆朗玛峰", Pinyin = "zhumulangmafeng", Hint = "8848米", Difficulty = 1, Category = "地理" },
        new BankQuestion { Question = "中国最大的沙漠是？", Answer = "塔克拉玛干沙漠", Pinyin = "takelamagan", Hint = "新疆", Difficulty = 3, Category = "地理" },
        new BankQuestion { Question = "中国最大的盆地是？", Answer = "塔里木盆地", Pinyin = "talimu pendi", Hint = "新疆", Difficulty = 3, Category = "地理" },
        new BankQuestion { Question = "中国最大的平原是？", Answer = "东北平原", Pinyin = "dongbei pingyuan", Hint = "黑龙江、吉林、辽宁", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "中国最大的岛屿是？", Answer = "台湾岛", Pinyin = "taiwandao", Hint = "面积约3.6万平方公里", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "中国最大的半岛是？", Answer = "山东半岛", Pinyin = "shandongbandao", Hint = "渤海与黄海之间", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "中国最大的海峡是？", Answer = "台湾海峡", Pinyin = "taiwanhaixia", Hint = "福建与台湾之间", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "中国最大的内海是？", Answer = "渤海", Pinyin = "bohai", Hint = "被辽东半岛和山东半岛包围", Difficulty = 3, Category = "地理" },
        new BankQuestion { Question = "中国最大的咸水湖是？", Answer = "青海湖", Pinyin = "qinghaihu", Hint = "青海省", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "中国最大的淡水湖是？", Answer = "鄱阳湖", Pinyin = "poyanghu", Hint = "江西省", Difficulty = 3, Category = "地理" },
        new BankQuestion { Question = "中国最长的铁路是？", Answer = "青藏铁路", Pinyin = "qingzangtielu", Hint = "西宁到拉萨", Difficulty = 3, Category = "地理" },
        new BankQuestion { Question = "世界最大的珊瑚礁是？", Answer = "大堡礁", Pinyin = "dabaijiao", Hint = "澳大利亚", Difficulty = 3, Category = "地理" },
        new BankQuestion { Question = "世界最大的峡谷是？", Answer = "雅鲁藏布大峡谷", Pinyin = "yaluzangbu", Hint = "中国西藏", Difficulty = 4, Category = "地理" },
        new BankQuestion { Question = "世界最大的半岛是？", Answer = "阿拉伯半岛", Pinyin = "alabo bandao", Hint = "亚洲西部", Difficulty = 3, Category = "地理" },
        new BankQuestion { Question = "世界最大的群岛是？", Answer = "马来群岛", Pinyin = "malai qundao", Hint = "东南亚", Difficulty = 4, Category = "地理" },
        new BankQuestion { Question = "世界最大的湖泊（咸水湖）是？", Answer = "里海", Pinyin = "lihai", Hint = "亚欧交界", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "世界最大的淡水湖是？", Answer = "贝加尔湖", Pinyin = "beijiaerhu", Hint = "西伯利亚", Difficulty = 2, Category = "地理" },
        new BankQuestion { Question = "世界最大的内陆国是？", Answer = "哈萨克斯坦", Pinyin = "hasakesitan", Hint = "中亚", Difficulty = 3, Category = "地理" },
        new BankQuestion { Question = "世界最小的内陆国是？", Answer = "梵蒂冈", Pinyin = "fandigang", Hint = "罗马城内", Difficulty = 3, Category = "地理" },
        new BankQuestion { Question = "世界最大的国家是？", Answer = "俄罗斯", Pinyin = "eluosi", Hint = "横跨11个时区", Difficulty = 1, Category = "地理" },
    });

    return list;
}

// ============================================================
// 🛠️ 辅助方法
// ============================================================
string EscapeSql(string value)
{
    if (string.IsNullOrEmpty(value)) return "";
    return value.Replace("'", "''");
}

