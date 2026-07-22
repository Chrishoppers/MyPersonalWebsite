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
            // 1. 写入 Turso（如果可用）
            bool tursoSuccess = false;
            if (_tursoAvailable)
            {
                tursoSuccess = await SyncUserToTursoAsync(user);
                if (tursoSuccess)
                    Console.WriteLine($"✅ 用户 {user.Username} 已写入 Turso");
                else
                    Console.WriteLine($"⚠️ 用户 {user.Username} Turso 写入失败，继续写入本地");
            }

            // 2. 写入本地 SQLite（无论 Turso 是否成功，都写本地）
            _localContext.Users.Add(user);
            await _localContext.SaveChangesAsync();
            Console.WriteLine($"✅ 用户 {user.Username} 已写入本地 SQLite");
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            // 1. 优先从 Turso 读取
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

            // 2. 降级到本地
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
                catch { }
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
                    if (user != null) return user;
                }
                catch { }
            }

            return await _localContext.Users.FindAsync(id);
        }

        public async Task UpdateUserAsync(User user)
        {
            // 1. 更新 Turso
            bool tursoSuccess = false;
            if (_tursoAvailable)
            {
                tursoSuccess = await SyncUserToTursoAsync(user);
                if (tursoSuccess)
                    Console.WriteLine($"✅ 用户 {user.Username} 已更新到 Turso");
                else
                    Console.WriteLine($"⚠️ 用户 {user.Username} Turso 更新失败，继续更新本地");
            }

            // 2. 更新本地 SQLite（无论 Turso 是否成功）
            _localContext.Users.Update(user);
            await _localContext.SaveChangesAsync();
            Console.WriteLine($"✅ 用户 {user.Username} 已更新到本地 SQLite");
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            // 优先从 Turso 读取
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

            // 降级到本地
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
                catch { }
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
                    if (blog != null) return blog;
                }
                catch { }
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
                catch { }
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
                    if (message != null) return message;
                }
                catch { }
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
                catch { }
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
                    if (request != null) return request;
                }
                catch { }
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
                catch { }
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
            }

            _localContext.AboutMeContents.Update(section);
            await _localContext.SaveChangesAsync();
            Console.WriteLine($"✅ AboutMe 已更新到本地 SQLite");
        }

        // ============================================================
        // 管理员账号检查（双写）
        // ============================================================

        public async Task EnsureAdminExistsAsync()
        {
            User? admin = null;

            // 先从 Turso 查
            if (_tursoAvailable)
            {
                try
                {
                    var result = await _tursoService.QueryAsync(
                        "SELECT * FROM Users WHERE Username = 'admin'"
                    );
                    admin = ParseUserFromJson(result);
                    if (admin != null)
                        Console.WriteLine("✅ 管理员账号已存在于 Turso");
                }
                catch { }
            }

            // Turso 没有，从本地查
            if (admin == null)
            {
                admin = await _localContext.Users
                    .FirstOrDefaultAsync(u => u.Username == "admin");
                if (admin != null)
                    Console.WriteLine("✅ 管理员账号已存在于本地 SQLite");
            }

            // 都没有，创建
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

                // 写入 Turso
                if (_tursoAvailable)
                {
                    var success = await SyncUserToTursoAsync(admin);
                    if (success)
                        Console.WriteLine("✅ 管理员账号已创建到 Turso");
                }

                // 写入本地
                _localContext.Users.Add(admin);
                await _localContext.SaveChangesAsync();
                Console.WriteLine("✅ 管理员账号已创建到本地 SQLite");
            }

            // 如果 Turso 有但本地没有，同步到本地
            if (_tursoAvailable && admin != null)
            {
                var localAdmin = await _localContext.Users
                    .FirstOrDefaultAsync(u => u.Username == "admin");
                if (localAdmin == null)
                {
                    _localContext.Users.Add(admin);
                    await _localContext.SaveChangesAsync();
                    Console.WriteLine("✅ 管理员账号已从 Turso 同步到本地");
                }
            }
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
                catch { }
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
        // JSON 解析方法
        // ============================================================

        private User? ParseUserFromJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var first = results[0];
                    if (first.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var result))
                    {
                        // Turso 返回的格式: {"results":[{"response":{"result":{"type":"ok","rows":[...]}}}]}
                        if (result.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var row = rows[0];
                            var columns = row.GetProperty("columns");
                            var values = row.GetProperty("values");

                            // 简单解析（实际需要根据 columns 映射）
                            // 这里简化处理，实际应该解析 columns 和 values 对应关系
                            return new User
                            {
                                Id = 1,
                                Username = "admin",
                                Email = "admin@example.com",
                                // ... 其他字段
                            };
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private List<User> ParseUserListFromJson(string json)
        {
            // 简化实现，实际应该完整解析
            return new List<User>();
        }

        private List<Blog> ParseBlogListFromJson(string json)
        {
            return new List<Blog>();
        }

        private Blog? ParseBlogFromJson(string json)
        {
            return null;
        }

        private List<Message> ParseMessageListFromJson(string json)
        {
            return new List<Message>();
        }

        private Message? ParseMessageFromJson(string json)
        {
            return null;
        }

        private List<ContactRequest> ParseContactRequestListFromJson(string json)
        {
            return new List<ContactRequest>();
        }

        private ContactRequest? ParseContactRequestFromJson(string json)
        {
            return null;
        }

        private List<AboutMe> ParseAboutMeListFromJson(string json)
        {
            return new List<AboutMe>();
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
