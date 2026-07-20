var app = builder.Build();

// ⭐ 强制 HTTPS（放在最前面）
app.Use((context, next) =>
{
    if (context.Request.Headers.ContainsKey("X-Forwarded-Proto") &&
        context.Request.Headers["X-Forwarded-Proto"] == "http")
    {
        var httpsUrl = "https://" + context.Request.Host + context.Request.Path;
        context.Response.Redirect(httpsUrl, true);
        return Task.CompletedTask;
    }
    return next();
});

// 自动创建数据库
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();  // 这行现在会被正确执行
// ... 其余代码
