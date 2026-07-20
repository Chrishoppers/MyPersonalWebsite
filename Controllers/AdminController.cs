using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
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

            ViewBag.UserCount = await _context.Users.CountAsync();
            ViewBag.BlogCount = await _context.Blogs.CountAsync();
            ViewBag.MessageCount = await _context.Messages.CountAsync();
            ViewBag.PendingMessages = await _context.Messages.CountAsync(m => !m.IsApproved);
            ViewBag.ContactRequestCount = await _context.ContactRequests.CountAsync();
            ViewBag.PendingContactRequests = await _context.ContactRequests.CountAsync(r => !r.IsUsed && !r.IsApproved);

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
                    var emailService = HttpContext.RequestServices.GetService<EmailService>();
                    if (emailService != null)
                    {
                        await emailService.SendAdminNewBlogNotificationAsync(blog.Title);
                    }
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
        // 3. 留言管理
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
        // 4. 用户管理
        // ============================================================
        public async Task<IActionResult> Users()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return RedirectToAction("Login", "Auth");
            }

            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> BanUser(int id, int hours, string reason)
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

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"已封禁用户 {user.Username}" });
        }

        [HttpPost]
        public async Task<IActionResult> UnbanUser(int id)
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

            user.IsBanned = false;
            user.BanExpiry = null;
            user.BanReason = null;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"已解封用户 {user.Username}" });
        }

        // ============================================================
        // 5. 授权码管理
        // ============================================================
        public async Task<IActionResult> ContactRequests()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return RedirectToAction("Login", "Auth");
            }

            var requests = await _context.ContactRequests
                .Include(r => r.User)
                .OrderByDescending(r => r.RequestTime)
                .ToListAsync();
            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> MarkContactUsed(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var request = await _context.ContactRequests.FindAsync(id);
            if (request == null)
            {
                return Json(new { success = false, message = "记录不存在" });
            }

            if (request.IsUsed)
            {
                return Json(new { success = false, message = "已被标记为已使用" });
            }

            request.IsUsed = true;
            request.UsedTime = DateTime.Now;
            request.UsedBy = HttpContext.Session.GetString("Username") ?? "admin";
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "已标记为已使用" });
        }

        [HttpPost]
        public async Task<IActionResult> UnmarkContactUsed(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var request = await _context.ContactRequests.FindAsync(id);
            if (request == null)
            {
                return Json(new { success = false, message = "记录不存在" });
            }

            if (!request.IsUsed)
            {
                return Json(new { success = false, message = "该记录未被标记为已使用" });
            }

            request.IsUsed = false;
            request.UsedTime = null;
            request.UsedBy = null;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "已撤销使用标记" });
        }

        [HttpGet]
        public async Task<IActionResult> ContactDetail(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var request = await _context.ContactRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return Json(new { success = false, message = "记录不存在" });
            }

            if (!request.IsApproved)
            {
                request.IsApproved = true;
                request.ViewTime = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    request.Platform,
                    request.AuthorizationCode,
                    request.HowKnowMe,
                    request.Identity,
                    request.Relationship,
                    request.Remarks,
                    request.RequestTime,
                    request.IsApproved,
                    request.ViewTime,
                    request.IsUsed,
                    request.UsedTime,
                    request.UsedBy,
                    User = new
                    {
                        request.Username,
                        request.UserEmail,
                        UserId = request.UserId
                    }
                }
            });
        }
    }
}