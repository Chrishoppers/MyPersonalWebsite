using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
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
    builder.Services.AddDbContext<TursoDbContext>(options =>
        options.UseSqlite($"Data Source={tursoUrl};Mode=ReadWriteCreate;Cache=Shared")
    );
    Console.WriteLine("✅ Turso 数据库已配置");
}
else
{
    builder.Services.AddDbContext<TursoDbContext>(options =>
        options.UseSqlite("Data Source=PersonalSite.db")
    );
    Console.WriteLine("⚠️ Turso 未配置，使用本地 SQLite");
}

// ============================================================
// ⭐ DataProtection 使用文件存储（Render 会保留 /app/keys 目录）
// ============================================================
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("MyPersonalWebsite")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(30));

// ============================================================
// ⭐ Session 使用内存缓存（配合持久化的 DataProtection）
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
    var tursoDb = scope.ServiceProvider.GetRequiredService<TursoDbContext>();
    var dataSync = scope.ServiceProvider.GetRequiredService<DataSyncService>();

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
