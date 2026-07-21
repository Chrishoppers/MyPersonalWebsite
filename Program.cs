using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1. 注册 MVC 服务
builder.Services.AddControllersWithViews();

// 2. 注册数据库上下文（SQLite）
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 3. 注册 Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. 注册 HTTP 客户端和上下文访问器
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// 5. 注册业务服务
builder.Services.AddScoped<BrevoEmailService>();
builder.Services.AddScoped<SvgCaptchaService>();
builder.Services.AddScoped<RateLimitService>();

// 6. 注册 SignalR（弹幕）
builder.Services.AddSignalR();

Console.WriteLine("✅ 服务注册完成，开始构建应用...");

var app = builder.Build();

Console.WriteLine("✅ 应用构建完成，开始创建数据库...");

// 7. 自动创建数据库
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
    Console.WriteLine("✅ 数据库创建/检查完成");
}

Console.WriteLine("✅ 数据库准备完成，开始配置管道...");

// 8. 配置 HTTP 管道
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

// 9. 配置路由
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<MessageHub>("/messageHub");

Console.WriteLine("✅ 所有配置完成，应用即将启动...");

// 10. 启动应用
app.Run();
