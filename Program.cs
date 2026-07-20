using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Hubs;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// 添加数据库上下文
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ⭐ 添加 Session 服务
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<CaptchaImageService>();

builder.Services.AddScoped<RateLimitService>();

builder.Services.AddScoped<LikeService>();

// ⭐ 添加 EmailService
builder.Services.AddScoped<EmailService>();

builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ⭐ 启用 Session
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<MessageHub>("/messageHub");
// ===== 临时：重置管理员密码为 Cc752279 =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
    if (admin != null)
    {
        admin.PasswordHash = MyPersonalWebsite.Helpers.PasswordHelper.HashPassword("Cc752279");
        await db.SaveChangesAsync();
        Console.WriteLine("✅ 管理员密码已重置为: Cc752279");
    }
}
// ==========================================
app.Run();