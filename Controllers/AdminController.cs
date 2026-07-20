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
// 头像审核 - 通过
// ============================================================
[HttpPost]
public async Task<IActionResult> ApproveAvatar(int userId)
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
    {
        return Json(new { success = false, message = "权限不足" });
    }

    var user = await _context.Users.FindAsync(userId);
    if (user == null)
    {
        return Json(new { success = false, message = "用户不存在" });
    }

    user.IsAvatarApproved = true;
    await _context.SaveChangesAsync();

    return Json(new { success = true, message = "头像已通过审核" });
}

// ============================================================
// 头像审核 - 拒绝
// ============================================================
[HttpPost]
public async Task<IActionResult> RejectAvatar(int userId)
{
    var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
    if (isAdmin != 1)
    {
        return Json(new { success = false, message = "权限不足" });
    }

    var user = await _context.Users.FindAsync(userId);
    if (user == null)
    {
        return Json(new { success = false, message = "用户不存在" });
    }

    // 清空头像
    user.AvatarUrl = null;
    user.IsAvatarApproved = false;
    user.AvatarSubmittedAt = null;
    await _context.SaveChangesAsync();

    return Json(new { success = true, message = "头像已拒绝" });
}

        // ============================================================
        // 6. 授权码管理
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

        // ============================================================
        // ⭐ 7. 关于我管理
        // ============================================================
        public async Task<IActionResult> About()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return RedirectToAction("Login", "Auth");
            }

            var sections = await _context.AboutMeContents
                .OrderBy(s => s.SortOrder)
                .ToListAsync();

            if (!sections.Any())
            {
                var defaults = new[]
                {
                    new AboutMe { SectionKey = "bio", Title = "👨‍💻 我是谁", Content = "你好！我是 Chris Hopper，一名热爱技术的全栈开发者。这个网站是我用 ASP.NET Core 10.0 打造的个人空间，用于分享技术心得、项目经验和生活感悟。", SortOrder = 1 },
                    new AboutMe { SectionKey = "journey", Title = "📚 学习路线", Content = "2024 - 开始学习 C# 和 .NET 平台\n2025 - 深入学习 ASP.NET Core Web 开发\n2026 - 构建个人网站，持续精进技术", SortOrder = 2 },
                    new AboutMe { SectionKey = "goal", Title = "🎯 我的目标", Content = "成为一名优秀的全栈开发者，用技术创造价值，用代码改变生活。持续学习，不断进步，分享知识，回馈社区。", SortOrder = 3 },
                    new AboutMe { SectionKey = "social", Title = "🔗 社交链接", Content = "github:https://github.com/chrishopper|twitter:https://twitter.com/chrishopper|linkedin:https://linkedin.com/in/chrishopper", SortOrder = 4 }
                };
                _context.AboutMeContents.AddRange(defaults);
                await _context.SaveChangesAsync();
                sections = await _context.AboutMeContents.OrderBy(s => s.SortOrder).ToListAsync();
            }

            return View(sections);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAboutMe([FromBody] Dictionary<string, string> data)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            try
            {
                // 处理社交链接
                if (data.ContainsKey("social_github") || data.ContainsKey("social_twitter") || 
                    data.ContainsKey("social_linkedin") || data.ContainsKey("social_discord"))
                {
                    var socialLinks = new List<string>();
                    if (data.ContainsKey("social_github") && !string.IsNullOrEmpty(data["social_github"]))
                        socialLinks.Add($"github:{data["social_github"]}");
                    if (data.ContainsKey("social_twitter") && !string.IsNullOrEmpty(data["social_twitter"]))
                        socialLinks.Add($"twitter:{data["social_twitter"]}");
                    if (data.ContainsKey("social_linkedin") && !string.IsNullOrEmpty(data["social_linkedin"]))
                        socialLinks.Add($"linkedin:{data["social_linkedin"]}");
                    if (data.ContainsKey("social_discord") && !string.IsNullOrEmpty(data["social_discord"]))
                        socialLinks.Add($"discord:{data["social_discord"]}");

                    var socialSection = await _context.AboutMeContents
                        .FirstOrDefaultAsync(s => s.SectionKey == "social");
                    if (socialSection != null)
                    {
                        socialSection.Content = string.Join("|", socialLinks);
                        socialSection.UpdatedAt = DateTime.Now;
                    }
                }

                // 处理其他内容
                var contentKeys = new[] { "bio", "journey", "goal" };
                foreach (var key in contentKeys)
                {
                    if (data.ContainsKey(key))
                    {
                        var section = await _context.AboutMeContents
                            .FirstOrDefaultAsync(s => s.SectionKey == key);
                        if (section != null)
                        {
                            section.Content = data[key];
                            section.UpdatedAt = DateTime.Now;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
