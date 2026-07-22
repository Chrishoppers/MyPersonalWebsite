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
        // 用户相关
        // ============================================================

        public async Task AddUserAsync(User user)
        {
            if (!_tursoAvailable) throw new Exception("Turso 未配置");

            var maxIdResult = await _tursoService.QueryAsync("SELECT MAX(Id) as MaxId FROM Users");
            var maxId = ParseMaxId(maxIdResult);
            user.Id = maxId + 1;

            var sql = $@"INSERT INTO Users (
                Id, Username, Email, PasswordHash, IsEmailVerified, IsAdmin,
                CreatedAt, LastLoginAt, IsBanned, BanExpiry, BanReason,
                IsDeleted, DeletedAt, DeleteReason, DeleteNote,
                AvatarUrl, IsAvatarApproved, AvatarSubmittedAt,
                PendingEmail, PendingUsername, IsEmailChangeApproved, IsUsernameChangeApproved,
                VerificationCode, VerificationCodeExpiry, IsApproved
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
                {(user.VerificationCodeExpiry.HasValue ? $"'{user.VerificationCodeExpiry.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                {(user.IsApproved ? 1 : 0)}
            )";

            await _tursoService.ExecuteSqlAsync(sql);
            Console.WriteLine($"✅ 用户 {user.Username} 已写入 Turso");
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (!_tursoAvailable) return null;

            var result = await _tursoService.QueryAsync($"SELECT * FROM Users WHERE Email = '{EscapeSql(email)}' AND IsDeleted = 0");
            return ParseUserFromJson(result);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            if (!_tursoAvailable) return null;

            var result = await _tursoService.QueryAsync($"SELECT * FROM Users WHERE Username = '{EscapeSql(username)}' AND IsDeleted = 0");
            return ParseUserFromJson(result);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            if (!_tursoAvailable) return null;

            var result = await _tursoService.QueryAsync($"SELECT * FROM Users WHERE Id = {id}");
            var user = ParseUserFromJson(result);
            
            // ⭐ 调试日志
            if (user != null)
            {
                Console.WriteLine($"✅ 读取用户: Id={user.Id}, 用户名={user.Username}, IsApproved={user.IsApproved}");
            }
            
            return user;
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
                DeleteReason = {(string.IsNullOrEmpty(user.DeleteReason) ? "NULL" : $"'{EscapeSql(user.DeleteReason)}'")},
                DeleteNote = {(string.IsNullOrEmpty(user.DeleteNote) ? "NULL" : $"'{EscapeSql(user.DeleteNote)}'")},
                AvatarUrl = {(string.IsNullOrEmpty(user.AvatarUrl) ? "NULL" : $"'{EscapeSql(user.AvatarUrl)}'")},
                IsAvatarApproved = {(user.IsAvatarApproved ? 1 : 0)},
                AvatarSubmittedAt = {(user.AvatarSubmittedAt.HasValue ? $"'{user.AvatarSubmittedAt.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                PendingEmail = {(string.IsNullOrEmpty(user.PendingEmail) ? "NULL" : $"'{EscapeSql(user.PendingEmail)}'")},
                PendingUsername = {(string.IsNullOrEmpty(user.PendingUsername) ? "NULL" : $"'{EscapeSql(user.PendingUsername)}'")},
                IsEmailChangeApproved = {(user.IsEmailChangeApproved ? 1 : 0)},
                IsUsernameChangeApproved = {(user.IsUsernameChangeApproved ? 1 : 0)},
                VerificationCode = {(string.IsNullOrEmpty(user.VerificationCode) ? "NULL" : $"'{EscapeSql(user.VerificationCode)}'")},
                VerificationCodeExpiry = {(user.VerificationCodeExpiry.HasValue ? $"'{user.VerificationCodeExpiry.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                IsApproved = {(user.IsApproved ? 1 : 0)}
            WHERE Id = {user.Id}";

            await _tursoService.ExecuteSqlAsync(sql);
            Console.WriteLine($"✅ 用户 {user.Username} 已更新到 Turso (IsApproved={user.IsApproved})");
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
        // 博客相关
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
            Console.WriteLine($"✅ 博客 {blog.Title} 已写入 Turso");
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
            Console.WriteLine($"✅ 博客 {blog.Title} 已更新到 Turso");
        }

        public async Task DeleteBlogAsync(int id)
        {
            if (!_tursoAvailable) return;
            await _tursoService.ExecuteSqlAsync($"DELETE FROM Blogs WHERE Id = {id}");
            Console.WriteLine($"✅ 博客 {id} 已从 Turso 删除");
        }

        // ============================================================
        // 留言相关
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
            Console.WriteLine($"✅ 留言已写入 Turso");
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
            Console.WriteLine($"✅ 留言已更新到 Turso");
        }

        public async Task DeleteMessageAsync(int id)
        {
            if (!_tursoAvailable) return;
            await _tursoService.ExecuteSqlAsync($"DELETE FROM Messages WHERE Id = {id}");
            Console.WriteLine($"✅ 留言 {id} 已从 Turso 删除");
        }

        public async Task SaveChangesAsync()
        {
            // 不需要，因为直接写 Turso
        }

        // ============================================================
        // 管理员账号
        // ============================================================

        public async Task EnsureAdminExistsAsync()
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
                    IsApproved = true,
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

        public async Task AddAboutMeAsync(AboutMe section)
        {
            if (!_tursoAvailable) return;

            var maxIdResult = await _tursoService.QueryAsync("SELECT MAX(Id) as MaxId FROM AboutMeContents");
            var maxId = ParseMaxId(maxIdResult);
            section.Id = maxId + 1;

            var sql = $@"INSERT INTO AboutMeContents (
                Id, SectionKey, Title, Content, Icon, SortOrder, UpdatedAt
            ) VALUES (
                {section.Id}, '{EscapeSql(section.SectionKey)}',
                '{EscapeSql(section.Title)}', '{EscapeSql(section.Content)}',
                {(string.IsNullOrEmpty(section.Icon) ? "NULL" : $"'{EscapeSql(section.Icon)}'")},
                {section.SortOrder}, '{section.UpdatedAt:yyyy-MM-dd HH:mm:ss}'
            )";

            await _tursoService.ExecuteSqlAsync(sql);
            Console.WriteLine($"✅ AboutMe {section.SectionKey} 已写入 Turso");
        }

        // ============================================================
        // 公开方法（供 Program.cs 调用）
        // ============================================================

        public async Task<string> QueryAsync(string sql)
        {
            if (!_tursoAvailable) return "{}";
            return await _tursoService.QueryAsync(sql);
        }

        public async Task<bool> ExecuteSqlAsync(string sql)
        {
            if (!_tursoAvailable) return false;
            return await _tursoService.ExecuteSqlAsync(sql);
        }

        // ============================================================
        // 兼容旧接口
        // ============================================================

        public async Task<List<User>> GetAllUsersWithFallbackAsync()
        {
            return await GetAllUsersAsync();
        }

        // ============================================================
        // JSON 解析方法
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
                return int.TryParse(je.GetString(), out var parsedValue) ? parsedValue : 0;
            return 0;
        }
        return int.TryParse(val?.ToString(), out var parsedValue) ? parsedValue : 0;
    }
    catch { return 0; }
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
                                var val = GetValueFromRow(row[0]);
                                if (val is JsonElement je && je.ValueKind != JsonValueKind.Null)
                                {
                                    if (je.ValueKind == JsonValueKind.Number)
                                        return je.GetInt32();
                                    if (je.ValueKind == JsonValueKind.String)
                                        return int.TryParse(je.GetString(), out var parsed) ? parsed : 0;
                                }
                            }
                        }
                    }
                }
                return 0;
            }
            catch { return 0; }
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
                                    case "Id": user.Id = GetIntFromRow(element); break;
                                    case "Username": user.Username = GetStringFromRow(element); break;
                                    case "Email": user.Email = GetStringFromRow(element); break;
                                    case "PasswordHash": user.PasswordHash = GetStringFromRow(element); break;
                                    case "IsEmailVerified": user.IsEmailVerified = GetBoolFromRow(element); break;
                                    case "IsAdmin": user.IsAdmin = GetBoolFromRow(element); break;
                                    case "IsApproved": user.IsApproved = GetBoolFromRow(element); break;
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
                                        case "Id": user.Id = GetIntFromRow(element); break;
                                        case "Username": user.Username = GetStringFromRow(element); break;
                                        case "Email": user.Email = GetStringFromRow(element); break;
                                        case "PasswordHash": user.PasswordHash = GetStringFromRow(element); break;
                                        case "IsEmailVerified": user.IsEmailVerified = GetBoolFromRow(element); break;
                                        case "IsAdmin": user.IsAdmin = GetBoolFromRow(element); break;
                                        case "IsApproved": user.IsApproved = GetBoolFromRow(element); break;
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
