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
        // 仪表盘
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
        // 博客管理
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
        // 留言管理
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
        // 用户管理
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
        // 授权码管理
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
        // 待审核更改
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
        // 头像审核
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> ApproveAvatar(int userId)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "用户不存在" });

            user.IsAvatarApproved = true;
            await _dataSync.UpdateUserAsync(user);
            return Json(new { success = true, message = "头像已通过" });
        }

        [HttpPost]
        public async Task<IActionResult> RejectAvatar(int userId)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "用户不存在" });

            user.AvatarUrl = null;
            user.AvatarSubmittedAt = null;
            user.IsAvatarApproved = false;
            await _dataSync.UpdateUserAsync(user);
            return Json(new { success = true, message = "头像已拒绝" });
        }
        // ============================================================
// 审核用户（通过/拒绝）
// ============================================================

[HttpGet]
public async Task<IActionResult> ApproveUser(int userId)
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
        return Content("权限不足，请登录管理员账号");

    var user = await _dataSync.GetUserByIdAsync(userId);
    if (user == null)
        return Content("用户不存在");

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

    return Content("✅ 用户已通过审核！用户将收到通知邮件。");
}

[HttpGet]
public async Task<IActionResult> RejectUser(int userId)
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
        return Content("权限不足，请登录管理员账号");

    var user = await _dataSync.GetUserByIdAsync(userId);
    if (user == null)
        return Content("用户不存在");

    // 软删除或标记拒绝
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

    return Content("❌ 已拒绝该用户。用户将收到通知邮件。");
}

        // ============================================================
// 关于我编辑
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

        // 更新普通字段
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
                Console.WriteLine($"✅ AboutMe {key} 已更新");
            }
        }

        // 更新社交链接
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
            Console.WriteLine($"✅ 社交链接已更新");
        }

        return Json(new { success = true, message = "保存成功" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ AboutMe 保存失败: {ex.Message}");
        return Json(new { success = false, message = ex.Message });
    }
}

        // ============================================================
// 审核用户（通过）- 无需登录
// ============================================================

[HttpGet]
public async Task<IActionResult> ApproveUser(int userId)
{
    var user = await _dataSync.GetUserByIdAsync(userId);
    if (user == null)
    {
        return Content("❌ 用户不存在");
    }

    // 防止重复审核
    if (user.IsApproved)
    {
        return Content("ℹ️ 该用户已审核通过，无需重复操作");
    }

    // 更新用户状态
    user.IsApproved = true;
    user.IsAvatarApproved = true;
    await _dataSync.UpdateUserAsync(user);

    // 发送通知邮件给用户
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

    return Content("✅ 用户已通过审核！用户将收到通知邮件。");
}

// ============================================================
// 审核用户（拒绝）- 无需登录
// ============================================================

[HttpGet]
public async Task<IActionResult> RejectUser(int userId)
{
    var user = await _dataSync.GetUserByIdAsync(userId);
    if (user == null)
    {
        return Content("❌ 用户不存在");
    }

    // 防止重复操作
    if (user.IsDeleted)
    {
        return Content("ℹ️ 该用户已被处理，无需重复操作");
    }

    // 软删除用户
    user.IsDeleted = true;
    user.DeletedAt = DateTime.Now;
    user.DeleteReason = "管理员审核拒绝";
    await _dataSync.UpdateUserAsync(user);

    // 发送通知邮件给用户
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

    return Content("❌ 已拒绝该用户。用户将收到通知邮件。");
}

        // ============================================================
        // 用户操作
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
