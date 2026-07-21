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
        private readonly AppDbContext _context;
        private readonly BrevoEmailService _emailService;

        public AdminController(AppDbContext context, BrevoEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ============================================================
        // 1. 仪表盘
        // ============================================================
        public async Task<IActionResult> Dashboard()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserCount = await _context.Users.CountAsync(u => !u.IsDeleted);
            ViewBag.BlogCount = await _context.Blogs.CountAsync();
            ViewBag.MessageCount = await _context.Messages.CountAsync();
            ViewBag.PendingMessages = await _context.Messages.CountAsync(m => !m.IsApproved);
            ViewBag.ContactRequestCount = await _context.ContactRequests.CountAsync();
            ViewBag.PendingContactRequests = await _context.ContactRequests.CountAsync(r => !r.IsUsed && !r.IsApproved);
            ViewBag.PendingChangesCount = await _context.Users
                .Where(u => !u.IsDeleted && (
                    !string.IsNullOrEmpty(u.PendingUsername) ||
                    !string.IsNullOrEmpty(u.PendingEmail) ||
                    (!u.IsAvatarApproved && !string.IsNullOrEmpty(u.AvatarUrl))
                ))
                .CountAsync();

            ViewBag.RecentMessages = await _context.Messages
                .OrderByDescending(m => m.CreateTime)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentContactRequests = await _context.ContactRequests
                .OrderByDescending(r => r.RequestTime)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // ============================================================
        // 2. 博客管理
        // ============================================================
        public async Task<IActionResult> Blogs()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return RedirectToAction("Login", "Auth");
            }

            var blogs = await _context.Blogs
                .OrderByDescending(b => b.PublishDate)
                .ToListAsync();
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
                _context.Blogs.Add(blog);
                await _context.SaveChangesAsync();

                try
                {
                    await _emailService.SendAdminNewBlogNotificationAsync(blog.Title);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"通知邮件发送失败: {ex.Message}");
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

            var blog = await _context.Blogs.FindAsync(id);
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
                _context.Blogs.Update(blog);
                await _context.SaveChangesAsync();
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

            var blog = await _context.Blogs.FindAsync(id);
            if (blog == null)
            {
                return Json(new { success = false, message = "博客不存在" });
            }

            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "删除成功" });
        }

        // ============================================================
        // 3. 博客图片上传
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
        // 4. 留言管理
        // ============================================================
        public async Task<IActionResult> Messages()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return RedirectToAction("Login", "Auth");
            }

            var messages = await _context.Messages
                .Include(m => m.User)
                .OrderByDescending(m => m.CreateTime)
                .ToListAsync();
            return View(messages);
        }

        // ============================================================
        // 5. 用户管理
        // ============================================================
        public async Task<IActionResult> Users()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return RedirectToAction("Login", "Auth");
            }

            var users = await _context.Users
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return View(users);
        }

        // ============================================================
        // 5a. 封禁用户（修复语法错误）
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> BanUser(int id, int hours, string reason, string note)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "用户不存在" });
            }

            if (user.IsAdmin)
            {
                return Json(new { success = false, message = "不能封禁管理员" });
            }

            user.IsBanned = true;

            if (hours > 0)
            {
                user.BanExpiry = DateTime.Now.AddHours(hours);
            }
            else
            {
                user.BanExpiry = null;
            }

            user.BanReason = reason;
            user.BanNote = note;

            a            ViewBag.BlogCount = await _context.Blogs.CountAsync();
            ViewBag.MessageCount = await _context.Messages.CountAsync();
            ViewBag.PendingMessages = await _context.Messages.CountAsync(m => !m.IsApproved);
            ViewBag.ContactRequestCount = await _context.ContactRequests.CountAsync();
            ViewBag.PendingContactRequests = await _context.ContactRequests.CountAsync(r => !r.IsUsed && !r.IsApproved);
            ViewBag.PendingChangesCount = await _context.Users
                .Where(u => !u.IsDeleted && (
                    !string.IsNullOrEmpty(u.PendingUsername) ||
                    !string.IsNullOrEmpty(u.PendingEmail) ||
                    (!u.IsAvatarApproved && !string.IsNullOrEmpty(u.AvatarUrl))
                ))
                .CountAsync();

            ViewBag.RecentMessages = await _context.Messages
                .OrderByDescending(m => m.CreateTime)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentContactRequests = await _context.ContactRequests
                .OrderByDescending(r => r.RequestTime)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // ============================================================
        // 2. 博客管理
        // ============================================================
        public async Task<IActionResult> Blogs()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return RedirectToAction("Login", "Auth");
            }

            var blogs = await _context.Blogs
                .OrderByDescending(b => b.PublishDate)
                .ToListAsync();
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
                _context.Blogs.Add(blog);
                await _context.SaveChangesAsync();

                try
                {
                    await _emailService.SendAdminNewBlogNotificationAsync(blog.Title);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"通知邮件发送失败: {ex.Message}");
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

            var blog = await _context.Blogs.FindAsync(id);
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
                _context.Blogs.Update(blog);
                await _context.SaveChangesAsync();
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

            var blog = await _context.Blogs.FindAsync(id);
            if (blog == null)
            {
                return Json(new { success = false, message = "博客不存在" });
            }

            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "删除成功" });
        }

        // ============================================================
        // 3. 博客图片上传
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
        // 4. 留言管理
        // ============================================================
        public async Task<IActionResult> Messages()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return RedirectToAction("Login", "Auth");
            }

            var messages = await _context.Messages
                .Include(m => m.User)
                .OrderByDescending(m => m.CreateTime)
                .ToListAsync();
            return View(messages);
        }

        // ============================================================
        // 5. 用户管理
        // ============================================================
        public async Task<IActionResult> Users()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return RedirectToAction("Login", "Auth");
            }

            var users = await _context.Users
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> BanUser(int id, int hours, string reason, string note)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var user = await _context.Users.FindAsync(id);
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

            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendUserActionNotificationAsync(
                    user.Email,
                    user.Username,
                    "ban",
                    reason ?? "违反网站规定",
                   
