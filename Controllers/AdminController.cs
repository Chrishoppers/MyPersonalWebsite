using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace MyPersonalWebsite.Controllers
{
    public class AdminController : Controller
    {
        private readonly DataSyncService _dataSync;
        private readonly BrevoEmailService _emailService;

        public AdminController(DataSyncService dataSync, BrevoEmailService emailService)
        {
            _dataSync = dataSync;
            _emailService = emailService;
        }
        // ============================================================
// 📧 开启/关闭连对邮件提醒
// ============================================================

[HttpPost]
public async Task<IActionResult> ToggleStreakEmail(bool enable)
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (!userId.HasValue)
        return Json(new { success = false, message = "请先登录" });

    var user = await _dataSync.GetUserByIdAsync(userId.Value);
    if (user == null)
        return Json(new { success = false, message = "用户不存在" });

    user.IsStreakEmailEnabled = enable;
    if (enable)
    {
        user.StreakEmailOptInAt = DateTime.Now;
    }
    else
    {
        user.StreakEmailOptInAt = null;
    }

    await _dataSync.UpdateUserAsync(user);
    return Json(new
    {
        success = true,
        message = enable ? "✅ 连对邮件提醒已开启！每天10:00准时送达" : "❌ 已关闭连对邮件提醒"
    });
}

[HttpGet]
public async Task<IActionResult> GetStreakEmailStatus()
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (!userId.HasValue)
        return Json(new { success = false, isEnabled = false });

    var user = await _dataSync.GetUserByIdAsync(userId.Value);
    if (user == null)
        return Json(new { success = false, isEnabled = false });

    return Json(new { success = true, isEnabled = user.IsStreakEmailEnabled });
}
        // ============================================================
// 🗑️ 批量删除题目
// ============================================================

[HttpPost]
public async Task<IActionResult> BatchDeleteQuestions([FromBody] List<int> ids)
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
        return Json(new { success = false, message = "权限不足" });

    if (ids == null || !ids.Any())
        return Json(new { success = false, message = "请选择要删除的题目" });

    int successCount = 0;
    int failCount = 0;

    foreach (var id in ids)
    {
        try
        {
            await _dataSync.DeleteBankQuestionAsync(id);
            successCount++;
        }
        catch
        {
            failCount++;
        }
    }

    return Json(new
    {
        success = true,
        message = $"✅ 已删除 {successCount} 道题" + (failCount > 0 ? $"，{failCount} 道失败" : "")
    });
}
//在线人数
[HttpGet]
public async Task<IActionResult> GetAllUsersBrief()
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
        return Json(new { success = false, message = "权限不足" });

    var users = await _dataSync.GetAllUsersAsync();
    var brief = users
        .Where(u => !u.IsDeleted)
        .Select(u => new
        {
            id = u.Id,
            username = u.Username,
            avatarUrl = u.AvatarUrl,
            isAvatarApproved = u.IsAvatarApproved,
            isAdmin = u.IsAdmin
        })
        .ToList();

    return Json(new { success = true, users = brief });
}

<!-- ===== 恐怖控制面板 ===== -->
<div style="display:flex;gap:0.5rem;flex-wrap:wrap;margin-top:0.8rem;padding-top:0.8rem;border-top:1px solid rgba(255,255,255,0.04);">
    <button onclick="triggerHorror()" style="padding:0.5rem 1.5rem;border:none;border-radius:40px;background:linear-gradient(135deg,#dc3545,#8B0000);color:#fff;font-weight:600;font-size:0.85rem;cursor:pointer;transition:all 0.3s ease;display:flex;align-items:center;gap:0.5rem;">
        👻 释放恐怖入侵
    </button>
    <button onclick="triggerHorrorWithMessage()" style="padding:0.5rem 1.5rem;border:none;border-radius:40px;background:linear-gradient(135deg,#8B008B,#4B0082);color:#fff;font-weight:600;font-size:0.85rem;cursor:pointer;transition:all 0.3s ease;display:flex;align-items:center;gap:0.5rem;">
        📝 自定义恐怖
    </button>
    <button onclick="triggerGhost()" style="padding:0.5rem 1.5rem;border:none;border-radius:40px;background:linear-gradient(135deg,#2d2d2d,#1a1a1a);color:rgba(255,255,255,0.6);font-weight:600;font-size:0.85rem;cursor:pointer;transition:all 0.3s ease;display:flex;align-items:center;gap:0.5rem;border:1px solid rgba(255,255,255,0.04);">
        👻 幽灵飘过
    </button>
    <button onclick="triggerShake()" style="padding:0.5rem 1.5rem;border:none;border-radius:40px;background:linear-gradient(135deg,#8B4513,#3d1f00);color:#fff;font-weight:600;font-size:0.85rem;cursor:pointer;transition:all 0.3s ease;display:flex;align-items:center;gap:0.5rem;">
        💀 屏幕震动
    </button>
</div>
        
        // ============================================================
// 📅 未来题目安排（自动AI安排 + 手动变更）
// ============================================================

[HttpGet]
public async Task<IActionResult> QuestionSchedule()
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
        return RedirectToAction("Login", "Auth");

    var today = DateTime.Today;

    // ⭐ 自动检查并安排未来7天的题目（智能AI安排）
    await AutoScheduleMissingDaysAsync();

    var schedule = new List<DailyScheduleItem>();

    for (int i = 0; i < 7; i++)
    {
        var date = today.AddDays(i);
        var dateStr = date.ToString("yyyy-MM-dd");

        var result = await _dataSync.QueryAsync($@"
            SELECT dq.Id, dq.QuestionId, dq.Date,
                   b.Question, b.Answer, b.Category, b.Difficulty
            FROM DailyQuestions dq
            JOIN DailyQuestionBank b ON dq.QuestionId = b.Id
            WHERE dq.Date = '{dateStr}'
            LIMIT 1
        ");

        var item = new DailyScheduleItem
        {
            Date = date,
            DateStr = dateStr,
            IsToday = date == today,
            IsPast = date < today
        };

        var question = ParseDailyQuestionFromJson(result);
        if (question != null)
        {
            item.QuestionId = question.QuestionId ?? 0;
            item.Question = question.Question;
            item.Answer = question.Answer;
            item.Category = question.Category;
            item.Difficulty = question.Difficulty;
            item.IsScheduled = true;
        }
        else
        {
            item.IsScheduled = false;
        }

        schedule.Add(item);
    }

    var bankQuestions = await _dataSync.GetAllBankQuestionsAsync();

    ViewBag.Schedule = schedule;
    ViewBag.BankQuestions = bankQuestions;
    ViewBag.Today = today;

    return View();
}



// ============================================================
// 辅助：从JSON解析QuestionId
// ============================================================

private int ParseQuestionIdFromJson(string json)
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

                    for (int i = 0; i < cols.GetArrayLength(); i++)
                    {
                        var colName = cols[i].GetProperty("name").GetString();
                        if (colName == "QuestionId")
                        {
                            var element = row[i];
                            var value = element.ValueKind == JsonValueKind.Object && element.TryGetProperty("value", out var v) ? v : element;
                            return value.ValueKind == JsonValueKind.Number ? value.GetInt32() : 0;
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
        // ⭐ 批量发送通知
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> BatchSendNotification([FromBody] BatchSendRequest request)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            if (request.UserIds == null || !request.UserIds.Any())
                return Json(new { success = false, message = "请选择至少一位用户" });

            int successCount = 0;
            int failCount = 0;
            var errors = new List<string>();

            foreach (var userId in request.UserIds)
            {
                try
                {
                    var user = await _dataSync.GetUserByIdAsync(userId);
                    if (user == null || user.IsDeleted)
                    {
                        failCount++;
                        continue;
                    }

                    var loginToken = await _dataSync.CreateLoginTokenAsync(userId);

                    var emailHtml = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                            <h2 style='color: #8B5CF6;'>📬 管理员通知</h2>
                            <p>您好 <strong>{user.Username}</strong>！</p>
                            <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #2a2a3e;'>
                                <p><strong>📌 标题：</strong>{request.Title}</p>
                                <p><strong>📝 内容：</strong></p>
                                <p style='color: #ccc;'>{request.Message}</p>
                            </div>
                            <div style='margin: 20px 0; text-align: center;'>
                                <a href='https://chris-hopper.org/Auth/AutoLogin?token={loginToken}' style='display: inline-block; padding: 14px 48px; background: linear-gradient(135deg, #8B5CF6, #EC4899); color: white; text-decoration: none; border-radius: 40px; font-weight: 600; font-size: 1rem; box-shadow: 0 4px 24px rgba(108,60,225,0.2);'>
                                    👁️ 查看详情
                                </a>
                                <p style='color: rgba(255,255,255,0.12); font-size: 0.7rem; margin-top: 0.3rem;'>🔒 点击后自动登录，无需输入密码</p>
                            </div>
                            <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                            <p style='color: #555; font-size: 12px;'>💌 系统自动发送，不用回复。</p>
                        </div>
                    ";

                    await _emailService.SendEmailAsync(user.Email, $"📬 {request.Title} - Chris hopper 个人网站", emailHtml);

                    var notification = new Notification
                    {
                        UserId = userId,
                        Title = request.Title,
                        Message = request.Message,
                        Type = request.Type,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };
                    await _dataSync.AddNotificationAsync(notification);

                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    errors.Add($"用户 {userId}: {ex.Message}");
                }
            }

            return Json(new
            {
                success = true,
                message = $"✅ 已发送给 {successCount} 位用户{(failCount > 0 ? $"，{failCount} 位失败" : "")}",
                details = failCount > 0 ? string.Join("; ", errors) : null
            });
        }

        // ============================================================
        // BatchSendRequest 模型
        // ============================================================

        public class BatchSendRequest
        {
            public List<int> UserIds { get; set; } = new List<int>();
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Type { get; set; } = "info";
        }
                // ============================================================
        // 1. 仪表盘
        // ============================================================
        public async Task<IActionResult> Dashboard()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");

            var users = await _dataSync.GetAllUsersAsync();
            var blogs = await _dataSync.GetBlogsAsync();
            var messages = await _dataSync.GetMessagesAsync();
            var contactRequests = await _dataSync.GetContactRequestsAsync();
            var notifications = await _dataSync.GetAllNotificationsAsync();

            ViewBag.UserCount = users.Count(u => !u.IsDeleted);
            ViewBag.BlogCount = blogs.Count;
            ViewBag.MessageCount = messages.Count;
            ViewBag.PendingMessages = messages.Count(m => !m.IsApproved);
            ViewBag.ContactRequestCount = contactRequests.Count;
            ViewBag.PendingContactRequests = contactRequests.Count(r => !r.IsUsed && !r.IsApproved);
            ViewBag.PendingChangesCount = users.Count(u =>
                !u.IsDeleted && (
                    !string.IsNullOrEmpty(u.PendingUsername) ||
                    !string.IsNullOrEmpty(u.PendingEmail) ||
                    (!u.IsAvatarApproved && !string.IsNullOrEmpty(u.AvatarUrl))
                ));
            ViewBag.NotificationCount = notifications.Count;

            ViewBag.RecentMessages = messages.OrderByDescending(m => m.CreateTime).Take(5).ToList();
            ViewBag.RecentContactRequests = contactRequests.OrderByDescending(r => r.RequestTime).Take(5).ToList();
            return View();
        }

        // ============================================================
        // 2. 博客管理
        // ============================================================
        public async Task<IActionResult> Blogs()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");

            var blogs = await _dataSync.GetBlogsAsync();
            return View(blogs);
        }

        [HttpGet]
        public IActionResult CreateBlog()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBlog(Blog blog)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                blog.PublishDate = DateTime.Now;
                await _dataSync.AddBlogAsync(blog);

                try
                {
                    await _emailService.SendAdminNewBlogNotificationAsync(blog.Title);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"邮件发送失败: {ex.Message}");
                }

                return RedirectToAction("Blogs");
            }
            return View(blog);
        }

        [HttpGet]
        public async Task<IActionResult> EditBlog(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");

            var blog = await _dataSync.GetBlogByIdAsync(id);
            if (blog == null)
                return NotFound();
            return View(blog);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBlog(Blog blog)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                await _dataSync.UpdateBlogAsync(blog);
                return RedirectToAction("Blogs");
            }
            return View(blog);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            await _dataSync.DeleteBlogAsync(id);
            return Json(new { success = true, message = "删除成功" });
        }

        [HttpPost]
        public async Task<IActionResult> UploadBlogImage(IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                    return Json(new { success = false, message = "请选择图片" });

                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(image.ContentType))
                    return Json(new { success = false, message = "只支持 JPG, PNG, GIF, WebP 格式" });

                if (image.Length > 5 * 1024 * 1024)
                    return Json(new { success = false, message = "图片大小不能超过 5MB" });

                var fileName = $"{Guid.NewGuid():N}_{image.FileName}";
                var uploadPath = Path.Combine("wwwroot", "images", "blog");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                return Json(new { success = true, url = $"/images/blog/{fileName}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
                // ============================================================
        // 3. 留言管理
        // ============================================================
        public async Task<IActionResult> Messages()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");

            var messages = await _dataSync.GetMessagesAsync();
            return View(messages);
        }

        // ============================================================
        // 4. 用户管理
        // ============================================================
        public async Task<IActionResult> Users()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");

            var users = await _dataSync.GetAllUsersAsync();
            return View(users.OrderByDescending(u => u.CreatedAt).ToList());
        }

        // ============================================================
        // 5. 授权码管理
        // ============================================================
        public async Task<IActionResult> ContactRequests()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");

            var requests = await _dataSync.GetContactRequestsAsync();
            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> MarkContactUsed(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            var request = await _dataSync.GetContactRequestByIdAsync(id);
            if (request == null)
                return Json(new { success = false, message = "记录不存在" });

            request.IsUsed = true;
            request.UsedTime = DateTime.Now;
            await _dataSync.UpdateContactRequestAsync(request);

            return Json(new { success = true, message = "已标记为已使用" });
        }

        [HttpPost]
        public async Task<IActionResult> UnmarkContactUsed(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            var request = await _dataSync.GetContactRequestByIdAsync(id);
            if (request == null)
                return Json(new { success = false, message = "记录不存在" });

            request.IsUsed = false;
            request.UsedTime = null;
            await _dataSync.UpdateContactRequestAsync(request);

            return Json(new { success = true, message = "已撤销使用标记" });
        }

        [HttpGet]
        public async Task<IActionResult> ContactDetail(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            var request = await _dataSync.GetContactRequestByIdAsync(id);
            if (request == null)
                return Json(new { success = false, message = "记录不存在" });

            return Json(new
            {
                success = true,
                data = new
                {
                    platform = request.Platform,
                    authorizationCode = request.AuthorizationCode,
                    user = new { userId = request.UserId, username = request.Username, userEmail = request.UserEmail },
                    howKnowMe = request.HowKnowMe,
                    identity = request.Identity,
                    relationship = request.Relationship,
                    remarks = request.Remarks,
                    requestTime = request.RequestTime,
                    isApproved = request.IsApproved,
                    isUsed = request.IsUsed,
                    usedTime = request.UsedTime
                }
            });
        }
                // ============================================================
        // 6. 待审核更改
        // ============================================================
        public async Task<IActionResult> PendingChanges()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");

            var users = await _dataSync.GetAllUsersAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUserChange(int userId)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "用户不存在" });

            if (!string.IsNullOrEmpty(user.PendingUsername))
            {
                user.Username = user.PendingUsername;
                user.PendingUsername = null;
                user.IsUsernameChangeApproved = true;
            }

            if (!string.IsNullOrEmpty(user.PendingEmail))
            {
                user.Email = user.PendingEmail;
                user.PendingEmail = null;
                user.IsEmailChangeApproved = true;
            }

            if (!user.IsAvatarApproved && !string.IsNullOrEmpty(user.AvatarUrl))
                user.IsAvatarApproved = true;

            await _dataSync.UpdateUserAsync(user);
            return Json(new { success = true, message = "更改已批准" });
        }

        [HttpPost]
        public async Task<IActionResult> RejectUserChange(int userId)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "用户不存在" });

            user.PendingUsername = null;
            user.PendingEmail = null;
            user.IsUsernameChangeApproved = false;
            user.IsEmailChangeApproved = false;

            if (!user.IsAvatarApproved && !string.IsNullOrEmpty(user.AvatarUrl))
            {
                user.AvatarUrl = null;
                user.AvatarSubmittedAt = null;
            }

            await _dataSync.UpdateUserAsync(user);
            return Json(new { success = true, message = "更改已拒绝" });
        }
                // ============================================================
        // 7. 新用户审核（通过）- 无需登录
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ApproveUser(int userId)
        {
            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = false,
                    Title = "❌ 审核失败",
                    Message = "用户不存在，可能已被删除。",
                    IconType = "fail"
                });
            }

            if (user.IsApproved)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = true,
                    Title = "ℹ️ 已审核",
                    Message = $"用户 <strong>{user.Username}</strong> 已经审核通过了，无需重复操作。",
                    IconType = "info"
                });
            }

            user.IsApproved = true;
            user.IsAvatarApproved = true;
            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email,
                    user.Username,
                    "approve",
                    "您的账号已通过管理员审核，现在可以登录了！",
                    "🎉 欢迎加入 Chris hopper 的个人网站！"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return View("AuditResult", new AuditResultViewModel
            {
                Success = true,
                Title = "✅ 审核通过！",
                Message = $"用户 <strong>{user.Username}</strong> 已通过审核。",
                Detail = "用户已收到审核通过的通知邮件。",
                Username = user.Username,
                Email = user.Email,
                IconType = "success"
            });
        }

        // ============================================================
        // 8. 新用户审核（拒绝）- 无需登录
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> RejectUser(int userId)
        {
            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = false,
                    Title = "❌ 审核失败",
                    Message = "用户不存在，可能已被删除。",
                    IconType = "fail"
                });
            }

            if (user.IsDeleted)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = true,
                    Title = "ℹ️ 已处理",
                    Message = $"用户 <strong>{user.Username}</strong> 已经被处理过了。",
                    IconType = "info"
                });
            }

            user.IsDeleted = true;
            user.DeletedAt = DateTime.Now;
            user.DeleteReason = "管理员审核拒绝";
            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email,
                    user.Username,
                    "reject",
                    "您的账号审核未通过，请重新注册或联系管理员。",
                    "如有疑问，请联系管理员 2908685235@qq.com"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return View("AuditResult", new AuditResultViewModel
            {
                Success = false,
                Title = "❌ 已拒绝",
                Message = $"用户 <strong>{user.Username}</strong> 已拒绝。",
                Detail = "用户已收到审核拒绝的通知邮件。",
                Username = user.Username,
                Email = user.Email,
                IconType = "fail"
            });
        }

        // ============================================================
        // 9. 头像审核（通过）- 无需登录
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ApproveAvatar(int userId)
        {
            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = false,
                    Title = "❌ 审核失败",
                    Message = "用户不存在。",
                    IconType = "fail"
                });
            }

            if (user.IsAvatarApproved)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = true,
                    Title = "ℹ️ 已审核",
                    Message = $"用户 <strong>{user.Username}</strong> 的头像已经审核通过了。",
                    IconType = "info"
                });
            }

            user.IsAvatarApproved = true;
            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email,
                    user.Username,
                    "avatar_approve",
                    "您的头像已通过管理员审核！",
                    "头像已更新，现在可以在个人资料中查看了。"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return View("AuditResult", new AuditResultViewModel
            {
                Success = true,
                Title = "✅ 头像审核通过！",
                Message = $"用户 <strong>{user.Username}</strong> 的头像已通过审核。",
                Detail = "用户已收到通知邮件。",
                Username = user.Username,
                Email = user.Email,
                IconType = "success"
            });
        }

        // ============================================================
        // 10. 头像审核（拒绝）- 无需登录
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> RejectAvatar(int userId)
        {
            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = false,
                    Title = "❌ 审核失败",
                    Message = "用户不存在。",
                    IconType = "fail"
                });
            }

            user.AvatarUrl = null;
            user.AvatarSubmittedAt = null;
            user.IsAvatarApproved = false;
            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email,
                    user.Username,
                    "avatar_reject",
                    "您的头像审核未通过，请重新上传。",
                    "请上传清晰、合规的头像图片。"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return View("AuditResult", new AuditResultViewModel
            {
                Success = false,
                Title = "❌ 头像已拒绝",
                Message = $"用户 <strong>{user.Username}</strong> 的头像已拒绝。",
                Detail = "用户已收到通知邮件。",
                Username = user.Username,
                Email = user.Email,
                IconType = "fail"
            });
        }

        // ============================================================
        // 11. 昵称修改审核（通过）- 无需登录
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ApproveUsername(int userId)
        {
            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = false,
                    Title = "❌ 审核失败",
                    Message = "用户不存在。",
                    IconType = "fail"
                });
            }

            if (string.IsNullOrEmpty(user.PendingUsername))
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = true,
                    Title = "ℹ️ 无需审核",
                    Message = $"用户 <strong>{user.Username}</strong> 没有待审核的昵称修改。",
                    IconType = "info"
                });
            }

            var oldUsername = user.Username;
            user.Username = user.PendingUsername;
            user.PendingUsername = null;
            user.IsUsernameChangeApproved = true;
            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email,
                    user.Username,
                    "username_approve",
                    $"您的昵称已从「{oldUsername}」改为「{user.Username}」，已通过审核！",
                    null
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return View("AuditResult", new AuditResultViewModel
            {
                Success = true,
                Title = "✅ 昵称修改通过！",
                Message = $"用户 <strong>{user.Username}</strong> 的昵称修改已通过。",
                Detail = $"原昵称：{oldUsername} → 新昵称：{user.Username}",
                Username = user.Username,
                Email = user.Email,
                IconType = "success"
            });
        }

        // ============================================================
        // 12. 昵称修改审核（拒绝）- 无需登录
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> RejectUsername(int userId)
        {
            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = false,
                    Title = "❌ 审核失败",
                    Message = "用户不存在。",
                    IconType = "fail"
                });
            }

            var pendingName = user.PendingUsername;
            user.PendingUsername = null;
            user.IsUsernameChangeApproved = false;
            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email,
                    user.Username,
                    "username_reject",
                    $"您的昵称「{pendingName}」修改申请未通过审核。",
                    "请使用合规的昵称重新申请。"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return View("AuditResult", new AuditResultViewModel
            {
                Success = false,
                Title = "❌ 昵称修改已拒绝",
                Message = $"用户 <strong>{user.Username}</strong> 的昵称修改已拒绝。",
                Detail = $"拒绝的昵称：{pendingName}",
                Username = user.Username,
                Email = user.Email,
                IconType = "fail"
            });
        }

        // ============================================================
        // 13. 邮箱修改审核（通过）- 无需登录
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ApproveEmail(int userId)
        {
            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = false,
                    Title = "❌ 审核失败",
                    Message = "用户不存在。",
                    IconType = "fail"
                });
            }

            if (string.IsNullOrEmpty(user.PendingEmail))
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = true,
                    Title = "ℹ️ 无需审核",
                    Message = $"用户 <strong>{user.Username}</strong> 没有待审核的邮箱修改。",
                    IconType = "info"
                });
            }

            var oldEmail = user.Email;
            user.Email = user.PendingEmail;
            user.PendingEmail = null;
            user.IsEmailChangeApproved = true;
            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email,
                    user.Username,
                    "email_approve",
                    $"您的邮箱已从「{oldEmail}」改为「{user.Email}」，已通过审核！",
                    null
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return View("AuditResult", new AuditResultViewModel
            {
                Success = true,
                Title = "✅ 邮箱修改通过！",
                Message = $"用户 <strong>{user.Username}</strong> 的邮箱修改已通过。",
                Detail = $"原邮箱：{oldEmail} → 新邮箱：{user.Email}",
                Username = user.Username,
                Email = user.Email,
                IconType = "success"
            });
        }

        // ============================================================
        // 14. 邮箱修改审核（拒绝）- 无需登录
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> RejectEmail(int userId)
        {
            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = false,
                    Title = "❌ 审核失败",
                    Message = "用户不存在。",
                    IconType = "fail"
                });
            }

            var pendingEmail = user.PendingEmail;
            user.PendingEmail = null;
            user.IsEmailChangeApproved = false;
            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email,
                    user.Username,
                    "email_reject",
                    $"您的邮箱「{pendingEmail}」修改申请未通过审核。",
                    "请使用合规的邮箱重新申请。"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return View("AuditResult", new AuditResultViewModel
            {
                Success = false,
                Title = "❌ 邮箱修改已拒绝",
                Message = $"用户 <strong>{user.Username}</strong> 的邮箱修改已拒绝。",
                Detail = $"拒绝的邮箱：{pendingEmail}",
                Username = user.Username,
                Email = user.Email,
                IconType = "fail"
            });
        }

        // ============================================================
        // 15. 留言审核（通过）- 无需登录
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ApproveMessage(int messageId)
        {
            var message = await _dataSync.GetMessageByIdAsync(messageId);
            if (message == null)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = false,
                    Title = "❌ 审核失败",
                    Message = "留言不存在。",
                    IconType = "fail"
                });
            }

            if (message.IsApproved)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = true,
                    Title = "ℹ️ 已审核",
                    Message = "该留言已经审核通过了。",
                    IconType = "info"
                });
            }

            message.IsApproved = true;
            await _dataSync.UpdateMessageAsync(message);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    message.Email,
                    message.VisitorName,
                    "message_approve",
                    "您的留言已通过管理员审核，现在可以在留言板中看到了！",
                    null
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return View("AuditResult", new AuditResultViewModel
            {
                Success = true,
                Title = "✅ 留言审核通过！",
                Message = $"留言已通过审核。",
                Detail = $"留言者：{message.VisitorName}",
                IconType = "success"
            });
        }

        // ============================================================
        // 16. 留言审核（删除）- 无需登录
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> RejectMessage(int messageId)
        {
            var message = await _dataSync.GetMessageByIdAsync(messageId);
            if (message == null)
            {
                return View("AuditResult", new AuditResultViewModel
                {
                    Success = false,
                    Title = "❌ 操作失败",
                    Message = "留言不存在。",
                    IconType = "fail"
                });
            }

            await _dataSync.DeleteMessageAsync(messageId);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    message.Email,
                    message.VisitorName,
                    "message_reject",
                    "您的留言审核未通过，已被删除。",
                    "请遵守留言规范重新发布。"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return View("AuditResult", new AuditResultViewModel
            {
                Success = false,
                Title = "🗑️ 留言已删除",
                Message = $"留言已删除。",
                Detail = $"留言者：{message.VisitorName}",
                IconType = "fail"
            });
        }
                // ============================================================
        // 17. 关于我编辑
        // ============================================================
        public async Task<IActionResult> About()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");

            var sections = await _dataSync.GetAboutMeAsync();
            return View(sections);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAboutMe([FromBody] Dictionary<string, string> data)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            try
            {
                var sections = await _dataSync.GetAboutMeAsync();

                foreach (var item in data)
                {
                    var key = item.Key;
                    var value = item.Value;

                    if (key.StartsWith("social_"))
                        continue;

                    var section = sections.FirstOrDefault(s => s.SectionKey == key);
                    if (section != null)
                    {
                        section.Content = value;
                        section.UpdatedAt = DateTime.Now;
                        await _dataSync.UpdateAboutMeAsync(section);
                    }
                }

                var socialSection = sections.FirstOrDefault(s => s.SectionKey == "social");
                if (socialSection != null)
                {
                    var socialParts = new List<string>();
                    if (!string.IsNullOrEmpty(data.GetValueOrDefault("social_github")))
                        socialParts.Add($"github:{data["social_github"]}");
                    if (!string.IsNullOrEmpty(data.GetValueOrDefault("social_twitter")))
                        socialParts.Add($"twitter:{data["social_twitter"]}");
                    if (!string.IsNullOrEmpty(data.GetValueOrDefault("social_linkedin")))
                        socialParts.Add($"linkedin:{data["social_linkedin"]}");
                    if (!string.IsNullOrEmpty(data.GetValueOrDefault("social_discord")))
                        socialParts.Add($"discord:{data["social_discord"]}");

                    socialSection.Content = string.Join("|", socialParts);
                    socialSection.UpdatedAt = DateTime.Now;
                    await _dataSync.UpdateAboutMeAsync(socialSection);
                }

                return Json(new { success = true, message = "保存成功" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
                // ============================================================
        // 18. ⭐ 发送通知给用户（弹窗 + 邮件 + 自动登录）
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> SendNotification(int userId, string title, string message, string type)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "用户不存在" });

            if (user.IsDeleted)
                return Json(new { success = false, message = "用户已被删除" });

            var loginToken = await _dataSync.CreateLoginTokenAsync(userId);

            try
            {
                var emailHtml = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                        <h2 style='color: #8B5CF6;'>📬 管理员通知</h2>
                        <p>您好 <strong>{user.Username}</strong>！</p>
                        <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #2a2a3e;'>
                            <p><strong>📌 标题：</strong>{title}</p>
                            <p><strong>📝 内容：</strong></p>
                            <p style='color: #ccc;'>{message}</p>
                        </div>
                        <div style='margin: 20px 0; text-align: center;'>
                            <a href='https://chris-hopper.org/Auth/AutoLogin?token={loginToken}' style='display: inline-block; padding: 14px 48px; background: linear-gradient(135deg, #8B5CF6, #EC4899); color: white; text-decoration: none; border-radius: 40px; font-weight: 600; font-size: 1rem;'>
                                👁️ 查看详情
                            </a>
                            <p style='color: rgba(255,255,255,0.12); font-size: 0.7rem; margin-top: 0.3rem;'>🔒 点击后自动登录</p>
                        </div>
                        <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                        <p style='color: #555; font-size: 12px;'>💌 系统自动发送，不用回复。</p>
                    </div>
                ";

                await _emailService.SendEmailAsync(user.Email, $"📬 {title} - Chris hopper 个人网站", emailHtml);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"邮件发送失败: {ex.Message}" });
            }

            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                await _dataSync.AddNotificationAsync(notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"通知保存失败: {ex.Message}");
            }

            return Json(new { success = true, message = $"✅ 通知已发送给 {user.Username}" });
        }

        // ============================================================
        // 19. 封禁用户
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> BanUser(int id, int hours, string reason, string note)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            var user = await _dataSync.GetUserByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "用户不存在" });

            if (user.IsAdmin)
                return Json(new { success = false, message = "不能封禁管理员" });

            user.IsBanned = true;
            user.BanExpiry = hours > 0 ? DateTime.Now.AddHours(hours) : (DateTime?)null;
            user.BanReason = reason;
            user.BanNote = note;

            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email, user.Username, "ban",
                    reason ?? "违反网站规定", note ?? "无");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return Json(new { success = true, message = $"已封禁用户 {user.Username}" });
        }

        // ============================================================
        // 20. 解封用户
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> UnbanUser(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            var user = await _dataSync.GetUserByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "用户不存在" });

            user.IsBanned = false;
            user.BanExpiry = null;
            user.BanReason = null;
            user.BanNote = null;

            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email, user.Username, "unban", "管理员已解封您的账号", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return Json(new { success = true, message = $"已解封用户 {user.Username}" });
        }

        // ============================================================
        // 21. 删除用户
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id, string reason, string note)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            var user = await _dataSync.GetUserByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "用户不存在" });

            if (user.IsAdmin)
                return Json(new { success = false, message = "不能删除管理员" });

            user.IsDeleted = true;
            user.DeletedAt = DateTime.Now;
            user.DeleteReason = reason;
            user.DeleteNote = note;

            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email, user.Username, "delete",
                    reason ?? "违反网站规定", note ?? "无");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return Json(new { success = true, message = $"已删除用户 {user.Username}" });
        }

        // ============================================================
        // 22. 激活用户
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> ActivateUser(int userId)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "用户不存在" });

            user.IsEmailVerified = true;
            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email, user.Username, "activate", "管理员已激活您的账号", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return Json(new { success = true, message = "用户已激活" });
        }
                // ============================================================
        // 📚 题库管理
        // ============================================================

       [HttpGet]
public async Task<IActionResult> QuestionBank()
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
        return RedirectToAction("Login", "Auth");

    var questions = await _dataSync.GetAllBankQuestionsAsync();
    // ⭐ 加上排序
    questions = questions.OrderBy(q => q.Id).ToList();
    return View(questions);
}

        [HttpGet]
        public async Task<IActionResult> GetQuestionDetail(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            var q = await _dataSync.GetBankQuestionByIdAsync(id);
            if (q == null)
                return Json(new { success = false, message = "题目不存在" });

            return Json(new
            {
                success = true,
                data = new
                {
                    q.Id,
                    q.Question,
                    q.Answer,
                    q.Pinyin,
                    q.Hint,
                    q.Category,
                    q.Difficulty,
                    q.IsActive,
                    q.UseCount,
                    q.CreatedAt
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleQuestionStatus(int id, bool enable)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            await _dataSync.ToggleQuestionStatusAsync(id, enable);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            await _dataSync.DeleteBankQuestionAsync(id);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddSingleQuestion([FromBody] AddQuestionRequest request)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            if (string.IsNullOrEmpty(request.Question) || string.IsNullOrEmpty(request.Answer))
                return Json(new { success = false, message = "题目和答案不能为空" });

            var question = new BankQuestion
            {
                Question = request.Question,
                Answer = request.Answer,
                Pinyin = request.Pinyin ?? "",
                Hint = request.Hint ?? "",
                Difficulty = request.Difficulty > 0 ? request.Difficulty : 1,
                Category = string.IsNullOrEmpty(request.Category) ? "综合" : request.Category,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _dataSync.AddBankQuestionAsync(question);
            return Json(new { success = true });
        }

        public class AddQuestionRequest
        {
            public string Question { get; set; } = string.Empty;
            public string Answer { get; set; } = string.Empty;
            public string Pinyin { get; set; } = string.Empty;
            public string Hint { get; set; } = string.Empty;
            public int Difficulty { get; set; } = 1;
            public string Category { get; set; } = "综合";
        }

        [HttpPost]
        public async Task<IActionResult> BatchAddQuestions([FromBody] BatchAddRequest request)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            if (request.Questions == null || !request.Questions.Any())
                return Json(new { success = false, message = "请至少添加一道题" });

            int successCount = 0;
            int failCount = 0;
            var errors = new List<string>();

            for (int i = 0; i < request.Questions.Count; i++)
            {
                var q = request.Questions[i];
                try
                {
                    if (string.IsNullOrEmpty(q.Question) || string.IsNullOrEmpty(q.Answer))
                    {
                        failCount++;
                        errors.Add($"第{i + 1}行：题目或答案为空");
                        continue;
                    }

                    var question = new BankQuestion
                    {
                        Question = q.Question,
                        Answer = q.Answer,
                        Pinyin = q.Pinyin ?? "",
                        Hint = q.Hint ?? "",
                        Difficulty = q.Difficulty > 0 ? q.Difficulty : 1,
                        Category = string.IsNullOrEmpty(q.Category) ? "综合" : q.Category,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    await _dataSync.AddBankQuestionAsync(question);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    errors.Add($"第{i + 1}行：{ex.Message}");
                }
            }

            return Json(new
            {
                success = true,
                count = successCount,
                failCount = failCount,
                errors = errors.Count > 0 ? string.Join("; ", errors.Take(5)) + (errors.Count > 5 ? $" 等{errors.Count - 5}条错误" : "") : null
            });
        }

        public class BatchAddRequest
        {
            public List<BankQuestionInput> Questions { get; set; } = new();
        }

        public class BankQuestionInput
        {
            public string Question { get; set; } = string.Empty;
            public string Answer { get; set; } = string.Empty;
            public string Pinyin { get; set; } = string.Empty;
            public string Hint { get; set; } = string.Empty;
            public int Difficulty { get; set; } = 1;
            public string Category { get; set; } = "综合";
        }
                
        // ============================================================
        // 🗓️ 更换某天的题目
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> ReplaceQuestion(string date, int newQuestionId)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            if (string.IsNullOrEmpty(date) || newQuestionId <= 0)
                return Json(new { success = false, message = "参数错误" });

            try
            {
                await _dataSync.ExecuteSqlAsync($"DELETE FROM DailyQuestions WHERE Date = '{date}'");

                var sql = $@"INSERT INTO DailyQuestions (
                    QuestionId, Date, CreatedAt
                ) VALUES (
                    {newQuestionId}, '{date}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
                )";

                await _dataSync.ExecuteSqlAsync(sql);

                await _dataSync.ExecuteSqlAsync(
                    $"UPDATE DailyQuestionBank SET UseCount = UseCount + 1, UsedAt = '{DateTime.Now:yyyy-MM-dd HH:mm:ss}' WHERE Id = {newQuestionId}"
                );

                return Json(new { success = true, message = "✅ 题目已更换" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"更换失败: {ex.Message}" });
            }
        }
        private async Task AutoScheduleMissingDaysAsync()
{
    var today = DateTime.Today;
    var random = new Random();

    // 获取所有可用题目
    var allQuestions = await _dataSync.GetAllBankQuestionsAsync();
    var availableQuestions = allQuestions
        .Where(q => q.IsActive)
        .ToList();

    if (!availableQuestions.Any()) return;

    var usedQuestions = new HashSet<int>();

    for (int i = 0; i < 7; i++)
    {
        var date = today.AddDays(i);
        var dateStr = date.ToString("yyyy-MM-dd");

        // 检查当天是否已有安排
        var checkResult = await _dataSync.QueryAsync(
            $"SELECT QuestionId FROM DailyQuestions WHERE Date = '{dateStr}' LIMIT 1"
        );

        if (!checkResult.Contains("\"rows\":[]"))
        {
            var existingId = ParseQuestionIdFromJson(checkResult);
            if (existingId > 0)
            {
                usedQuestions.Add(existingId);
                continue;
            }
        }

        // ⭐ 随机选一道题（优先使用次数少的）
        var candidate = availableQuestions
            .Where(q => !usedQuestions.Contains(q.Id))
            .OrderBy(q => q.UseCount)
            .ThenBy(q => random.Next())  // 随机排序
            .FirstOrDefault();

        // 如果所有题目都用完了，重置
        if (candidate == null)
        {
            usedQuestions.Clear();
            candidate = availableQuestions
                .Where(q => !usedQuestions.Contains(q.Id))
                .OrderBy(q => q.UseCount)
                .ThenBy(q => random.Next())
                .FirstOrDefault();
        }

        if (candidate != null)
        {
            usedQuestions.Add(candidate.Id);

            var sql = $@"INSERT INTO DailyQuestions (
                QuestionId, Date, CreatedAt
            ) VALUES (
                {candidate.Id}, '{dateStr}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
            )";

            await _dataSync.ExecuteSqlAsync(sql);
            Console.WriteLine($"🤖 AI自动安排 {dateStr}: {candidate.Question} (难度: {candidate.Difficulty}⭐)");
        }
    }
}


        // ============================================================
        private DailyQuestion? ParseDailyQuestionFromJson(string json)
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

                    var q = new DailyQuestion();

                    for (int i = 0; i < cols.GetArrayLength(); i++)
                    {
                        var colName = cols[i].GetProperty("name").GetString();
                        var element = row[i];

                        // ⭐ 关键修复：处理 Turso 的 { value: xxx } 格式
                        var value = element;
                        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("value", out var v))
                        {
                            value = v;
                        }

                        if (value.ValueKind == JsonValueKind.Null)
                        {
                            continue;
                        }

                        switch (colName)
                        {
                            case "Id":
                                q.Id = value.ValueKind == JsonValueKind.Number ? value.GetInt32() : 0;
                                break;
                            case "QuestionId":
                                q.QuestionId = value.ValueKind == JsonValueKind.Number ? value.GetInt32() : 0;
                                break;
                            case "Date":
                                q.Date = value.ValueKind == JsonValueKind.String ? DateTime.Parse(value.GetString() ?? "") : DateTime.Now;
                                break;
                            case "Question":
                                q.Question = value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
                                break;
                            case "Answer":
                                q.Answer = value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
                                break;
                            case "Category":
                                q.Category = value.ValueKind == JsonValueKind.String ? value.GetString() ?? "综合" : "综合";
                                break;
                            case "Difficulty":
                                q.Difficulty = value.ValueKind == JsonValueKind.Number ? value.GetInt32() : 1;
                                break;
                        }
                    }
                    return q;
                }
            }
        }
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"解析每日题目失败: {ex.Message}");
        return null;
    }
}


        // ============================================================
        // 辅助类
        // ============================================================

        public class DailyScheduleItem
        {
            public DateTime Date { get; set; }
            public string DateStr { get; set; } = string.Empty;
            public bool IsToday { get; set; }
            public bool IsPast { get; set; }
            public bool IsScheduled { get; set; }
            public int QuestionId { get; set; }
            public string Question { get; set; } = string.Empty;
            public string Answer { get; set; } = string.Empty;
            public string Category { get; set; } = "综合";
            public int Difficulty { get; set; } = 1;
        }
        // ============================================================
// 🗑️ 题库去重（删除重复题目）
// ============================================================

[HttpPost]
public async Task<IActionResult> DeduplicateQuestions()
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
        return Json(new { success = false, message = "权限不足" });

    try
    {
        var allQuestions = await _dataSync.GetAllBankQuestionsAsync();
        var seen = new HashSet<string>();
        var duplicateIds = new List<int>();

        foreach (var q in allQuestions)
        {
            var key = $"{q.Question}_{q.Answer}";
            if (seen.Contains(key))
            {
                duplicateIds.Add(q.Id);
            }
            else
            {
                seen.Add(key);
            }
        }

        if (duplicateIds.Any())
        {
            foreach (var id in duplicateIds)
            {
                await _dataSync.DeleteBankQuestionAsync(id);
            }
        }

        return Json(new
        {
            success = true,
            message = $"✅ 已删除 {duplicateIds.Count} 道重复题目",
            deletedCount = duplicateIds.Count
        });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = $"去重失败: {ex.Message}" });
    }
}
                    
    }  // ⬅️ AdminController 类结束（只有一个）
}  // ⬅️ namespace 结束（只有一个）
