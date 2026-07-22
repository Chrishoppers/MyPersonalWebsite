using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ============================================================
// ⭐ 本地 SQLite（仅作为缓存/备用，不用于主数据）
// ============================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ============================================================
// ⭐ TursoDbContext 不再使用 EF Core 连接！
// ============================================================
// 删除或注释掉 TursoDbContext 的注册
// builder.Services.AddDbContext<TursoDbContext>(options =>
//     options.UseSqlite($"Data Source={tursoUrl};Mode=ReadWriteCreate;Cache=Shared")
// );

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
builder.Services.AddScoped<DataSyncService>();  // 改用 TursoService
builder.Services.AddScoped<TursoService>();     // HTTP API

builder.Services.AddSignalR();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var dataSync = scope.ServiceProvider.GetRequiredService<DataSyncService>();

    // 本地数据库仅用于缓存
    db.Database.EnsureCreated();
    Console.WriteLine("✅ 本地 SQLite 缓存已就绪");

    // 确保 Turso 中有管理员账号
    await dataSync.EnsureAdminExistsInTursoAsync();
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
