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
        private readonly bool _tursoAvailable;

        public DataSyncService(AppDbContext localContext, TursoDbContext tursoContext)
        {
            _localContext = localContext;
            _tursoContext = tursoContext;
            _tursoAvailable = tursoContext.Database.CanConnect();
        }

        // ============================================================
        // 用户相关 - 优先 Turso，备用本地
        // ============================================================

        public async Task AddUserAsync(User user)
        {
            if (_tursoAvailable)
            {
                _tursoContext.Users.Add(user);
                await _tursoContext.SaveChangesAsync();
                Console.WriteLine($"✅ 用户 {user.Username} 已保存到 Turso");
            }
            
            // 同时保存到本地（备份）
            _localContext.Users.Add(user);
            await _localContext.SaveChangesAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            // 优先从 Turso 读取
            try
            {
                if (_tursoAvailable)
                {
                    var user = await _tursoContext.Users
                        .FirstOrDefaultAsync(u => u.Email == email);
                    if (user != null) return user;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Turso 读取失败: {ex.Message}");
            }

            // 降级到本地
            return await _localContext.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                if (_tursoAvailable)
                {
                    var user = await _tursoContext.Users
                        .FirstOrDefaultAsync(u => u.Username == username);
                    if (user != null) return user;
                }
            }
            catch { }

            return await _localContext.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            try
            {
                if (_tursoAvailable)
                {
                    var user = await _tursoContext.Users.FindAsync(id);
                    if (user != null) return user;
                }
            }
            catch { }

            return await _localContext.Users.FindAsync(id);
        }

        public async Task UpdateUserAsync(User user)
        {
            if (_tursoAvailable)
            {
                _tursoContext.Users.Update(user);
                await _tursoContext.SaveChangesAsync();
            }
            
            _localContext.Users.Update(user);
            await _localContext.SaveChangesAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                if (_tursoAvailable)
                {
                    return await _tursoContext.Users.ToListAsync();
                }
            }
            catch { }

            return await _localContext.Users.ToListAsync();
        }

        // ============================================================
        // 博客相关 - 优先 Turso
        // ============================================================

        public async Task AddBlogAsync(Blog blog)
        {
            if (_tursoAvailable)
            {
                _tursoContext.Blogs.Add(blog);
                await _tursoContext.SaveChangesAsync();
            }
            
            _localContext.Blogs.Add(blog);
            await _localContext.SaveChangesAsync();
        }

        public async Task<List<Blog>> GetBlogsAsync()
        {
            try
            {
                if (_tursoAvailable)
                {
                    return await _tursoContext.Blogs
                        .OrderByDescending(b => b.PublishDate)
                        .ToListAsync();
                }
            }
            catch { }

            return await _localContext.Blogs
                .OrderByDescending(b => b.PublishDate)
                .ToListAsync();
        }

        public async Task<Blog?> GetBlogByIdAsync(int id)
        {
            try
            {
                if (_tursoAvailable)
                {
                    var blog = await _tursoContext.Blogs.FindAsync(id);
                    if (blog != null) return blog;
                }
            }
            catch { }

            return await _localContext.Blogs.FindAsync(id);
        }

        public async Task UpdateBlogAsync(Blog blog)
        {
            if (_tursoAvailable)
            {
                _tursoContext.Blogs.Update(blog);
                await _tursoContext.SaveChangesAsync();
            }
            
            _localContext.Blogs.Update(blog);
            await _localContext.SaveChangesAsync();
        }

        public async Task DeleteBlogAsync(int id)
        {
            if (_tursoAvailable)
            {
                var tursoBlog = await _tursoContext.Blogs.FindAsync(id);
                if (tursoBlog != null)
                {
                    _tursoContext.Blogs.Remove(tursoBlog);
                    await _tursoContext.SaveChangesAsync();
                }
            }

            var localBlog = await _localContext.Blogs.FindAsync(id);
            if (localBlog != null)
            {
                _localContext.Blogs.Remove(localBlog);
                await _localContext.SaveChangesAsync();
            }
        }

        // ============================================================
        // 留言相关 - 优先 Turso
        // ============================================================

        public async Task AddMessageAsync(Message message)
        {
            if (_tursoAvailable)
            {
                _tursoContext.Messages.Add(message);
                await _tursoContext.SaveChangesAsync();
            }
            
            _localContext.Messages.Add(message);
            await _localContext.SaveChangesAsync();
        }

        public async Task<List<Message>> GetMessagesAsync()
        {
            try
            {
                if (_tursoAvailable)
                {
                    return await _tursoContext.Messages
                        .OrderByDescending(m => m.CreateTime)
                        .ToListAsync();
                }
            }
            catch { }

            return await _localContext.Messages
                .OrderByDescending(m => m.CreateTime)
                .ToListAsync();
        }

        public async Task<Message?> GetMessageByIdAsync(int id)
        {
            try
            {
                if (_tursoAvailable)
                {
                    var msg = await _tursoContext.Messages.FindAsync(id);
                    if (msg != null) return msg;
                }
            }
            catch { }

            return await _localContext.Messages.FindAsync(id);
        }

        public async Task UpdateMessageAsync(Message message)
        {
            if (_tursoAvailable)
            {
                _tursoContext.Messages.Update(message);
                await _tursoContext.SaveChangesAsync();
            }
            
            _localContext.Messages.Update(message);
            await _localContext.SaveChangesAsync();
        }

        public async Task DeleteMessageAsync(int id)
        {
            if (_tursoAvailable)
            {
                var tursoMsg = await _tursoContext.Messages.FindAsync(id);
                if (tursoMsg != null)
                {
                    _tursoContext.Messages.Remove(tursoMsg);
                    await _tursoContext.SaveChangesAsync();
                }
            }

            var localMsg = await _localContext.Messages.FindAsync(id);
            if (localMsg != null)
            {
                _localContext.Messages.Remove(localMsg);
                await _localContext.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            if (_tursoAvailable)
            {
                await _tursoContext.SaveChangesAsync();
            }
            await _localContext.SaveChangesAsync();
        }

        // ============================================================
        // ContactRequest 相关
        // ============================================================

        public async Task<List<ContactRequest>> GetContactRequestsAsync()
        {
            try
            {
                if (_tursoAvailable)
                {
                    return await _tursoContext.ContactRequests
                        .OrderByDescending(r => r.RequestTime)
                        .ToListAsync();
                }
            }
            catch { }

            return await _localContext.ContactRequests
                .OrderByDescending(r => r.RequestTime)
                .ToListAsync();
        }

        public async Task<ContactRequest?> GetContactRequestByIdAsync(int id)
        {
            try
            {
                if (_tursoAvailable)
                {
                    var request = await _tursoContext.ContactRequests.FindAsync(id);
                    if (request != null) return request;
                }
            }
            catch { }

            return await _localContext.ContactRequests.FindAsync(id);
        }

        public async Task UpdateContactRequestAsync(ContactRequest request)
        {
            if (_tursoAvailable)
            {
                _tursoContext.ContactRequests.Update(request);
                await _tursoContext.SaveChangesAsync();
            }
            
            _localContext.ContactRequests.Update(request);
            await _localContext.SaveChangesAsync();
        }

        public async Task AddContactRequestAsync(ContactRequest request)
        {
            if (_tursoAvailable)
            {
                _tursoContext.ContactRequests.Add(request);
                await _tursoContext.SaveChangesAsync();
            }
            
            _localContext.ContactRequests.Add(request);
            await _localContext.SaveChangesAsync();
        }

        // ============================================================
        // AboutMe 相关
        // ============================================================

        public async Task<List<AboutMe>> GetAboutMeAsync()
        {
            try
            {
                if (_tursoAvailable)
                {
                    return await _tursoContext.AboutMeContents
                        .OrderBy(s => s.SortOrder)
                        .ToListAsync();
                }
            }
            catch { }

            return await _localContext.AboutMeContents
                .OrderBy(s => s.SortOrder)
                .ToListAsync();
        }

        public async Task UpdateAboutMeAsync(AboutMe section)
        {
            if (_tursoAvailable)
            {
                _tursoContext.AboutMeContents.Update(section);
                await _tursoContext.SaveChangesAsync();
            }
            
            _localContext.AboutMeContents.Update(section);
            await _localContext.SaveChangesAsync();
        }

        // ============================================================
        // 管理员账号检查
        // ============================================================

        public async Task EnsureAdminExistsAsync()
        {
            User? admin = null;

            // 先检查 Turso
            try
            {
                if (_tursoAvailable)
                {
                    admin = await _tursoContext.Users
                        .FirstOrDefaultAsync(u => u.Username == "admin");
                }
            }
            catch { }

            // 如果 Turso 没有，检查本地
            if (admin == null)
            {
                admin = await _localContext.Users
                    .FirstOrDefaultAsync(u => u.Username == "admin");
            }

            if (admin == null)
            {
                var newAdmin = new User
                {
                    Username = "admin",
                    Email = "2908685235@qq.com",
                    PasswordHash = "AQAAAAIAAYagAAAAEJ4Zj6zVqZMjSx5k5r5WYg==",
                    IsEmailVerified = true,
                    IsAdmin = true,
                    IsBanned = false,
                    CreatedAt = DateTime.Now
                };

                if (_tursoAvailable)
                {
                    _tursoContext.Users.Add(newAdmin);
                    await _tursoContext.SaveChangesAsync();
                    Console.WriteLine("✅ 管理员账号已创建到 Turso");
                }

                _localContext.Users.Add(newAdmin);
                await _localContext.SaveChangesAsync();
                Console.WriteLine("✅ 管理员账号已创建到本地");
            }
            else
            {
                Console.WriteLine("✅ 管理员账号已存在");
            }
        }

        // ============================================================
        // 获取所有用户（带本地备用）
        // ============================================================

        public async Task<List<User>> GetAllUsersWithFallbackAsync()
        {
            try
            {
                if (_tursoAvailable)
                {
                    return await _tursoContext.Users
                        .Where(u => !u.IsDeleted)
                        .OrderByDescending(u => u.CreatedAt)
                        .ToListAsync();
                }
            }
            catch { }

            return await _localContext.Users
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }
    }
}
