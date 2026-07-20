using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. 首页
        // ============================================================
        public IActionResult Index()
        {
            var latestBlogs = _context.Blogs
                .OrderByDescending(b => b.PublishDate)
                .Take(3)
                .ToList();

            var projects = _context.Projects.ToList();

            ViewBag.LatestBlogs = latestBlogs;
            ViewBag.Projects = projects;

            return View();
        }

        // ============================================================
        // 2. 关于我
        // ============================================================
        public async Task<IActionResult> About()
        {
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

            var userId = HttpContext.Session.GetInt32("UserId");
            User? currentUser = null;
            if (userId.HasValue)
            {
                currentUser = await _context.Users.FindAsync(userId.Value);
            }

            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            var pendingAvatars = new List<User>();
            if (isAdmin == 1)
            {
                pendingAvatars = await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.AvatarUrl) && !u.IsAvatarApproved)
                    .OrderByDescending(u => u.AvatarSubmittedAt)
                    .ToListAsync();
            }

            ViewBag.Sections = sections;
            ViewBag.CurrentUser = currentUser;
            ViewBag.PendingAvatars = pendingAvatars;

            return View();
        }

        // ============================================================
        // 3. 联系方式页面
        // ============================================================
        public IActionResult Contact()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }

        // ============================================================
        // 4. 申请联系方式
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> RequestContact([FromBody] ContactRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { success = false, message = "请先登录" });
                }

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "用户不存在" });
                }

                if (string.IsNullOrEmpty(request.Platform) ||
                    string.IsNullOrEmpty(request.HowKnowMe) ||
                    string.IsNullOrEmpty(request.Identity) ||
                    string.IsNullOrEmpty(request.Relationship))
                {
                    return Json(new { success = false, message = "请填写所有必填项" });
                }

                var code = GenerateAuthorizationCode();

                var contactRequest = new ContactRequest
                {
                    Platform = request.Platform,
                    AuthorizationCode = code,
                    HowKnowMe = request.HowKnowMe,
                    Identity = request.Identity,
                    Relationship = request.Relationship,
                    Remarks = request.Remarks ?? string.Empty,
                    RequestTime = DateTime.Now,
                    IsApproved = true,
                    IsUsed = false,
                    UserId = user.Id,
                    Username = user.Username,
                    UserEmail = user.Email
                };

                _context.ContactRequests.Add(contactRequest);
                await _context.SaveChangesAsync();

                try
                {
                    var emailService = HttpContext.RequestServices.GetService<EmailService>();
                    if (emailService != null)
                    {
                        await emailService.SendAdminNewContactRequestNotificationAsync(
                            request.Identity,
                            request.Platform,
                            code,
                            request.HowKnowMe,
                            request.Relationship,
                            user.Username,
                            user.Email
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"管理员通知邮件发送失败: {ex.Message}");
                }

                return Json(new { success = true, code = code });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "申请失败，请重试" });
            }
        }

        // ============================================================
        // 5. 管理员查询授权码
        // ============================================================
        public async Task<IActionResult> QueryContact(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return Json(new { success = false, message = "请输入授权码" });
            }

            var request = await _context.ContactRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.AuthorizationCode == code.ToUpper());

            if (request == null)
            {
                return Json(new { success = false, message = "授权码无效或不存在" });
            }

            if (request.IsUsed)
            {
                return Json(new { success = false, message = "该授权码已被使用，请联系管理员" });
            }

            if (!request.IsApproved)
            {
                request.IsApproved = true;
                request.ViewTime = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            string contactInfo = request.Platform == "WeChat"
                ? "💬 微信号：Chris_hopper"
                : "🐧 QQ号：2908685235";

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
                    ContactInfo = contactInfo,
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
        // 6. 上传头像
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { success = false, message = "请先登录" });
                }

                if (avatar == null || avatar.Length == 0)
                {
                    return Json(new { success = false, message = "请选择图片" });
                }

                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(avatar.ContentType))
                {
                    return Json(new { success = false, message = "只支持 JPG, PNG, GIF, WebP 格式" });
                }

                if (avatar.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "图片不能超过 5MB" });
                }

                var fileName = $"{Guid.NewGuid():N}_{avatar.FileName}";
                var uploadPath = Path.Combine("wwwroot", "images", "avatars");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                var avatarUrl = $"/images/avatars/{fileName}";

                // ⭐ 先获取 isAdmin 值
                var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;

                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    if (isAdmin == 1)
                    {
                        user.IsAvatarApproved = true;
                    }
                    else
                    {
                        user.IsAvatarApproved = false;
                    }
                    user.AvatarUrl = avatarUrl;
                    user.AvatarSubmittedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                var message = isAdmin == 1 ? "头像更新成功！" : "头像已提交，等待管理员审核";
                return Json(new { success = true, url = avatarUrl, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // 7. 错误页面
        // ============================================================
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ============================================================
        // 8. 生成授权码
        // ============================================================
        private string GenerateAuthorizationCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            var parts = new string[4];
            for (int i = 0; i < 4; i++)
            {
                char[] part = new char[4];
                for (int j = 0; j < 4; j++)
                {
                    part[j] = chars[random.Next(chars.Length)];
                }
                parts[i] = new string(part);
            }
            return string.Join("-", parts);
        }
    }
}
