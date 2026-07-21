using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ⭐ 配置 Turso 连接
var tursoUrl = builder.Configuration.GetConnectionString("TursoConnection");
var tursoToken = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN") ?? "";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"{tursoUrl}?authToken={tursoToken}")
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

builder.Services.AddSignalR();

var app = builder.Build();

// ⭐ 自动创建数据库表
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
