using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Services
{
    public class DataSyncService
    {
        private readonly AppDbContext _localContext;
        private readonly TursoDbContext _tursoContext;

        public DataSyncService(AppDbContext localContext, TursoDbContext tursoContext)
        {
            _localContext = localContext;
            _tursoContext = tursoContext;
        }

        // ============================================================
        // 用户相关
        // ============================================================

        public async Task AddUserAsync(User user)
        {
            _localContext.Users.Add(user);
            await _localContext.SaveChangesAsync();

            try
            {
                _tursoContext.Users.Add(user);
                await _tursoContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Turso 同步失败: {ex.Message}");
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _tursoContext.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (user != null) return user;
            }
            catch { }

            return await _localContext.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                var user = await _tursoContext.Users
                    .FirstOrDefaultAsync(u => u.Username == username);
                if (user != null) return user;
            }
            catch { }

            return await _localContext.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }
        public async Task<User?> GetUserByIdAsync(int id)
{
    try
    {
        var user = await _tursoContext.Users.FindAsync(id);
        if (user != null) return user;
    }
    catch { }

    return await _localContext.Users.FindAsync(id);
}

public async Task UpdateUserAsync(User user)
{
    _localContext.Users.Update(user);
    await _localContext.SaveChangesAsync();

    try
    {
        _tursoContext.Users.Update(user);
        await _tursoContext.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Turso 同步失败: {ex.Message}");
    }
}

public async Task<List<User>> GetAllUsersAsync()
{
    try
    {
        return await _tursoContext.Users.ToListAsync();
    }
    catch
    {
        return await _localContext.Users.ToListAsync();
    }
}
        // ============================================================
// 博客相关
// ============================================================

public async Task AddBlogAsync(Blog blog)
{
    _localContext.Blogs.Add(blog);
    await _localContext.SaveChangesAsync();

    try
    {
        _tursoContext.Blogs.Add(blog);
        await _tursoContext.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Turso 同步失败: {ex.Message}");
    }
}

public async Task<List<Blog>> GetBlogsAsync()
{
    try
    {
        return await _tursoContext.Blogs
            .OrderByDescending(b => b.PublishDate)
            .ToListAsync();
    }
    catch
    {
        return await _localContext.Blogs
            .OrderByDescending(b => b.PublishDate)
            .ToListAsync();
    }
}

public async Task<Blog?> GetBlogByIdAsync(int id)
{
    try
    {
        var blog = await _tursoContext.Blogs.FindAsync(id);
        if (blog != null) return blog;
    }
    catch { }

    return await _localContext.Blogs.FindAsync(id);
}

public async Task UpdateBlogAsync(Blog blog)
{
    _localContext.Blogs.Update(blog);
    await _localContext.SaveChangesAsync();

    try
    {
        _tursoContext.Blogs.Update(blog);
        await _tursoContext.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Turso 同步失败: {ex.Message}");
    }
}

public async Task DeleteBlogAsync(int id)
{
    var blog = await _localContext.Blogs.FindAsync(id);
    if (blog != null)
    {
        _localContext.Blogs.Remove(blog);
        await _localContext.SaveChangesAsync();
    }

    try
    {
        var tursoBlog = await _tursoContext.Blogs.FindAsync(id);
        if (tursoBlog != null)
        {
            _tursoContext.Blogs.Remove(tursoBlog);
            await _tursoContext.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Turso 同步失败: {ex.Message}");
    }
}
        // ============================================================
// 留言相关
// ============================================================

public async Task AddMessageAsync(Message message)
{
    _localContext.Messages.Add(message);
    await _localContext.SaveChangesAsync();

    try
    {
        _tursoContext.Messages.Add(message);
        await _tursoContext.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Turso 同步失败: {ex.Message}");
    }
}

public async Task<List<Message>> GetMessagesAsync()
{
    try
    {
        return await _tursoContext.Messages
            .OrderByDescending(m => m.CreateTime)
            .ToListAsync();
    }
    catch
    {
        return await _localContext.Messages
            .OrderByDescending(m => m.CreateTime)
            .ToListAsync();
    }
}

public async Task<Message?> GetMessageByIdAsync(int id)
{
    try
    {
        var msg = await _tursoContext.Messages.FindAsync(id);
        if (msg != null) return msg;
    }
    catch { }

    return await _localContext.Messages.FindAsync(id);
}

public async Task UpdateMessageAsync(Message message)
{
    _localContext.Messages.Update(message);
    await _localContext.SaveChangesAsync();

    try
    {
        _tursoContext.Messages.Update(message);
        await _tursoContext.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Turso 同步失败: {ex.Message}");
    }
}

public async Task DeleteMessageAsync(int id)
{
    var msg = await _localContext.Messages.FindAsync(id);
    if (msg != null)
    {
        _localContext.Messages.Remove(msg);
        await _localContext.SaveChangesAsync();
    }

    try
    {
        var tursoMsg = await _tursoContext.Messages.FindAsync(id);
        if (tursoMsg != null)
        {
            _tursoContext.Messages.Remove(tursoMsg);
            await _tursoContext.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Turso 同步失败: {ex.Message}");
    }
}

public async Task SaveChangesAsync()
{
    await _localContext.SaveChangesAsync();

    try
    {
        await _tursoContext.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Turso 同步失败: {ex.Message}");
    }
}
                // ============================================================
        // 管理员账号检查
        // ============================================================

        public async Task EnsureAdminExistsAsync()
        {
            var localAdmin = await _localContext.Users
                .FirstOrDefaultAsync(u => u.Username == "admin");

            if (localAdmin == null)
            {
                var admin = new User
                {
                    Username = "admin",
                    Email = "2908685235@qq.com",
                    PasswordHash = "AQAAAAIAAYagAAAAEJ4Zj6zVqZMjSx5k5r5WYg==",
                    IsEmailVerified = true,
                    IsAdmin = true,
                    IsBanned = false,
                    CreatedAt = DateTime.Now
                };
                _localContext.Users.Add(admin);
                await _localContext.SaveChangesAsync();

                try
                {
                    _tursoContext.Users.Add(admin);
                    await _tursoContext.SaveChangesAsync();
                    Console.WriteLine("✅ 管理员账号已同步到 Turso");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 管理员同步失败: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    var tursoAdmin = await _tursoContext.Users
                        .FirstOrDefaultAsync(u => u.Username == "admin");
                    if (tursoAdmin == null)
                    {
                        _tursoContext.Users.Add(localAdmin);
                        await _tursoContext.SaveChangesAsync();
                        Console.WriteLine("✅ 管理员账号已补录到 Turso");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 管理员检查失败: {ex.Message}");
                }
            }
        }

        // ============================================================
        // 获取所有用户（带本地备用）
        // ============================================================

        public async Task<List<User>> GetAllUsersWithFallbackAsync()
        {
            try
            {
                return await _tursoContext.Users
                    .Where(u => !u.IsDeleted)
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();
            }
            catch
            {
                return await _localContext.Users
                    .Where(u => !u.IsDeleted)
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();
            }
        }
    }
}
        
