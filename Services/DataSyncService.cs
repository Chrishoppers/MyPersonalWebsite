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
        // 用户相关
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
                    var jsonResult = await _tursoService.QueryAsync($"SELECT * FROM Users WHERE Email = '{EscapeSql(email)}'");
                    var user = ParseUserFromJson(jsonResult);
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
            return await _localContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            if (_tursoAvailable)
            {
                try
                {
                    var jsonResult = await _tursoService.QueryAsync($"SELECT * FROM Users WHERE Username = '{EscapeSql(username)}'");
                    var user = ParseUserFromJson(jsonResult);
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
            return await _localContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            if (_tursoAvailable)
            {
                try
                {
                    var jsonResult = await _tursoService.QueryAsync($"SELECT * FROM Users WHERE Id = {id}");
                    var user = ParseUserFromJson(jsonResult);
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

        public async Task DeleteUser(int id)
        {
            var user = await _localContext.Users.FindAsync(id);
            if (user != null)
            {
                user.IsDeleted = true;
                user.DeletedAt = DateTime.Now;
                _localContext.Users.Update(user);
                await _localContext.SaveChangesAsync();
                Console.WriteLine($"✅ 用户 {user.Username} 已软删除（本地）");

                if (_tursoAvailable)
                {
                    await SyncUserToTursoAsync(user);
                    Console.WriteLine($"✅ 用户 {user.Username} 已软删除（Turso）");
                }
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            if (_tursoAvailable)
            {
                try
                {
                    var jsonResult = await _tursoService.QueryAsync("SELECT * FROM Users");
                    var users = ParseUserListFromJson(jsonResult);
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
        // 博客相关
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
                    var jsonResult = await _tursoService.QueryAsync("SELECT * FROM Blogs ORDER BY PublishDate DESC");
                    var blogs = ParseBlogListFromJson(jsonResult);
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

            var localBlogs = await _localContext.Blogs.OrderByDescending(b => b.PublishDate).ToListAsync();
            Console.WriteLine($"📂 从本地 SQLite 读取 {localBlogs.Count} 篇博客");
            return localBlogs;
        }

        public async Task<Blog?> GetBlogByIdAsync(int id)
        {
            if (_tursoAvailable)
            {
                try
                {
                    var jsonResult = await _tursoService.QueryAsync($"SELECT * FROM Blogs WHERE Id = {id}");
                    var blog = ParseBlogFromJson(jsonResult);
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
        // 留言相关
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
                    var jsonResult = await _tursoService.QueryAsync("SELECT * FROM Messages ORDER BY CreateTime DESC");
                    var messages = ParseMessageListFromJson(jsonResult);
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

            var localMessages = await _localContext.Messages.OrderByDescending(m => m.CreateTime).ToListAsync();
            Console.WriteLine($"📂 从本地 SQLite 读取 {localMessages.Count} 条留言");
            return localMessages;
        }

        public async Task<Message?> GetMessageByIdAsync(int id)
        {
            if (_tursoAvailable)
            {
                try
                {
                    var jsonResult = await _tursoService.QueryAsync($"SELECT * FROM Messages WHERE Id = {id}");
                    var message = ParseMessageFromJson(jsonResult);
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
        // ContactRequest 相关
        // ============================================================

        public async Task<List<ContactRequest>> GetContactRequestsAsync()
        {
            if (_tursoAvailable)
            {
                try
                {
                    var jsonResult = await _tursoService.QueryAsync("SELECT * FROM ContactRequests ORDER BY RequestTime DESC");
                    var requests = ParseContactRequestListFromJson(jsonResult);
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

            return await _localContext.ContactRequests.OrderByDescending(r => r.RequestTime).ToListAsync();
        }

        public async Task<ContactRequest?> GetContactRequestByIdAsync(int id)
        {
            if (_tursoAvailable)
            {
                try
                {
                    var jsonResult = await _tursoService.QueryAsync($"SELECT * FROM ContactRequests WHERE Id = {id}");
                    var request = ParseContactRequestFromJson(jsonResult);
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
        // AboutMe 相关
        // ============================================================

        public async Task<List<AboutMe>> GetAboutMeAsync()
        {
            if (_tursoAvailable)
            {
                try
                {
                    var jsonResult = await _tursoService.QueryAsync("SELECT * FROM AboutMeContents ORDER BY SortOrder");
                    var sections = ParseAboutMeListFromJson(jsonResult);
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

            return await _localContext.AboutMeContents.OrderBy(s => s.SortOrder).ToListAsync();
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
        // 管理员账号
        // ============================================================

        public async Task EnsureAdminExistsAsync()
        {
            User? admin = null;

            if (_tursoAvailable)
            {
                try
                {
                    var jsonResult = await _tursoService.QueryAsync("SELECT * FROM Users WHERE Username = 'admin'");
                    admin = ParseUserFromJson(jsonResult);
                    if (admin != null)
                    {
                        Console.WriteLine("✅ 管理员账号已存在于 Turso");
                        var localAdmin = await _localContext.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                        if (localAdmin == null)
                        {
                            _localContext.Users.Add(admin);
                            await _localContext.SaveChangesAsync();
                            Console.WriteLine("✅ 管理员账号已从 Turso 同步到本地");
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Turso 查询管理员失败: {ex.Message}");
                }
            }

            admin = await _localContext.Users.FirstOrDefaultAsync(u => u.Username == "admin");

            if (admin != null)
            {
                Console.WriteLine("✅ 管理员账号已存在于本地 SQLite");
                if (_tursoAvailable)
                {
                    await SyncUserToTursoAsync(admin);
                    Console.WriteLine("✅ 管理员账号已从本地同步到 Turso");
                }
                return;
            }

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
                    var jsonResult = await _tursoService.QueryAsync("SELECT * FROM Users WHERE IsDeleted = 0 ORDER BY CreatedAt DESC");
                    var users = ParseUserListFromJson(jsonResult);
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

            return await _localContext.Users.Where(u => !u.IsDeleted).OrderByDescending(u => u.CreatedAt).ToListAsync();
        }

        // ============================================================
        // Turso 同步方法
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
                    '{EscapeSql(user.PasswordHash)}',
                    {(user.IsEmailVerified ? 1 : 0)},
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
        // JSON 解析方法 - 所有变量名都已修复
        // ============================================================

        private object? GetValueFromRow(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("value", out var val))
                    return val;
                if (element.TryGetProperty("Value", out var val2))
                    return val2;
                return element;
            }
            return element;
        }

        private string GetStringFromRow(JsonElement element)
        {
            try
            {
                var val = GetValueFromRow(element);
                if (val is JsonElement je)
                    return je.ValueKind == JsonValueKind.Null ? "" : je.GetString() ?? "";
                return val?.ToString() ?? "";
            }
            catch { return ""; }
        }

        private int GetIntFromRow(JsonElement element)
        {
            try
            {
                var val = GetValueFromRow(element);
                if (val is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.Null) return 0;
                    if (je.ValueKind == JsonValueKind.Number) return je.GetInt32();
                    if (je.ValueKind == JsonValueKind.String)
                        return int.TryParse(je.GetString(), out var parsed) ? parsed : 0;
                    return 0;
                }
                return int.TryParse(val?.ToString(), out var parsed) ? parsed : 0;
            }
            catch { return 0; }
        }

        private bool GetBoolFromRow(JsonElement element)
        {
            try
            {
                var val = GetValueFromRow(element);
                if (val is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.Null) return false;
                    if (je.ValueKind == JsonValueKind.True) return true;
                    if (je.ValueKind == JsonValueKind.False) return false;
                    if (je.ValueKind == JsonValueKind.Number) return je.GetInt32() == 1;
                    if (je.ValueKind == JsonValueKind.String)
                    {
                        var str = je.GetString()?.ToLower();
                        return str == "1" || str == "true" || str == "yes";
                    }
                    return false;
                }
                var str2 = val?.ToString()?.ToLower();
                return str2 == "1" || str2 == "true" || str2 == "yes";
            }
            catch { return false; }
        }

        private DateTime? GetDateTimeFromRow(JsonElement element)
        {
            try
            {
                var val = GetStringFromRow(element);
                if (string.IsNullOrEmpty(val)) return null;
                return DateTime.Parse(val);
            }
            catch { return null; }
        }

        private string? GetStringOrNullFromRow(JsonElement element)
        {
            try
            {
                var val = GetValueFromRow(element);
                if (val is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.Null) return null;
                    return je.GetString();
                }
                return val?.ToString();
            }
            catch { return null; }
        }

        // ============================================================
        // ParseUserFromJson
        // ============================================================

        private User? ParseUserFromJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstRes = results[0];
                    if (firstRes.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var resultObj))
                    {
                        if (resultObj.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var row = rows[0];
                            var cols = resultObj.GetProperty("cols");

                            if (row.ValueKind != JsonValueKind.Array)
                                return null;

                            var user = new User();

                            for (int i = 0; i < cols.GetArrayLength(); i++)
                            {
                                var colName = cols[i].GetProperty("name").GetString();
                                var element = row[i];

                                switch (colName)
                                {
                                    case "Id": user.Id = GetIntFromRow(element); break;
                                    case "Username": user.Username = GetStringFromRow(element); break;
                                    case "Email": user.Email = GetStringFromRow(element); break;
                                    case "PasswordHash": user.PasswordHash = GetStringFromRow(element); break;
                                    case "IsEmailVerified": user.IsEmailVerified = GetBoolFromRow(element); break;
                                    case "IsAdmin": user.IsAdmin = GetBoolFromRow(element); break;
                                    case "CreatedAt": user.CreatedAt = GetDateTimeFromRow(element) ?? DateTime.Now; break;
                                    case "LastLoginAt": user.LastLoginAt = GetDateTimeFromRow(element); break;
                                    case "IsBanned": user.IsBanned = GetBoolFromRow(element); break;
                                    case "BanExpiry": user.BanExpiry = GetDateTimeFromRow(element); break;
                                    case "BanReason": user.BanReason = GetStringOrNullFromRow(element); break;
                                    case "IsDeleted": user.IsDeleted = GetBoolFromRow(element); break;
                                    case "DeletedAt": user.DeletedAt = GetDateTimeFromRow(element); break;
                                    case "DeleteReason": user.DeleteReason = GetStringOrNullFromRow(element); break;
                                    case "DeleteNote": user.DeleteNote = GetStringOrNullFromRow(element); break;
                                    case "AvatarUrl": user.AvatarUrl = GetStringOrNullFromRow(element); break;
                                    case "IsAvatarApproved": user.IsAvatarApproved = GetBoolFromRow(element); break;
                                    case "AvatarSubmittedAt": user.AvatarSubmittedAt = GetDateTimeFromRow(element); break;
                                    case "PendingEmail": user.PendingEmail = GetStringOrNullFromRow(element); break;
                                    case "PendingUsername": user.PendingUsername = GetStringOrNullFromRow(element); break;
                                    case "IsEmailChangeApproved": user.IsEmailChangeApproved = GetBoolFromRow(element); break;
                                    case "IsUsernameChangeApproved": user.IsUsernameChangeApproved = GetBoolFromRow(element); break;
                                    case "VerificationCode": user.VerificationCode = GetStringOrNullFromRow(element); break;
                                    case "VerificationCodeExpiry": user.VerificationCodeExpiry = GetDateTimeFromRow(element); break;
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

        // ============================================================
        // ParseUserListFromJson
        // ============================================================

        private List<User> ParseUserListFromJson(string json)
        {
            var users = new List<User>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstRes = results[0];
                    if (firstRes.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var resultObj))
                    {
                        if (resultObj.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var cols = resultObj.GetProperty("cols");

                            for (int r = 0; r < rows.GetArrayLength(); r++)
                            {
                                var row = rows[r];

                                if (row.ValueKind != JsonValueKind.Array)
                                    continue;

                                var user = new User();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var element = row[i];

                                    switch (colName)
                                    {
                                        case "Id": user.Id = GetIntFromRow(element); break;
                                        case "Username": user.Username = GetStringFromRow(element); break;
                                        case "Email": user.Email = GetStringFromRow(element); break;
                                        case "PasswordHash": user.PasswordHash = GetStringFromRow(element); break;
                                        case "IsEmailVerified": user.IsEmailVerified = GetBoolFromRow(element); break;
                                        case "IsAdmin": user.IsAdmin = GetBoolFromRow(element); break;
                                        case "CreatedAt": user.CreatedAt = GetDateTimeFromRow(element) ?? DateTime.Now; break;
                                        case "IsBanned": user.IsBanned = GetBoolFromRow(element); break;
                                        case "IsDeleted": user.IsDeleted = GetBoolFromRow(element); break;
                                        case "AvatarUrl": user.AvatarUrl = GetStringOrNullFromRow(element); break;
                                        case "IsAvatarApproved": user.IsAvatarApproved = GetBoolFromRow(element); break;
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

        // ============================================================
        // ParseBlogListFromJson
        // ============================================================

        private List<Blog> ParseBlogListFromJson(string json)
        {
            var blogs = new List<Blog>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstRes = results[0];
                    if (firstRes.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var resultObj))
                    {
                        if (resultObj.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var cols = resultObj.GetProperty("cols");

                            for (int r = 0; r < rows.GetArrayLength(); r++)
                            {
                                var row = rows[r];

                                if (row.ValueKind != JsonValueKind.Array)
                                    continue;

                                var blog = new Blog();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var element = row[i];

                                    switch (colName)
                                    {
                                        case "Id": blog.Id = GetIntFromRow(element); break;
                                        case "Title": blog.Title = GetStringFromRow(element); break;
                                        case "Content": blog.Content = GetStringFromRow(element); break;
                                        case "Summary": blog.Summary = GetStringFromRow(element); break;
                                        case "PublishDate": blog.PublishDate = GetDateTimeFromRow(element) ?? DateTime.Now; break;
                                        case "CoverImageUrl": blog.CoverImageUrl = GetStringOrNullFromRow(element); break;
                                        case "LikeCount": blog.LikeCount = GetIntFromRow(element); break;
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

        // ============================================================
        // ParseMessageListFromJson
        // ============================================================

        private List<Message> ParseMessageListFromJson(string json)
        {
            var messages = new List<Message>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstRes = results[0];
                    if (firstRes.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var resultObj))
                    {
                        if (resultObj.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var cols = resultObj.GetProperty("cols");

                            for (int r = 0; r < rows.GetArrayLength(); r++)
                            {
                                var row = rows[r];

                                if (row.ValueKind != JsonValueKind.Array)
                                    continue;

                                var msg = new Message();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var element = row[i];

                                    switch (colName)
                                    {
                                        case "Id": msg.Id = GetIntFromRow(element); break;
                                        case "UserId": msg.UserId = GetIntFromRow(element); break;
                                        case "VisitorName": msg.VisitorName = GetStringFromRow(element); break;
                                        case "Email": msg.Email = GetStringFromRow(element); break;
                                        case "Content": msg.Content = GetStringFromRow(element); break;
                                        case "CreateTime": msg.CreateTime = GetDateTimeFromRow(element) ?? DateTime.Now; break;
                                        case "IsApproved": msg.IsApproved = GetBoolFromRow(element); break;
                                        case "LikeCount": msg.LikeCount = GetIntFromRow(element); break;
                                        case "AdminReply": msg.AdminReply = GetStringOrNullFromRow(element); break;
                                        case "AdminReplyTime": msg.AdminReplyTime = GetDateTimeFromRow(element); break;
                                        case "ReportCount": msg.ReportCount = GetIntFromRow(element); break;
                                        case "IsReported": msg.IsReported = GetBoolFromRow(element); break;
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

        // ============================================================
        // ParseContactRequestListFromJson
        // ============================================================

        private List<ContactRequest> ParseContactRequestListFromJson(string json)
        {
            var requests = new List<ContactRequest>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstRes = results[0];
                    if (firstRes.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var resultObj))
                    {
                        if (resultObj.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var cols = resultObj.GetProperty("cols");

                            for (int r = 0; r < rows.GetArrayLength(); r++)
                            {
                                var row = rows[r];

                                if (row.ValueKind != JsonValueKind.Array)
                                    continue;

                                var req = new ContactRequest();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var element = row[i];

                                    switch (colName)
                                    {
                                        case "Id": req.Id = GetIntFromRow(element); break;
                                        case "Platform": req.Platform = GetStringFromRow(element); break;
                                        case "AuthorizationCode": req.AuthorizationCode = GetStringFromRow(element); break;
                                        case "HowKnowMe": req.HowKnowMe = GetStringFromRow(element); break;
                                        case "Identity": req.Identity = GetStringFromRow(element); break;
                                        case "Relationship": req.Relationship = GetStringFromRow(element); break;
                                        case "Remarks": req.Remarks = GetStringFromRow(element); break;
                                        case "UserId": req.UserId = GetIntFromRow(element); break;
                                        case "Username": req.Username = GetStringFromRow(element); break;
                                        case "UserEmail": req.UserEmail = GetStringFromRow(element); break;
                                        case "RequestTime": req.RequestTime = GetDateTimeFromRow(element) ?? DateTime.Now; break;
                                        case "IsApproved": req.IsApproved = GetBoolFromRow(element); break;
                                        case "IsUsed": req.IsUsed = GetBoolFromRow(element); break;
                                        case "UsedTime": req.UsedTime = GetDateTimeFromRow(element); break;
                                        case "UsedBy": req.UsedBy = GetStringOrNullFromRow(element); break;
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

        // ============================================================
        // ParseAboutMeListFromJson
        // ============================================================

        private List<AboutMe> ParseAboutMeListFromJson(string json)
        {
            var sections = new List<AboutMe>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstRes = results[0];
                    if (firstRes.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var resultObj))
                    {
                        if (resultObj.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var cols = resultObj.GetProperty("cols");

                            for (int r = 0; r < rows.GetArrayLength(); r++)
                            {
                                var row = rows[r];

                                if (row.ValueKind != JsonValueKind.Array)
                                    continue;

                                var section = new AboutMe();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var element = row[i];

                                    switch (colName)
                                    {
                                        case "Id": section.Id = GetIntFromRow(element); break;
                                        case "SectionKey": section.SectionKey = GetStringFromRow(element); break;
                                        case "Title": section.Title = GetStringFromRow(element); break;
                                        case "Content": section.Content = GetStringFromRow(element); break;
                                        case "Icon": section.Icon = GetStringOrNullFromRow(element); break;
                                        case "SortOrder": section.SortOrder = GetIntFromRow(element); break;
                                        case "UpdatedAt": section.UpdatedAt = GetDateTimeFromRow(element) ?? DateTime.Now; break;
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
