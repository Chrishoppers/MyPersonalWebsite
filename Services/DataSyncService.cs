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
        private readonly TursoService _tursoService;
        private readonly bool _tursoAvailable;

        public DataSyncService(TursoService tursoService)
        {
            _tursoService = tursoService;
            var url = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL") ?? "";
            var token = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN") ?? "";
            _tursoAvailable = !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(token);

            if (_tursoAvailable)
                Console.WriteLine("✅ Turso 已连接（使用 HTTP API）");
            else
                Console.WriteLine("⚠️ Turso 未配置");
        }

        // ============================================================
        // 用户相关（完全使用 Turso）
        // ============================================================

        public async Task AddUserAsync(User user)
        {
            if (!_tursoAvailable) throw new Exception("Turso 未配置");

            // 生成新 ID
            var maxIdResult = await _tursoService.QueryAsync("SELECT MAX(Id) as MaxId FROM Users");
            var maxId = ParseMaxId(maxIdResult);
            user.Id = maxId + 1;

            var sql = $@"INSERT INTO Users (
                Id, Username, Email, PasswordHash, IsEmailVerified, IsAdmin,
                CreatedAt, IsBanned, IsDeleted
            ) VALUES (
                {user.Id}, '{EscapeSql(user.Username)}', '{EscapeSql(user.Email)}',
                '{EscapeSql(user.PasswordHash)}', {(user.IsEmailVerified ? 1 : 0)},
                {(user.IsAdmin ? 1 : 0)}, '{user.CreatedAt:yyyy-MM-dd HH:mm:ss}',
                {(user.IsBanned ? 1 : 0)}, {(user.IsDeleted ? 1 : 0)}
            )";

            await _tursoService.ExecuteSqlAsync(sql);
            Console.WriteLine($"✅ 用户 {user.Username} 已写入 Turso");
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (!_tursoAvailable) return null;

            var result = await _tursoService.QueryAsync($"SELECT * FROM Users WHERE Email = '{EscapeSql(email)}'");
            return ParseUserFromJson(result);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            if (!_tursoAvailable) return null;

            var result = await _tursoService.QueryAsync($"SELECT * FROM Users WHERE Username = '{EscapeSql(username)}'");
            return ParseUserFromJson(result);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            if (!_tursoAvailable) return null;

            var result = await _tursoService.QueryAsync($"SELECT * FROM Users WHERE Id = {id}");
            return ParseUserFromJson(result);
        }

        public async Task UpdateUserAsync(User user)
        {
            if (!_tursoAvailable) return;

            var sql = $@"UPDATE Users SET
                Username = '{EscapeSql(user.Username)}',
                Email = '{EscapeSql(user.Email)}',
                PasswordHash = '{EscapeSql(user.PasswordHash)}',
                IsEmailVerified = {(user.IsEmailVerified ? 1 : 0)},
                IsAdmin = {(user.IsAdmin ? 1 : 0)},
                LastLoginAt = {(user.LastLoginAt.HasValue ? $"'{user.LastLoginAt.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                IsBanned = {(user.IsBanned ? 1 : 0)},
                BanExpiry = {(user.BanExpiry.HasValue ? $"'{user.BanExpiry.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                BanReason = {(string.IsNullOrEmpty(user.BanReason) ? "NULL" : $"'{EscapeSql(user.BanReason)}'")},
                IsDeleted = {(user.IsDeleted ? 1 : 0)},
                DeletedAt = {(user.DeletedAt.HasValue ? $"'{user.DeletedAt.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                AvatarUrl = {(string.IsNullOrEmpty(user.AvatarUrl) ? "NULL" : $"'{EscapeSql(user.AvatarUrl)}'")},
                IsAvatarApproved = {(user.IsAvatarApproved ? 1 : 0)},
                PendingEmail = {(string.IsNullOrEmpty(user.PendingEmail) ? "NULL" : $"'{EscapeSql(user.PendingEmail)}'")},
                PendingUsername = {(string.IsNullOrEmpty(user.PendingUsername) ? "NULL" : $"'{EscapeSql(user.PendingUsername)}'")},
                IsEmailChangeApproved = {(user.IsEmailChangeApproved ? 1 : 0)},
                IsUsernameChangeApproved = {(user.IsUsernameChangeApproved ? 1 : 0)}
            WHERE Id = {user.Id}";

            await _tursoService.ExecuteSqlAsync(sql);
            Console.WriteLine($"✅ 用户 {user.Username} 已更新到 Turso");
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            if (!_tursoAvailable) return new List<User>();

            var result = await _tursoService.QueryAsync("SELECT * FROM Users");
            return ParseUserListFromJson(result);
        }

        public async Task DeleteUser(int id)
        {
            if (!_tursoAvailable) return;

            await _tursoService.ExecuteSqlAsync($"DELETE FROM Users WHERE Id = {id}");
            Console.WriteLine($"✅ 用户 {id} 已从 Turso 删除");
        }

        // ============================================================
        // 博客相关（完全使用 Turso）
        // ============================================================

        public async Task AddBlogAsync(Blog blog)
        {
            if (!_tursoAvailable) return;

            var maxIdResult = await _tursoService.QueryAsync("SELECT MAX(Id) as MaxId FROM Blogs");
            var maxId = ParseMaxId(maxIdResult);
            blog.Id = maxId + 1;

            var sql = $@"INSERT INTO Blogs (
                Id, Title, Content, Summary, PublishDate, CoverImageUrl, LikeCount
            ) VALUES (
                {blog.Id}, '{EscapeSql(blog.Title)}', '{EscapeSql(blog.Content)}',
                '{EscapeSql(blog.Summary)}', '{blog.PublishDate:yyyy-MM-dd HH:mm:ss}',
                {(string.IsNullOrEmpty(blog.CoverImageUrl) ? "NULL" : $"'{EscapeSql(blog.CoverImageUrl)}'")},
                {blog.LikeCount}
            )";

            await _tursoService.ExecuteSqlAsync(sql);
        }

        public async Task<List<Blog>> GetBlogsAsync()
        {
            if (!_tursoAvailable) return new List<Blog>();

            var result = await _tursoService.QueryAsync("SELECT * FROM Blogs ORDER BY PublishDate DESC");
            return ParseBlogListFromJson(result);
        }

        public async Task<Blog?> GetBlogByIdAsync(int id)
        {
            if (!_tursoAvailable) return null;

            var result = await _tursoService.QueryAsync($"SELECT * FROM Blogs WHERE Id = {id}");
            return ParseBlogFromJson(result);
        }

        public async Task UpdateBlogAsync(Blog blog)
        {
            if (!_tursoAvailable) return;

            var sql = $@"UPDATE Blogs SET
                Title = '{EscapeSql(blog.Title)}',
                Content = '{EscapeSql(blog.Content)}',
                Summary = '{EscapeSql(blog.Summary)}',
                PublishDate = '{blog.PublishDate:yyyy-MM-dd HH:mm:ss}',
                CoverImageUrl = {(string.IsNullOrEmpty(blog.CoverImageUrl) ? "NULL" : $"'{EscapeSql(blog.CoverImageUrl)}'")},
                LikeCount = {blog.LikeCount}
            WHERE Id = {blog.Id}";

            await _tursoService.ExecuteSqlAsync(sql);
        }

        public async Task DeleteBlogAsync(int id)
        {
            if (!_tursoAvailable) return;
            await _tursoService.ExecuteSqlAsync($"DELETE FROM Blogs WHERE Id = {id}");
        }

        // ============================================================
        // 留言相关（完全使用 Turso）
        // ============================================================

        public async Task AddMessageAsync(Message message)
        {
            if (!_tursoAvailable) return;

            var maxIdResult = await _tursoService.QueryAsync("SELECT MAX(Id) as MaxId FROM Messages");
            var maxId = ParseMaxId(maxIdResult);
            message.Id = maxId + 1;

            var sql = $@"INSERT INTO Messages (
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
        }

        public async Task<List<Message>> GetMessagesAsync()
        {
            if (!_tursoAvailable) return new List<Message>();

            var result = await _tursoService.QueryAsync("SELECT * FROM Messages ORDER BY CreateTime DESC");
            return ParseMessageListFromJson(result);
        }

        public async Task<Message?> GetMessageByIdAsync(int id)
        {
            if (!_tursoAvailable) return null;

            var result = await _tursoService.QueryAsync($"SELECT * FROM Messages WHERE Id = {id}");
            return ParseMessageFromJson(result);
        }

        public async Task UpdateMessageAsync(Message message)
        {
            if (!_tursoAvailable) return;

            var sql = $@"UPDATE Messages SET
                UserId = {message.UserId},
                VisitorName = '{EscapeSql(message.VisitorName)}',
                Email = '{EscapeSql(message.Email)}',
                Content = '{EscapeSql(message.Content)}',
                IsApproved = {(message.IsApproved ? 1 : 0)},
                LikeCount = {message.LikeCount},
                AdminReply = {(string.IsNullOrEmpty(message.AdminReply) ? "NULL" : $"'{EscapeSql(message.AdminReply)}'")},
                AdminReplyTime = {(message.AdminReplyTime.HasValue ? $"'{message.AdminReplyTime.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                ReportCount = {message.ReportCount},
                IsReported = {(message.IsReported ? 1 : 0)}
            WHERE Id = {message.Id}";

            await _tursoService.ExecuteSqlAsync(sql);
        }

        public async Task DeleteMessageAsync(int id)
        {
            if (!_tursoAvailable) return;
            await _tursoService.ExecuteSqlAsync($"DELETE FROM Messages WHERE Id = {id}");
        }

        public async Task SaveChangesAsync()
        {
            // 不需要，因为直接写 Turso
        }

        // ============================================================
        // 管理员账号
        // ============================================================

        public async Task EnsureAdminExistsInTursoAsync()
        {
            if (!_tursoAvailable) return;

            var result = await _tursoService.QueryAsync("SELECT * FROM Users WHERE Username = 'admin'");
            var admin = ParseUserFromJson(result);

            if (admin == null)
            {
                var newAdmin = new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "2908685235@qq.com",
                    PasswordHash = "AQAAAAIAAYagAAAAEJ4Zj6zVqZMjSx5k5r5WYg==",
                    IsEmailVerified = true,
                    IsAdmin = true,
                    IsBanned = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now
                };

                await AddUserAsync(newAdmin);
                Console.WriteLine("✅ 管理员账号已创建到 Turso");
            }
            else
            {
                Console.WriteLine("✅ 管理员账号已存在于 Turso");
            }
        }

        // ============================================================
        // ContactRequest 相关
        // ============================================================

        public async Task<List<ContactRequest>> GetContactRequestsAsync()
        {
            if (!_tursoAvailable) return new List<ContactRequest>();

            var result = await _tursoService.QueryAsync("SELECT * FROM ContactRequests ORDER BY RequestTime DESC");
            return ParseContactRequestListFromJson(result);
        }

        public async Task<ContactRequest?> GetContactRequestByIdAsync(int id)
        {
            if (!_tursoAvailable) return null;

            var result = await _tursoService.QueryAsync($"SELECT * FROM ContactRequests WHERE Id = {id}");
            return ParseContactRequestFromJson(result);
        }

        public async Task UpdateContactRequestAsync(ContactRequest request)
        {
            if (!_tursoAvailable) return;

            var sql = $@"UPDATE ContactRequests SET
                IsUsed = {(request.IsUsed ? 1 : 0)},
                UsedTime = {(request.UsedTime.HasValue ? $"'{request.UsedTime.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                UsedBy = {(string.IsNullOrEmpty(request.UsedBy) ? "NULL" : $"'{EscapeSql(request.UsedBy)}'")}
            WHERE Id = {request.Id}";

            await _tursoService.ExecuteSqlAsync(sql);
        }

        public async Task AddContactRequestAsync(ContactRequest request)
        {
            if (!_tursoAvailable) return;

            var maxIdResult = await _tursoService.QueryAsync("SELECT MAX(Id) as MaxId FROM ContactRequests");
            var maxId = ParseMaxId(maxIdResult);
            request.Id = maxId + 1;

            var sql = $@"INSERT INTO ContactRequests (
                Id, Platform, AuthorizationCode, HowKnowMe, Identity,
                Relationship, Remarks, UserId, Username, UserEmail,
                RequestTime, IsApproved, IsUsed
            ) VALUES (
                {request.Id}, '{EscapeSql(request.Platform)}',
                '{EscapeSql(request.AuthorizationCode)}',
                '{EscapeSql(request.HowKnowMe)}', '{EscapeSql(request.Identity)}',
                '{EscapeSql(request.Relationship)}', '{EscapeSql(request.Remarks)}',
                {request.UserId}, '{EscapeSql(request.Username)}',
                '{EscapeSql(request.UserEmail)}',
                '{request.RequestTime:yyyy-MM-dd HH:mm:ss}',
                {(request.IsApproved ? 1 : 0)},
                {(request.IsUsed ? 1 : 0)}
            )";

            await _tursoService.ExecuteSqlAsync(sql);
        }

        // ============================================================
        // AboutMe 相关
        // ============================================================

        public async Task<List<AboutMe>> GetAboutMeAsync()
        {
            if (!_tursoAvailable) return new List<AboutMe>();

            var result = await _tursoService.QueryAsync("SELECT * FROM AboutMeContents ORDER BY SortOrder");
            return ParseAboutMeListFromJson(result);
        }

        public async Task UpdateAboutMeAsync(AboutMe section)
        {
            if (!_tursoAvailable) return;

            var sql = $@"UPDATE AboutMeContents SET
                Content = '{EscapeSql(section.Content)}',
                UpdatedAt = '{section.UpdatedAt:yyyy-MM-dd HH:mm:ss}'
            WHERE Id = {section.Id}";

            await _tursoService.ExecuteSqlAsync(sql);
        }

        // ============================================================
        // JSON 解析方法
        // ============================================================

        private int ParseMaxId(string json)
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
                            if (row.ValueKind == JsonValueKind.Array && row.GetArrayLength() > 0)
                            {
                                var val = row[0];
                                if (val.ValueKind != JsonValueKind.Null)
                                    return val.GetInt32();
                            }
                        }
                    }
                }
                return 0;
            }
            catch { return 0; }
        }

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

                            if (row.ValueKind != JsonValueKind.Array)
                                return null;

                            var user = new User();

                            for (int i = 0; i < cols.GetArrayLength(); i++)
                            {
                                var colName = cols[i].GetProperty("name").GetString();
                                var element = row[i];

                                switch (colName)
                                {
                                    case "Id": user.Id = element.ValueKind == JsonValueKind.Null ? 0 : element.GetInt32(); break;
                                    case "Username": user.Username = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                    case "Email": user.Email = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                    case "PasswordHash": user.PasswordHash = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                    case "IsEmailVerified": user.IsEmailVerified = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
                                    case "IsAdmin": user.IsAdmin = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
                                    case "CreatedAt": user.CreatedAt = element.ValueKind == JsonValueKind.Null ? DateTime.Now : DateTime.Parse(element.GetString() ?? DateTime.Now.ToString()); break;
                                    case "LastLoginAt": user.LastLoginAt = element.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(element.GetString()!); break;
                                    case "IsBanned": user.IsBanned = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
                                    case "BanExpiry": user.BanExpiry = element.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(element.GetString()!); break;
                                    case "BanReason": user.BanReason = element.ValueKind == JsonValueKind.Null ? null : element.GetString(); break;
                                    case "IsDeleted": user.IsDeleted = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
                                    case "DeletedAt": user.DeletedAt = element.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(element.GetString()!); break;
                                    case "AvatarUrl": user.AvatarUrl = element.ValueKind == JsonValueKind.Null ? null : element.GetString(); break;
                                    case "IsAvatarApproved": user.IsAvatarApproved = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
                                    case "PendingEmail": user.PendingEmail = element.ValueKind == JsonValueKind.Null ? null : element.GetString(); break;
                                    case "PendingUsername": user.PendingUsername = element.ValueKind == JsonValueKind.Null ? null : element.GetString(); break;
                                    case "IsEmailChangeApproved": user.IsEmailChangeApproved = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
                                    case "IsUsernameChangeApproved": user.IsUsernameChangeApproved = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
                                    case "VerificationCode": user.VerificationCode = element.ValueKind == JsonValueKind.Null ? null : element.GetString(); break;
                                    case "VerificationCodeExpiry": user.VerificationCodeExpiry = element.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(element.GetString()!); break;
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

                                if (row.ValueKind != JsonValueKind.Array)
                                    continue;

                                var user = new User();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var element = row[i];

                                    switch (colName)
                                    {
                                        case "Id": user.Id = element.ValueKind == JsonValueKind.Null ? 0 : element.GetInt32(); break;
                                        case "Username": user.Username = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "Email": user.Email = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "PasswordHash": user.PasswordHash = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "IsEmailVerified": user.IsEmailVerified = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
                                        case "IsAdmin": user.IsAdmin = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
                                        case "CreatedAt": user.CreatedAt = element.ValueKind == JsonValueKind.Null ? DateTime.Now : DateTime.Parse(element.GetString() ?? DateTime.Now.ToString()); break;
                                        case "IsBanned": user.IsBanned = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
                                        case "IsDeleted": user.IsDeleted = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
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

                                if (row.ValueKind != JsonValueKind.Array)
                                    continue;

                                var blog = new Blog();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var element = row[i];

                                    switch (colName)
                                    {
                                        case "Id": blog.Id = element.ValueKind == JsonValueKind.Null ? 0 : element.GetInt32(); break;
                                        case "Title": blog.Title = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "Content": blog.Content = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "Summary": blog.Summary = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "PublishDate": blog.PublishDate = element.ValueKind == JsonValueKind.Null ? DateTime.Now : DateTime.Parse(element.GetString() ?? DateTime.Now.ToString()); break;
                                        case "CoverImageUrl": blog.CoverImageUrl = element.ValueKind == JsonValueKind.Null ? null : element.GetString(); break;
                                        case "LikeCount": blog.LikeCount = element.ValueKind == JsonValueKind.Null ? 0 : element.GetInt32(); break;
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

                                if (row.ValueKind != JsonValueKind.Array)
                                    continue;

                                var msg = new Message();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var element = row[i];

                                    switch (colName)
                                    {
                                        case "Id": msg.Id = element.ValueKind == JsonValueKind.Null ? 0 : element.GetInt32(); break;
                                        case "UserId": msg.UserId = element.ValueKind == JsonValueKind.Null ? 0 : element.GetInt32(); break;
                                        case "VisitorName": msg.VisitorName = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "Email": msg.Email = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "Content": msg.Content = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "CreateTime": msg.CreateTime = element.ValueKind == JsonValueKind.Null ? DateTime.Now : DateTime.Parse(element.GetString() ?? DateTime.Now.ToString()); break;
                                        case "IsApproved": msg.IsApproved = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
                                        case "LikeCount": msg.LikeCount = element.ValueKind == JsonValueKind.Null ? 0 : element.GetInt32(); break;
                                        case "AdminReply": msg.AdminReply = element.ValueKind == JsonValueKind.Null ? null : element.GetString(); break;
                                        case "AdminReplyTime": msg.AdminReplyTime = element.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(element.GetString()!); break;
                                        case "ReportCount": msg.ReportCount = element.ValueKind == JsonValueKind.Null ? 0 : element.GetInt32(); break;
                                        case "IsReported": msg.IsReported = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
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

                                if (row.ValueKind != JsonValueKind.Array)
                                    continue;

                                var req = new ContactRequest();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var element = row[i];

                                    switch (colName)
                                    {
                                        case "Id": req.Id = element.ValueKind == JsonValueKind.Null ? 0 : element.GetInt32(); break;
                                        case "Platform": req.Platform = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "AuthorizationCode": req.AuthorizationCode = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "HowKnowMe": req.HowKnowMe = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "Identity": req.Identity = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "Relationship": req.Relationship = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "Remarks": req.Remarks = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "UserId": req.UserId = element.ValueKind == JsonValueKind.Null ? 0 : element.GetInt32(); break;
                                        case "Username": req.Username = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "UserEmail": req.UserEmail = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "RequestTime": req.RequestTime = element.ValueKind == JsonValueKind.Null ? DateTime.Now : DateTime.Parse(element.GetString() ?? DateTime.Now.ToString()); break;
                                        case "IsApproved": req.IsApproved = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
                                        case "IsUsed": req.IsUsed = element.ValueKind == JsonValueKind.Null ? false : element.GetInt32() == 1; break;
                                        case "UsedTime": req.UsedTime = element.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(element.GetString()!); break;
                                        case "UsedBy": req.UsedBy = element.ValueKind == JsonValueKind.Null ? null : element.GetString(); break;
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

                                if (row.ValueKind != JsonValueKind.Array)
                                    continue;

                                var section = new AboutMe();

                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var element = row[i];

                                    switch (colName)
                                    {
                                        case "Id": section.Id = element.ValueKind == JsonValueKind.Null ? 0 : element.GetInt32(); break;
                                        case "SectionKey": section.SectionKey = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "Title": section.Title = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "Content": section.Content = element.ValueKind == JsonValueKind.Null ? "" : element.GetString() ?? ""; break;
                                        case "Icon": section.Icon = element.ValueKind == JsonValueKind.Null ? null : element.GetString(); break;
                                        case "SortOrder": section.SortOrder = element.ValueKind == JsonValueKind.Null ? 0 : element.GetInt32(); break;
                                        case "UpdatedAt": section.UpdatedAt = element.ValueKind == JsonValueKind.Null ? DateTime.Now : DateTime.Parse(element.GetString() ?? DateTime.Now.ToString()); break;
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

        // 兼容旧接口
        public async Task<List<User>> GetAllUsersWithFallbackAsync()
        {
            return await GetAllUsersAsync();
        }
    }
}
