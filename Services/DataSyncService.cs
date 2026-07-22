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
        private readonly TursoService _tursoService;
        private readonly bool _tursoAvailable;

        public DataSyncService(AppDbContext localContext, TursoService tursoService)
        {
            _localContext = localContext;
            _tursoService = tursoService;
            var url = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL") ?? "";
            var token = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN") ?? "";
            _tursoAvailable = !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(token);
        }

        // ============================================================
        // 用户相关
        // ============================================================

        public async Task AddUserAsync(User user)
        {
            _localContext.Users.Add(user);
            await _localContext.SaveChangesAsync();
            _ = Task.Run(() => SyncUserToTursoAsync(user));
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _localContext.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _localContext.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _localContext.Users.FindAsync(id);
        }

        public async Task UpdateUserAsync(User user)
        {
            _localContext.Users.Update(user);
            await _localContext.SaveChangesAsync();
            _ = Task.Run(() => SyncUserToTursoAsync(user));
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _localContext.Users.ToListAsync();
        }

        // ============================================================
        // 博客相关
        // ============================================================

        public async Task AddBlogAsync(Blog blog)
        {
            _localContext.Blogs.Add(blog);
            await _localContext.SaveChangesAsync();
            _ = Task.Run(() => SyncBlogToTursoAsync(blog));
        }

        public async Task<List<Blog>> GetBlogsAsync()
        {
            return await _localContext.Blogs
                .OrderByDescending(b => b.PublishDate)
                .ToListAsync();
        }

        public async Task<Blog?> GetBlogByIdAsync(int id)
        {
            return await _localContext.Blogs.FindAsync(id);
        }

        public async Task UpdateBlogAsync(Blog blog)
        {
            _localContext.Blogs.Update(blog);
            await _localContext.SaveChangesAsync();
            _ = Task.Run(() => SyncBlogToTursoAsync(blog));
        }

        public async Task DeleteBlogAsync(int id)
        {
            var blog = await _localContext.Blogs.FindAsync(id);
            if (blog != null)
            {
                _localContext.Blogs.Remove(blog);
                await _localContext.SaveChangesAsync();
            }
            _ = Task.Run(() => _tursoService.ExecuteSqlAsync($"DELETE FROM Blogs WHERE Id = {id}"));
        }

        // ============================================================
        // 留言相关
        // ============================================================

        public async Task AddMessageAsync(Message message)
        {
            _localContext.Messages.Add(message);
            await _localContext.SaveChangesAsync();
            _ = Task.Run(() => SyncMessageToTursoAsync(message));
        }

        public async Task<List<Message>> GetMessagesAsync()
        {
            return await _localContext.Messages
                .OrderByDescending(m => m.CreateTime)
                .ToListAsync();
        }

        public async Task<Message?> GetMessageByIdAsync(int id)
        {
            return await _localContext.Messages.FindAsync(id);
        }

        public async Task UpdateMessageAsync(Message message)
        {
            _localContext.Messages.Update(message);
            await _localContext.SaveChangesAsync();
            _ = Task.Run(() => SyncMessageToTursoAsync(message));
        }

        public async Task DeleteMessageAsync(int id)
        {
            var msg = await _localContext.Messages.FindAsync(id);
            if (msg != null)
            {
                _localContext.Messages.Remove(msg);
                await _localContext.SaveChangesAsync();
            }
            _ = Task.Run(() => _tursoService.ExecuteSqlAsync($"DELETE FROM Messages WHERE Id = {id}"));
        }

        public async Task SaveChangesAsync()
        {
            await _localContext.SaveChangesAsync();
        }

        // ============================================================
        // ContactRequest 相关
        // ============================================================

        public async Task<List<ContactRequest>> GetContactRequestsAsync()
        {
            return await _localContext.ContactRequests
                .OrderByDescending(r => r.RequestTime)
                .ToListAsync();
        }

        public async Task<ContactRequest?> GetContactRequestByIdAsync(int id)
        {
            return await _localContext.ContactRequests.FindAsync(id);
        }

        public async Task UpdateContactRequestAsync(ContactRequest request)
        {
            _localContext.ContactRequests.Update(request);
            await _localContext.SaveChangesAsync();
            _ = Task.Run(() => SyncContactRequestToTursoAsync(request));
        }

        public async Task AddContactRequestAsync(ContactRequest request)
        {
            _localContext.ContactRequests.Add(request);
            await _localContext.SaveChangesAsync();
            _ = Task.Run(() => SyncContactRequestToTursoAsync(request));
        }

        // ============================================================
        // AboutMe 相关
        // ============================================================

        public async Task<List<AboutMe>> GetAboutMeAsync()
        {
            return await _localContext.AboutMeContents
                .OrderBy(s => s.SortOrder)
                .ToListAsync();
        }

        public async Task UpdateAboutMeAsync(AboutMe section)
        {
            _localContext.AboutMeContents.Update(section);
            await _localContext.SaveChangesAsync();
            _ = Task.Run(() => SyncAboutMeToTursoAsync(section));
        }

        // ============================================================
        // 管理员账号检查
        // ============================================================

        public async Task EnsureAdminExistsAsync()
        {
            var admin = await _localContext.Users
                .FirstOrDefaultAsync(u => u.Username == "admin");

            if (admin == null)
            {
                admin = new User
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
                _ = Task.Run(() => SyncUserToTursoAsync(admin));
                Console.WriteLine("✅ 管理员账号已创建");
            }
            else
            {
                Console.WriteLine("✅ 管理员账号已存在");
            }
        }

        public async Task<List<User>> GetAllUsersWithFallbackAsync()
        {
            return await _localContext.Users
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        // ============================================================
        // Turso 同步方法（后台静默执行，不阻塞主线程）
        // ============================================================

        private async Task SyncUserToTursoAsync(User user)
        {
            if (!_tursoAvailable) return;
            try
            {
                var sql = $@"INSERT OR REPLACE INTO Users (
                    Id, Username, Email, PasswordHash, IsEmailVerified, IsAdmin,
                    CreatedAt, LastLoginAt, IsBanned, BanExpiry, BanReason,
                    IsDeleted, DeletedAt, AvatarUrl, IsAvatarApproved,
                    PendingEmail, PendingUsername, IsEmailChangeApproved, IsUsernameChangeApproved
                ) VALUES (
                    {user.Id}, '{EscapeSql(user.Username)}', '{EscapeSql(user.Email)}',
                    '{EscapeSql(user.PasswordHash)}', {(user.IsEmailVerified ? 1 : 0)},
                    {(user.IsAdmin ? 1 : 0)}, '{user.CreatedAt:yyyy-MM-dd HH:mm:ss}',
                    {(user.LastLoginAt.HasValue ? $"'{user.LastLoginAt.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                    {(user.IsBanned ? 1 : 0)},
                    {(user.BanExpiry.HasValue ? $"'{user.BanExpiry.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                    {(string.IsNullOrEmpty(user.BanReason) ? "NULL" : $"'{EscapeSql(user.BanReason)}'")},
                    {(user.IsDeleted ? 1 : 0)},
                    {(user.DeletedAt.HasValue ? $"'{user.DeletedAt.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                    {(string.IsNullOrEmpty(user.AvatarUrl) ? "NULL" : $"'{EscapeSql(user.AvatarUrl)}'")},
                    {(user.IsAvatarApproved ? 1 : 0)},
                    {(string.IsNullOrEmpty(user.PendingEmail) ? "NULL" : $"'{EscapeSql(user.PendingEmail)}'")},
                    {(string.IsNullOrEmpty(user.PendingUsername) ? "NULL" : $"'{EscapeSql(user.PendingUsername)}'")},
                    {(user.IsEmailChangeApproved ? 1 : 0)},
                    {(user.IsUsernameChangeApproved ? 1 : 0)}
                )";
                await _tursoService.ExecuteSqlAsync(sql);
                Console.WriteLine($"✅ 用户 {user.Username} 已同步到 Turso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Turso 同步失败: {ex.Message}");
            }
        }

        private async Task SyncBlogToTursoAsync(Blog blog)
        {
            if (!_tursoAvailable) return;
            try
            {
                var sql = $@"INSERT OR REPLACE INTO Blogs (
                    Id, Title, Content, Summary, PublishDate, CoverImageUrl, LikeCount
                ) VALUES (
                    {blog.Id}, '{EscapeSql(blog.Title)}', '{EscapeSql(blog.Content)}',
                    '{EscapeSql(blog.Summary)}', '{blog.PublishDate:yyyy-MM-dd HH:mm:ss}',
                    {(string.IsNullOrEmpty(blog.CoverImageUrl) ? "NULL" : $"'{EscapeSql(blog.CoverImageUrl)}'")},
                    {blog.LikeCount}
                )";
                await _tursoService.ExecuteSqlAsync(sql);
                Console.WriteLine($"✅ 博客 {blog.Title} 已同步到 Turso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Turso 同步失败: {ex.Message}");
            }
        }

        private async Task SyncMessageToTursoAsync(Message message)
        {
            if (!_tursoAvailable) return;
            try
            {
                var sql = $@"INSERT OR REPLACE INTO Messages (
                    Id, UserId, VisitorName, Email, Content, CreateTime,
                    IsApproved, LikeCount, AdminReply, AdminReplyTime, ReportCount, IsReported
                ) VALUES (
                    {message.Id}, {message.UserId}, '{EscapeSql(message.VisitorName)}',
                    '{EscapeSql(message.Email)}', '{EscapeSql(message.Content)}',
                    '{message.CreateTime:yyyy-MM-dd HH:mm:ss}',
                    {(message.IsApproved ? 1 : 0)}, {message.LikeCount},
                    {(string.IsNullOrEmpty(message.AdminReply) ? "NULL" : $"'{EscapeSql(message.AdminReply)}'")},
                    {(message.AdminReplyTime.HasValue ? $"'{message.AdminReplyTime.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                    {message.ReportCount}, {(message.IsReported ? 1 : 0)}
                )";
                await _tursoService.ExecuteSqlAsync(sql);
                Console.WriteLine($"✅ 留言已同步到 Turso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Turso 同步失败: {ex.Message}");
            }
        }

        private async Task SyncContactRequestToTursoAsync(ContactRequest request)
        {
            if (!_tursoAvailable) return;
            try
            {
                var sql = $@"INSERT OR REPLACE INTO ContactRequests (
                    Id, Platform, AuthorizationCode, HowKnowMe, Identity,
                    Relationship, Remarks, UserId, Username, UserEmail,
                    RequestTime, IsApproved, ViewTime, IsUsed, UsedTime, UsedBy
                ) VALUES (
                    {request.Id}, '{EscapeSql(request.Platform)}',
                    '{EscapeSql(request.AuthorizationCode)}',
                    '{EscapeSql(request.HowKnowMe)}', '{EscapeSql(request.Identity)}',
                    '{EscapeSql(request.Relationship)}', '{EscapeSql(request.Remarks)}',
                    {request.UserId}, '{EscapeSql(request.Username)}',
                    '{EscapeSql(request.UserEmail)}',
                    '{request.RequestTime:yyyy-MM-dd HH:mm:ss}',
                    {(request.IsApproved ? 1 : 0)},
                    {(request.ViewTime.HasValue ? $"'{request.ViewTime.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                    {(request.IsUsed ? 1 : 0)},
                    {(request.UsedTime.HasValue ? $"'{request.UsedTime.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                    {(string.IsNullOrEmpty(request.UsedBy) ? "NULL" : $"'{EscapeSql(request.UsedBy)}'")}
                )";
                await _tursoService.ExecuteSqlAsync(sql);
                Console.WriteLine($"✅ 授权码已同步到 Turso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Turso 同步失败: {ex.Message}");
            }
        }

        private async Task SyncAboutMeToTursoAsync(AboutMe section)
        {
            if (!_tursoAvailable) return;
            try
            {
                var sql = $@"INSERT OR REPLACE INTO AboutMeContents (
                    Id, SectionKey, Title, Content, Icon, SortOrder, UpdatedAt
                ) VALUES (
                    {section.Id}, '{EscapeSql(section.SectionKey)}',
                    '{EscapeSql(section.Title)}', '{EscapeSql(section.Content)}',
                    {(string.IsNullOrEmpty(section.Icon) ? "NULL" : $"'{EscapeSql(section.Icon)}'")},
                    {section.SortOrder}, '{section.UpdatedAt:yyyy-MM-dd HH:mm:ss}'
                )";
                await _tursoService.ExecuteSqlAsync(sql);
                Console.WriteLine($"✅ AboutMe 已同步到 Turso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Turso 同步失败: {ex.Message}");
            }
        }

        private string EscapeSql(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("'", "''");
        }
    }
}
