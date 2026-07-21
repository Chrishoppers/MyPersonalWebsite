using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
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

// 创建数据库并初始化管理员（只在第一次运行）
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();

    // 只在管理员不存在时创建
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
    else
    {
        Console.WriteLine("✅ 管理员账号已存在，跳过创建");
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
