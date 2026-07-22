using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

            if (_tursoAvailable)
                Console.WriteLine("✅ Turso 已连接（双写模式：Turso + 本地 SQLite）");
            else
                Console.WriteLine("⚠️ Turso 未配置，仅使用本地 SQLite");
        }

        // ============================================================
        // 用户相关（双写 + 优先 Turso）
        // ============================================================

        public async Task AddUserAsync(User user)
        {
            bool tursoSuccess = false;
            if (_tursoAvailable)
            {
                tursoSuccess = await SyncUserToTursoAsync(user);
                if (tursoSuccess)
                    Console.WriteLine($"✅ 用户 {user.Username} 已写入 Turso");
                else
                    Console.WriteLine($"⚠️ 用户 {user.Username} Turso 写入失败");
            }

            _localContext.Users.Add(user);
            await _localContext.SaveChangesAsync();
            Console.WriteLine($"✅ 用户 {user.Username} 已写入本地 SQLite");
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync(
                        $"SELECT * FROM Users WHERE Email = '{EscapeSql(email)}'"
                    );
                    var user = ParseUserFromJson(result);
                    if (user != null)
                    {
                        Console.WriteLine($"✅ 从 Turso 读取用户: {email}");
                        return user;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 读取失败: {ex.Message}");
                }
            }

            Console.WriteLine($"📂 从本地 SQLite 读取用户: {email}");
            return await _localContext.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync(
                        $"SELECT * FROM Users WHERE Username = '{EscapeSql(username)}'"
                    );
                    var user = ParseUserFromJson(result);
                    if (user != null)
                    {
                        Console.WriteLine($"✅ 从 Turso 读取用户: {username}");
                        return user;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 读取失败: {ex.Message}");
                }
            }

            Console.WriteLine($"📂 从本地 SQLite 读取用户: {username}");
            return await _localContext.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync(
                        $"SELECT * FROM Users WHERE Id = {id}"
                    );
                    var user = ParseUserFromJson(result);
                    if (user != null)
                    {
                        Console.WriteLine($"✅ 从 Turso 读取用户 ID: {id}");
                        return user;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 读取失败: {ex.Message}");
                }
            }

            return await _localContext.Users.FindAsync(id);
        }

        public async Task UpdateUserAsync(User user)
        {
            bool tursoSuccess = false;
            if (_tursoAvailable)
            {
                tursoSuccess = await SyncUserToTursoAsync(user);
                if (tursoSuccess)
                    Console.WriteLine($"✅ 用户 {user.Username} 已更新到 Turso");
                else
                    Console.WriteLine($"⚠️ 用户 {user.Username} Turso 更新失败");
            }

            _localContext.Users.Update(user);
            await _localContext.SaveChangesAsync();
            Console.WriteLine($"✅ 用户 {user.Username} 已更新到本地 SQLite");
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync("SELECT * FROM Users");
                    var users = ParseUserListFromJson(result);
                    if (users != null && users.Any())
                    {
                        Console.WriteLine($"✅ 从 Turso 读取 {users.Count} 个用户");
                        return users;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 读取失败: {ex.Message}");
                }
            }

            var localUsers = await _localContext.Users.ToListAsync();
            Console.WriteLine($"📂 从本地 SQLite 读取 {localUsers.Count} 个用户");
            return localUsers;
        }

        // ============================================================
        // 博客相关（双写 + 优先 Turso）
        // ============================================================

        public async Task AddBlogAsync(Blog blog)
        {
            if (_tursoAvailable)
            {
                var success = await SyncBlogToTursoAsync(blog);
                if (success)
                    Console.WriteLine($"✅ 博客 {blog.Title} 已写入 Turso");
                else
                    Console.WriteLine($"⚠️ 博客 {blog.Title} Turso 写入失败");
            }

            _localContext.Blogs.Add(blog);
            await _localContext.SaveChangesAsync();
            Console.WriteLine($"✅ 博客 {blog.Title} 已写入本地 SQLite");
        }

        public async Task<List<Blog>> GetBlogsAsync()
        {
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync(
                        "SELECT * FROM Blogs ORDER BY PublishDate DESC"
                    );
                    var blogs = ParseBlogListFromJson(result);
                    if (blogs != null && blogs.Any())
                    {
                        Console.WriteLine($"✅ 从 Turso 读取 {blogs.Count} 篇博客");
                        return blogs;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 读取博客失败: {ex.Message}");
                }
            }

            var localBlogs = await _localContext.Blogs
                .OrderByDescending(b => b.PublishDate)
                .ToListAsync();
            Console.WriteLine($"📂 从本地 SQLite 读取 {localBlogs.Count} 篇博客");
            return localBlogs;
        }

        public async Task<Blog?> GetBlogByIdAsync(int id)
        {
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync(
                        $"SELECT * FROM Blogs WHERE Id = {id}"
                    );
                    var blog = ParseBlogFromJson(result);
                    if (blog != null)
                    {
                        Console.WriteLine($"✅ 从 Turso 读取博客 ID: {id}");
                        return blog;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 读取博客失败: {ex.Message}");
                }
            }

            return await _localContext.Blogs.FindAsync(id);
        }

        public async Task UpdateBlogAsync(Blog blog)
        {
            if (_tursoAvailable)
            {
                var success = await SyncBlogToTursoAsync(blog);
                if (success)
                    Console.WriteLine($"✅ 博客 {blog.Title} 已更新到 Turso");
                else
                    Console.WriteLine($"⚠️ 博客 {blog.Title} Turso 更新失败");
            }

            _localContext.Blogs.Update(blog);
            await _localContext.SaveChangesAsync();
            Console.WriteLine($"✅ 博客 {blog.Title} 已更新到本地 SQLite");
        }

        public async Task DeleteBlogAsync(int id)
        {
            if (_tursoAvailable)
            {
                await _tursoService.ExecuteSqlAsync($"DELETE FROM Blogs WHERE Id = {id}");
                Console.WriteLine($"✅ 博客 {id} 已从 Turso 删除");
            }

            var blog = await _localContext.Blogs.FindAsync(id);
            if (blog != null)
            {
                _localContext.Blogs.Remove(blog);
                await _localContext.SaveChangesAsync();
                Console.WriteLine($"✅ 博客 {id} 已从本地 SQLite 删除");
            }
        }

        // ============================================================
        // 留言相关（双写 + 优先 Turso）
        // ============================================================

        public async Task AddMessageAsync(Message message)
        {
            if (_tursoAvailable)
            {
                var success = await SyncMessageToTursoAsync(message);
                if (success)
                    Console.WriteLine($"✅ 留言已写入 Turso");
                else
                    Console.WriteLine($"⚠️ 留言 Turso 写入失败");
            }

            _localContext.Messages.Add(message);
            await _localContext.SaveChangesAsync();
            Console.WriteLine($"✅ 留言已写入本地 SQLite");
        }

        public async Task<List<Message>> GetMessagesAsync()
        {
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync(
                        "SELECT * FROM Messages ORDER BY CreateTime DESC"
                    );
                    var messages = ParseMessageListFromJson(result);
                    if (messages != null && messages.Any())
                    {
                        Console.WriteLine($"✅ 从 Turso 读取 {messages.Count} 条留言");
                        return messages;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 读取留言失败: {ex.Message}");
                }
            }

            var localMessages = await _localContext.Messages
                .OrderByDescending(m => m.CreateTime)
                .ToListAsync();
            Console.WriteLine($"📂 从本地 SQLite 读取 {localMessages.Count} 条留言");
            return localMessages;
        }

        public async Task<Message?> GetMessageByIdAsync(int id)
        {
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync(
                        $"SELECT * FROM Messages WHERE Id = {id}"
                    );
                    var message = ParseMessageFromJson(result);
                    if (message != null)
                    {
                        Console.WriteLine($"✅ 从 Turso 读取留言 ID: {id}");
                        return message;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 读取留言失败: {ex.Message}");
                }
            }

            return await _localContext.Messages.FindAsync(id);
        }

        public async Task UpdateMessageAsync(Message message)
        {
            if (_tursoAvailable)
            {
                var success = await SyncMessageToTursoAsync(message);
                if (success)
                    Console.WriteLine($"✅ 留言已更新到 Turso");
                else
                    Console.WriteLine($"⚠️ 留言 Turso 更新失败");
            }

            _localContext.Messages.Update(message);
            await _localContext.SaveChangesAsync();
            Console.WriteLine($"✅ 留言已更新到本地 SQLite");
        }

        public async Task DeleteMessageAsync(int id)
        {
            if (_tursoAvailable)
            {
                await _tursoService.ExecuteSqlAsync($"DELETE FROM Messages WHERE Id = {id}");
                Console.WriteLine($"✅ 留言 {id} 已从 Turso 删除");
            }

            var msg = await _localContext.Messages.FindAsync(id);
            if (msg != null)
            {
                _localContext.Messages.Remove(msg);
                await _localContext.SaveChangesAsync();
                Console.WriteLine($"✅ 留言 {id} 已从本地 SQLite 删除");
            }
        }

        public async Task SaveChangesAsync()
        {
            await _localContext.SaveChangesAsync();
        }

        // ============================================================
        // ContactRequest 相关（双写 + 优先 Turso）
        // ============================================================

        public async Task<List<ContactRequest>> GetContactRequestsAsync()
        {
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync(
                        "SELECT * FROM ContactRequests ORDER BY RequestTime DESC"
                    );
                    var requests = ParseContactRequestListFromJson(result);
                    if (requests != null && requests.Any())
                    {
                        Console.WriteLine($"✅ 从 Turso 读取 {requests.Count} 条授权码申请");
                        return requests;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 读取授权码失败: {ex.Message}");
                }
            }

            return await _localContext.ContactRequests
                .OrderByDescending(r => r.RequestTime)
                .ToListAsync();
        }

        public async Task<ContactRequest?> GetContactRequestByIdAsync(int id)
        {
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync(
                        $"SELECT * FROM ContactRequests WHERE Id = {id}"
                    );
                    var request = ParseContactRequestFromJson(result);
                    if (request != null)
                    {
                        Console.WriteLine($"✅ 从 Turso 读取授权码 ID: {id}");
                        return request;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 读取授权码失败: {ex.Message}");
                }
            }

            return await _localContext.ContactRequests.FindAsync(id);
        }

        public async Task UpdateContactRequestAsync(ContactRequest request)
        {
            if (_tursoAvailable)
            {
                var success = await SyncContactRequestToTursoAsync(request);
                if (success)
                    Console.WriteLine($"✅ 授权码已更新到 Turso");
                else
                    Console.WriteLine($"⚠️ 授权码 Turso 更新失败");
            }

            _localContext.ContactRequests.Update(request);
            await _localContext.SaveChangesAsync();
            Console.WriteLine($"✅ 授权码已更新到本地 SQLite");
        }

        public async Task AddContactRequestAsync(ContactRequest request)
        {
            if (_tursoAvailable)
            {
                var success = await SyncContactRequestToTursoAsync(request);
                if (success)
                    Console.WriteLine($"✅ 授权码已写入 Turso");
                else
                    Console.WriteLine($"⚠️ 授权码 Turso 写入失败");
            }

            _localContext.ContactRequests.Add(request);
            await _localContext.SaveChangesAsync();
            Console.WriteLine($"✅ 授权码已写入本地 SQLite");
        }

        // ============================================================
        // AboutMe 相关（双写 + 优先 Turso）
        // ============================================================

        public async Task<List<AboutMe>> GetAboutMeAsync()
        {
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync(
                        "SELECT * FROM AboutMeContents ORDER BY SortOrder"
                    );
                    var sections = ParseAboutMeListFromJson(result);
                    if (sections != null && sections.Any())
                    {
                        Console.WriteLine($"✅ 从 Turso 读取 {sections.Count} 条 AboutMe");
                        return sections;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 读取 AboutMe 失败: {ex.Message}");
                }
            }

            return await _localContext.AboutMeContents
                .OrderBy(s => s.SortOrder)
                .ToListAsync();
        }

        public async Task UpdateAboutMeAsync(AboutMe section)
        {
            if (_tursoAvailable)
            {
                var success = await SyncAboutMeToTursoAsync(section);
                if (success)
                    Console.WriteLine($"✅ AboutMe 已更新到 Turso");
                else
                    Console.WriteLine($"⚠️ AboutMe Turso 更新失败");
            }

            _localContext.AboutMeContents.Update(section);
            await _localContext.SaveChangesAsync();
            Console.WriteLine($"✅ AboutMe 已更新到本地 SQLite");
        }

        // ============================================================
        // 管理员账号检查（不重置密码！）
        // ============================================================

        public async Task EnsureAdminExistsAsync()
        {
            User? admin = null;

            // 1. 先从 Turso 查
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync(
                        "SELECT * FROM Users WHERE Username = 'admin'"
                    );
                    admin = ParseUserFromJson(result);
                    if (admin != null)
                    {
                        Console.WriteLine("✅ 管理员账号已存在于 Turso");
                        // 同步到本地
                        var localAdmin = await _localContext.Users
                            .FirstOrDefaultAsync(u => u.Username == "admin");
                        if (localAdmin == null)
                        {
                            _localContext.Users.Add(admin);
                            await _localContext.SaveChangesAsync();
                            Console.WriteLine("✅ 管理员账号已从 Turso 同步到本地");
                        }
                        return;  // 直接返回，不重置密码
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 查询管理员失败: {ex.Message}");
                }
            }

            // 2. 从本地查
            admin = await _localContext.Users
                .FirstOrDefaultAsync(u => u.Username == "admin");

            if (admin != null)
            {
                Console.WriteLine("✅ 管理员账号已存在于本地 SQLite");
                // 同步到 Turso
                if (_tursoAvailable)
                {
                    await SyncUserToTursoAsync(admin);
                    Console.WriteLine("✅ 管理员账号已从本地同步到 Turso");
                }
                return;  // 直接返回，不重置密码
            }

            // 3. 都没有才创建（只有第一次部署会执行）
            Console.WriteLine("📝 首次部署，创建管理员账号...");
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

            if (_tursoAvailable)
            {
                await SyncUserToTursoAsync(admin);
                Console.WriteLine("✅ 管理员账号已创建到 Turso");
            }

            _localContext.Users.Add(admin);
            await _localContext.SaveChangesAsync();
            Console.WriteLine("✅ 管理员账号已创建到本地 SQLite");
        }

        public async Task<List<User>> GetAllUsersWithFallbackAsync()
        {
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync(
                        "SELECT * FROM Users WHERE IsDeleted = 0 ORDER BY CreatedAt DESC"
                    );
                    var users = ParseUserListFromJson(result);
                    if (users != null && users.Any())
                    {
                        Console.WriteLine($"✅ 从 Turso 读取 {users.Count} 个活跃用户");
                        return users;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 读取用户失败: {ex.Message}");
                }
            }

            return await _localContext.Users
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        // ============================================================
        // Turso 同步方法（写入 Turso）
        // ============================================================

        private async Task<bool> SyncUserToTursoAsync(User user)
        {
            if (!_tursoAvailable) return false;
            try
            {
                var sql = $@"INSERT OR REPLACE INTO Users (
                    Id, Username, Email, PasswordHash, IsEmailVerified, IsAdmin,
                    CreatedAt, LastLoginAt, IsBanned, BanExpiry, BanReason,
                    IsDeleted, DeletedAt, DeleteReason, DeleteNote,
                    AvatarUrl, IsAvatarApproved, AvatarSubmittedAt,
                    PendingEmail, PendingUsername, IsEmailChangeApproved, IsUsernameChangeApproved,
                    VerificationCode, VerificationCodeExpiry
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
                    {(string.IsNullOrEmpty(user.DeleteReason) ? "NULL" : $"'{EscapeSql(user.DeleteReason)}'")},
                    {(string.IsNullOrEmpty(user.DeleteNote) ? "NULL" : $"'{EscapeSql(user.DeleteNote)}'")},
                    {(string.IsNullOrEmpty(user.AvatarUrl) ? "NULL" : $"'{EscapeSql(user.AvatarUrl)}'")},
                    {(user.IsAvatarApproved ? 1 : 0)},
                    {(user.AvatarSubmittedAt.HasValue ? $"'{user.AvatarSubmittedAt.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                    {(string.IsNullOrEmpty(user.PendingEmail) ? "NULL" : $"'{EscapeSql(user.PendingEmail)}'")},
                    {(string.IsNullOrEmpty(user.PendingUsername) ? "NULL" : $"'{EscapeSql(user.PendingUsername)}'")},
                    {(user.IsEmailChangeApproved ? 1 : 0)},
                    {(user.IsUsernameChangeApproved ? 1 : 0)},
                    {(string.IsNullOrEmpty(user.VerificationCode) ? "NULL" : $"'{EscapeSql(user.VerificationCode)}'")},
                    {(user.VerificationCodeExpiry.HasValue ? $"'{user.VerificationCodeExpiry.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")}
                )";
                return await _tursoService.ExecuteSqlAsync(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Turso 同步用户失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SyncBlogToTursoAsync(Blog blog)
        {
            if (!_tursoAvailable) return false;
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
                return await _tursoService.ExecuteSqlAsync(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Turso 同步博客失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SyncMessageToTursoAsync(Message message)
        {
            if (!_tursoAvailable) return false;
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
                return await _tursoService.ExecuteSqlAsync(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Turso 同步留言失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SyncContactRequestToTursoAsync(ContactRequest request)
        {
            if (!_tursoAvailable) return false;
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
                return await _tursoService.ExecuteSqlAsync(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Turso 同步授权码失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SyncAboutMeToTursoAsync(AboutMe section)
        {
            if (!_tursoAvailable) return false;
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
                return await _tursoService.ExecuteSqlAsync(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Turso 同步 AboutMe 失败: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        // JSON 解析方法（真正解析 Turso 返回数据）
        // ============================================================

        private User? ParseUserFromJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var result))
                    {
                        if (result.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var row = rows[0];
                            var cols = result.GetProperty("cols");
                            var values = row.GetProperty("values");

                            var user = new User();

                            for (int i = 0; i < cols.GetArrayLength(); i++)
                            {
                                var colName = cols[i].GetProperty("name").GetString();
                                var value = values[i];

                                switch (colName)
                                {
                                    case "Id": user.Id = value.ValueKind == JsonValueKind.Null ? 0 : value.GetInt32(); break;
                                    case "Username": user.Username = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                    case "Email": user.Email = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                    case "PasswordHash": user.PasswordHash = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                    case "IsEmailVerified": user.IsEmailVerified = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                    case "IsAdmin": user.IsAdmin = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                    case "CreatedAt": user.CreatedAt = value.ValueKind == JsonValueKind.Null ? DateTime.Now : DateTime.Parse(value.GetString() ?? DateTime.Now.ToString()); break;
                                    case "LastLoginAt": user.LastLoginAt = value.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(value.GetString()!); break;
                                    case "IsBanned": user.IsBanned = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                    case "BanExpiry": user.BanExpiry = value.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(value.GetString()!); break;
                                    case "BanReason": user.BanReason = value.ValueKind == JsonValueKind.Null ? null : value.GetString(); break;
                                    case "IsDeleted": user.IsDeleted = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                    case "DeletedAt": user.DeletedAt = value.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(value.GetString()!); break;
                                    case "DeleteReason": user.DeleteReason = value.ValueKind == JsonValueKind.Null ? null : value.GetString(); break;
                                    case "DeleteNote": user.DeleteNote = value.ValueKind == JsonValueKind.Null ? null : value.GetString(); break;
                                    case "AvatarUrl": user.AvatarUrl = value.ValueKind == JsonValueKind.Null ? null : value.GetString(); break;
                                    case "IsAvatarApproved": user.IsAvatarApproved = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                    case "AvatarSubmittedAt": user.AvatarSubmittedAt = value.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(value.GetString()!); break;
                                    case "PendingEmail": user.PendingEmail = value.ValueKind == JsonValueKind.Null ? null : value.GetString(); break;
                                    case "PendingUsername": user.PendingUsername = value.ValueKind == JsonValueKind.Null ? null : value.GetString(); break;
                                    case "IsEmailChangeApproved": user.IsEmailChangeApproved = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                    case "IsUsernameChangeApproved": user.IsUsernameChangeApproved = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                    case "VerificationCode": user.VerificationCode = value.ValueKind == JsonValueKind.Null ? null : value.GetString(); break;
                                    case "VerificationCodeExpiry": user.VerificationCodeExpiry = value.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(value.GetString()!); break;
                                }
                            }
                            return user;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 解析用户 JSON 失败: {ex.Message}");
                return null;
            }
        }

        private List<User> ParseUserListFromJson(string json)
        {
            var users = new List<User>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var result))
                    {
                        if (result.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var cols = result.GetProperty("cols");

                            for (int r = 0; r < rows.GetArrayLength(); r++)
                            {
                                var row = rows[r];
                                var values = row.GetProperty("values");
                                var user = new User();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var value = values[i];

                                    switch (colName)
                                    {
                                        case "Id": user.Id = value.ValueKind == JsonValueKind.Null ? 0 : value.GetInt32(); break;
                                        case "Username": user.Username = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "Email": user.Email = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "PasswordHash": user.PasswordHash = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "IsEmailVerified": user.IsEmailVerified = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                        case "IsAdmin": user.IsAdmin = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                        case "CreatedAt": user.CreatedAt = value.ValueKind == JsonValueKind.Null ? DateTime.Now : DateTime.Parse(value.GetString() ?? DateTime.Now.ToString()); break;
                                        case "IsBanned": user.IsBanned = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                        case "IsDeleted": user.IsDeleted = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                        case "AvatarUrl": user.AvatarUrl = value.ValueKind == JsonValueKind.Null ? null : value.GetString(); break;
                                        case "IsAvatarApproved": user.IsAvatarApproved = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                    }
                                }
                                users.Add(user);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 解析用户列表 JSON 失败: {ex.Message}");
            }
            return users;
        }

        private List<Blog> ParseBlogListFromJson(string json)
        {
            var blogs = new List<Blog>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var result))
                    {
                        if (result.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var cols = result.GetProperty("cols");

                            for (int r = 0; r < rows.GetArrayLength(); r++)
                            {
                                var row = rows[r];
                                var values = row.GetProperty("values");
                                var blog = new Blog();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var value = values[i];

                                    switch (colName)
                                    {
                                        case "Id": blog.Id = value.ValueKind == JsonValueKind.Null ? 0 : value.GetInt32(); break;
                                        case "Title": blog.Title = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "Content": blog.Content = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "Summary": blog.Summary = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "PublishDate": blog.PublishDate = value.ValueKind == JsonValueKind.Null ? DateTime.Now : DateTime.Parse(value.GetString() ?? DateTime.Now.ToString()); break;
                                        case "CoverImageUrl": blog.CoverImageUrl = value.ValueKind == JsonValueKind.Null ? null : value.GetString(); break;
                                        case "LikeCount": blog.LikeCount = value.ValueKind == JsonValueKind.Null ? 0 : value.GetInt32(); break;
                                    }
                                }
                                blogs.Add(blog);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 解析博客列表 JSON 失败: {ex.Message}");
            }
            return blogs;
        }

        private Blog? ParseBlogFromJson(string json)
        {
            var blogs = ParseBlogListFromJson(json);
            return blogs.FirstOrDefault();
        }

        private List<Message> ParseMessageListFromJson(string json)
        {
            var messages = new List<Message>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var result))
                    {
                        if (result.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var cols = result.GetProperty("cols");

                            for (int r = 0; r < rows.GetArrayLength(); r++)
                            {
                                var row = rows[r];
                                var values = row.GetProperty("values");
                                var msg = new Message();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var value = values[i];

                                    switch (colName)
                                    {
                                        case "Id": msg.Id = value.ValueKind == JsonValueKind.Null ? 0 : value.GetInt32(); break;
                                        case "UserId": msg.UserId = value.ValueKind == JsonValueKind.Null ? 0 : value.GetInt32(); break;
                                        case "VisitorName": msg.VisitorName = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "Email": msg.Email = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "Content": msg.Content = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "CreateTime": msg.CreateTime = value.ValueKind == JsonValueKind.Null ? DateTime.Now : DateTime.Parse(value.GetString() ?? DateTime.Now.ToString()); break;
                                        case "IsApproved": msg.IsApproved = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                        case "LikeCount": msg.LikeCount = value.ValueKind == JsonValueKind.Null ? 0 : value.GetInt32(); break;
                                        case "AdminReply": msg.AdminReply = value.ValueKind == JsonValueKind.Null ? null : value.GetString(); break;
                                        case "AdminReplyTime": msg.AdminReplyTime = value.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(value.GetString()!); break;
                                        case "ReportCount": msg.ReportCount = value.ValueKind == JsonValueKind.Null ? 0 : value.GetInt32(); break;
                                        case "IsReported": msg.IsReported = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                    }
                                }
                                messages.Add(msg);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 解析留言列表 JSON 失败: {ex.Message}");
            }
            return messages;
        }

        private Message? ParseMessageFromJson(string json)
        {
            var messages = ParseMessageListFromJson(json);
            return messages.FirstOrDefault();
        }

        private List<ContactRequest> ParseContactRequestListFromJson(string json)
        {
            var requests = new List<ContactRequest>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var result))
                    {
                        if (result.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var cols = result.GetProperty("cols");

                            for (int r = 0; r < rows.GetArrayLength(); r++)
                            {
                                var row = rows[r];
                                var values = row.GetProperty("values");
                                var req = new ContactRequest();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var value = values[i];

                                    switch (colName)
                                    {
                                        case "Id": req.Id = value.ValueKind == JsonValueKind.Null ? 0 : value.GetInt32(); break;
                                        case "Platform": req.Platform = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "AuthorizationCode": req.AuthorizationCode = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "HowKnowMe": req.HowKnowMe = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "Identity": req.Identity = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "Relationship": req.Relationship = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "Remarks": req.Remarks = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "UserId": req.UserId = value.ValueKind == JsonValueKind.Null ? 0 : value.GetInt32(); break;
                                        case "Username": req.Username = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "UserEmail": req.UserEmail = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "RequestTime": req.RequestTime = value.ValueKind == JsonValueKind.Null ? DateTime.Now : DateTime.Parse(value.GetString() ?? DateTime.Now.ToString()); break;
                                        case "IsApproved": req.IsApproved = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                        case "IsUsed": req.IsUsed = value.ValueKind == JsonValueKind.Null ? false : value.GetInt32() == 1; break;
                                        case "UsedTime": req.UsedTime = value.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(value.GetString()!); break;
                                        case "UsedBy": req.UsedBy = value.ValueKind == JsonValueKind.Null ? null : value.GetString(); break;
                                    }
                                }
                                requests.Add(req);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 解析授权码列表 JSON 失败: {ex.Message}");
            }
            return requests;
        }

        private ContactRequest? ParseContactRequestFromJson(string json)
        {
            var requests = ParseContactRequestListFromJson(json);
            return requests.FirstOrDefault();
        }

        private List<AboutMe> ParseAboutMeListFromJson(string json)
        {
            var sections = new List<AboutMe>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var result))
                    {
                        if (result.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var cols = result.GetProperty("cols");

                            for (int r = 0; r < rows.GetArrayLength(); r++)
                            {
                                var row = rows[r];
                                var values = row.GetProperty("values");
                                var section = new AboutMe();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var value = values[i];

                                    switch (colName)
                                    {
                                        case "Id": section.Id = value.ValueKind == JsonValueKind.Null ? 0 : value.GetInt32(); break;
                                        case "SectionKey": section.SectionKey = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "Title": section.Title = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "Content": section.Content = value.ValueKind == JsonValueKind.Null ? "" : value.GetString() ?? ""; break;
                                        case "Icon": section.Icon = value.ValueKind == JsonValueKind.Null ? null : value.GetString(); break;
                                        case "SortOrder": section.SortOrder = value.ValueKind == JsonValueKind.Null ? 0 : value.GetInt32(); break;
                                        case "UpdatedAt": section.UpdatedAt = value.ValueKind == JsonValueKind.Null ? DateTime.Now : DateTime.Parse(value.GetString() ?? DateTime.Now.ToString()); break;
                                    }
                                }
                                sections.Add(section);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 解析 AboutMe 列表 JSON 失败: {ex.Message}");
            }
            return sections;
        }

        // ============================================================
        // 工具方法
        // ============================================================

        private string EscapeSql(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("'", "''");
        }
    }
}
