using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using System;
using System.Diagnostics;
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
        // 首页
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
        // 关于我
        // ============================================================
        public IActionResult About()
        {
            return View();
        }

        // ============================================================
        // 联系方式页面（需要登录）
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
        // 申请联系方式（POST）- 需要登录
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

                // 生成授权码
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
                    IsApproved = true,      // ⭐ 自动通过
                    IsUsed = false,
                    UserId = user.Id,
                    Username = user.Username,
                    UserEmail = user.Email
                };

                _context.ContactRequests.Add(contactRequest);
                await _context.SaveChangesAsync();

                // 发送管理员通知
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
        // 管理员查询授权码（GET）
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

            // 如果已使用，拒绝查询
            if (request.IsUsed)
            {
                return Json(new { success = false, message = "该授权码已被使用，请联系管理员" });
            }

            // 标记为已查看
            if (!request.IsApproved)
            {
                request.IsApproved = true;
                request.ViewTime = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            // 根据平台返回对应的联系方式
            string contactInfo = request.Platform == "WeChat"
                ? "💬 微信号：chris_hopper_wechat"
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
                    // ⭐ 新增：申请人信息
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
        // 错误页面
        // ============================================================
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ============================================================
        // 私有方法：生成授权码
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