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

            // 生成登录Token
            var loginToken = await _dataSync.CreateLoginTokenAsync(userId);

            // 发送邮件
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

            // 保存通知到数据库
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

        // ============================================================
        // 2.1 创建博客 - GET
        // ============================================================
        [HttpGet]
        public IActionResult CreateBlog()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");
            return View();
        }

        // ============================================================
        // 2.2 创建博客 - POST
        // ============================================================
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

        // ============================================================
        // 2.3 编辑博客 - GET
        // ============================================================
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

        // ============================================================
        // 2.4 编辑博客 - POST
        // ============================================================
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

        // ============================================================
        // 2.5 删除博客
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            await _dataSync.DeleteBlogAsync(id);
            return Json(new { success = true, message = "删除成功" });
        }

        // ============================================================
        // 2.6 上传博客图片
        // ============================================================
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

        // ============================================================
        // 5.1 标记授权码已使用
        // ============================================================
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

        // ============================================================
        // 5.2 撤销授权码使用标记
        // ============================================================
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

        // ============================================================
        // 5.3 授权码详情
        // ============================================================
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

        // ============================================================
        // 6.1 批准用户更改
        // ============================================================
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

        // ============================================================
        // 6.2 拒绝用户更改
        // ============================================================
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

        // ============================================================
        // 17.1 保存关于我
        // ============================================================
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
    }
}
