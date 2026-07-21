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
            {
                return RedirectToAction("Login", "Auth");
            }

            var users = await _dataSync.GetAllUsersAsync();
            var blogs = await _dataSync.GetBlogsAsync();
            var messages = await _dataSync.GetMessagesAsync();

            ViewBag.UserCount = users.Count(u => !u.IsDeleted);
            ViewBag.BlogCount = blogs.Count;
            ViewBag.MessageCount = messages.Count;
            ViewBag.PendingMessages = messages.Count(m => !m.IsApproved);
            ViewBag.ContactRequestCount = 0;
            ViewBag.PendingContactRequests = 0;
            ViewBag.PendingChangesCount = users.Count(u =>
                !u.IsDeleted && (
                    !string.IsNullOrEmpty(u.PendingUsername) ||
                    !string.IsNullOrEmpty(u.PendingEmail) ||
                    (!u.IsAvatarApproved && !string.IsNullOrEmpty(u.AvatarUrl))
                ));

            ViewBag.RecentMessages = messages.OrderByDescending(m => m.CreateTime).Take(5).ToList();

            return View();
        }
    }
}
// ============================================================
// 博客管理
// ============================================================
public async Task<IActionResult> Blogs()
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
    {
        return RedirectToAction("Login", "Auth");
    }

    var blogs = await _dataSync.GetBlogsAsync();
    return View(blogs);
}

[HttpGet]
public IActionResult CreateBlog()
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
    {
        return RedirectToAction("Login", "Auth");
    }
    return View();
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateBlog(Blog blog)
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
    {
        return RedirectToAction("Login", "Auth");
    }

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
    {
        return RedirectToAction("Login", "Auth");
    }

    var blog = await _dataSync.GetBlogByIdAsync(id);
    if (blog == null)
    {
        return NotFound();
    }
    return View(blog);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditBlog(Blog blog)
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
    {
        return RedirectToAction("Login", "Auth");
    }

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
    {
        return Json(new { success = false, message = "权限不足" });
    }

    await _dataSync.DeleteBlogAsync(id);
    return Json(new { success = true, message = "删除成功" });
}
// ============================================================
// 博客图片上传
// ============================================================
[HttpPost]
public async Task<IActionResult> UploadBlogImage(IFormFile image)
{
    try
    {
        if (image == null || image.Length == 0)
        {
            return Json(new { success = false, message = "请选择图片" });
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(image.ContentType))
        {
            return Json(new { success = false, message = "只支持 JPG, PNG, GIF, WebP 格式" });
        }

        if (image.Length > 5 * 1024 * 1024)
        {
            return Json(new { success = false, message = "图片大小不能超过 5MB" });
        }

        var fileName = $"{Guid.NewGuid():N}_{image.FileName}";
        var uploadPath = Path.Combine("wwwroot", "images", "blog");

        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        var filePath = Path.Combine(uploadPath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        var imageUrl = $"/images/blog/{fileName}";
        return Json(new { success = true, url = imageUrl });
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
    {
        return RedirectToAction("Login", "Auth");
    }

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
    {
        return RedirectToAction("Login", "Auth");
    }

    var users = await _dataSync.GetAllUsersAsync();
    return View(users.Where(u => !u.IsDeleted).OrderByDescending(u => u.CreatedAt).ToList());
}
// ============================================================
// 封禁用户
// ============================================================
[HttpPost]
public async Task<IActionResult> BanUser(int id, int hours, string reason, string note)
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
    {
        return Json(new { success = false, message = "权限不足" });
    }

    var user = await _dataSync.GetUserByIdAsync(id);
    if (user == null)
    {
        return Json(new { success = false, message = "用户不存在" });
    }

    if (user.IsAdmin)
    {
        return Json(new { success = false, message = "不能封禁管理员" });
    }

    user.IsBanned = true;
    user.BanExpiry = hours > 0 ? DateTime.Now.AddHours(hours) : (DateTime?)null;
    user.BanReason = reason;
    user.BanNote = note;

    await _dataSync.UpdateUserAsync(user);

    try
    {
        await _emailService.SendUserActionNotificationAsync(
            user.Email,
            user.Username,
            "ban",
            reason ?? "违反网站规定",
            note ?? "无"
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"邮件发送失败: {ex.Message}");
    }

    return Json(new { success = true, message = $"已封禁用户 {user.Username}" });
}

// ============================================================
// 解封用户
// ============================================================
[HttpPost]
public async Task<IActionResult> UnbanUser(int id)
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
    {
        return Json(new { success = false, message = "权限不足" });
    }

    var user = await _dataSync.GetUserByIdAsync(id);
    if (user == null)
    {
        return Json(new { success = false, message = "用户不存在" });
    }

    user.IsBanned = false;
    user.BanExpiry = null;
    user.BanReason = null;
    user.BanNote = null;

    await _dataSync.UpdateUserAsync(user);

    try
    {
        await _emailService.SendUserActionNotificationAsync(
            user.Email,
            user.Username,
            "unban",
            "管理员已解封您的账号",
            null
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"邮件发送失败: {ex.Message}");
    }

    return Json(new { success = true, message = $"已解封用户 {user.Username}" });
}
        // ============================================================
        // 删除用户
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id, string reason, string note)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var user = await _dataSync.GetUserByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "用户不存在" });
            }

            if (user.IsAdmin)
            {
                return Json(new { success = false, message = "不能删除管理员" });
            }

            user.IsDeleted = true;
            user.DeletedAt = DateTime.Now;
            user.DeleteReason = reason;
            user.DeleteNote = note;

            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email,
                    user.Username,
                    "delete",
                    reason ?? "违反网站规定",
                    note ?? "无"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return Json(new { success = true, message = $"已删除用户 {user.Username}" });
        }

        // ============================================================
        // 激活用户
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> ActivateUser(int userId)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "用户不存在" });
            }

            user.IsEmailVerified = true;
            await _dataSync.UpdateUserAsync(user);

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email,
                    user.Username,
                    "activate",
                    "管理员已激活您的账号",
                    null
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
            }

            return Json(new { success = true, message = "用户已激活" });
        }
    }
}
