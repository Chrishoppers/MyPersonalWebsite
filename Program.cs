using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ============================================================
// 本地 SQLite
// ============================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ============================================================
// Turso 云端
// ============================================================
var tursoUrl = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL") ?? "";
var tursoToken = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN") ?? "";

if (!string.IsNullOrEmpty(tursoUrl) && !string.IsNullOrEmpty(tursoToken))
{
    // 使用 Turso 作为主数据库
    builder.Services.AddDbContext<TursoDbContext>(options =>
        options.UseSqlite($"Data Source={tursoUrl};Mode=ReadWriteCreate;Cache=Shared")
    );
    Console.WriteLine("✅ Turso 数据库已配置");
}
else
{
    // 降级到本地 SQLite
    builder.Services.AddDbContext<TursoDbContext>(options =>
        options.UseSqlite("Data Source=PersonalSite.db")
    );
    Console.WriteLine("⚠️ Turso 未配置，使用本地 SQLite");
}

// ============================================================
// ⭐ 使用 Turso 存储 Session（跨部署持久化）
// ============================================================
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<TursoDbContext>()
    .SetApplicationName("MyPersonalWebsite");

// 使用 Turso 作为 Session 存储
builder.Services.AddDistributedSqliteCache(options =>
{
    options.ConnectionString = "Data Source=PersonalSite.db";
    options.SchemaName = "dbo";
    options.TableName = "Sessions";
});

// 或者使用内存缓存（如果 Turso 不可用）
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
    var tursoDb = scope.ServiceProvider.GetRequiredService<TursoDbContext>();
    var dataSync = scope.ServiceProvider.GetRequiredService<DataSyncService>();

    // 创建本地数据库
    db.Database.EnsureCreated();
    Console.WriteLine("✅ 本地 SQLite 数据库已就绪");

    try
    {
        tursoDb.Database.EnsureCreated();
        Console.WriteLine("✅ Turso 数据库已就绪");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Turso 数据库创建失败: {ex.Message}");
        Console.WriteLine("⚠️ 网站将继续使用本地 SQLite");
    }

    // 确保管理员账号存在
    await dataSync.EnsureAdminExistsAsync();
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
