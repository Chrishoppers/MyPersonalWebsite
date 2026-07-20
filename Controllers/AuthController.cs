using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Helpers;
using MyPersonalWebsite.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly CaptchaImageService _captchaImageService;
        private readonly RateLimitService _rateLimitService;

        public AuthController(
            AppDbContext context,
            EmailService emailService,
            CaptchaImageService captchaImageService,
            RateLimitService rateLimitService)
        {
            _context = context;
            _emailService = emailService;
            _captchaImageService = captchaImageService;
            _rateLimitService = rateLimitService;
        }

        // ============================================================
        // 1. 注册
        // ============================================================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password, string captchaAnswer)
        {
            // IP限流检查
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!_rateLimitService.CanRegister(clientIp))
            {
                var remainMinutes = _rateLimitService.GetRemainingMinutes(clientIp);
                ModelState.AddModelError("", $"注册尝试过于频繁，请等待 {remainMinutes} 分钟后再试");
                return View();
            }

            // 验证图形验证码
            var storedCaptcha = HttpContext.Session.GetString("CaptchaText");
            if (string.IsNullOrEmpty(storedCaptcha) || !storedCaptcha.Equals(captchaAnswer?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "验证码错误，请重新输入");
                return View();
            }

            // 验证码使用后立即清除
            HttpContext.Session.Remove("CaptchaText");

            // 检查用户名是否已存在
            if (_context.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError("", "用户名已被使用");
                return View();
            }

            // 检查邮箱是否已存在
            if (_context.Users.Any(u => u.Email == email))
            {
                ModelState.AddModelError("", "邮箱已被注册");
                return View();
            }

            // 生成6位验证码
            var code = new Random().Next(100000, 999999).ToString();

            // 创建用户
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(password),
                VerificationCode = code,
                VerificationCodeExpiry = DateTime.Now.AddMinutes(10),
                IsEmailVerified = false,
                IsAdmin = false,
                IsBanned = false,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 发送管理员通知邮件（新用户注册）
            try
            {
                await _emailService.SendAdminNewUserNotificationAsync(username, email);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"管理员通知邮件发送失败: {ex.Message}");
            }

            // 发送验证码邮件
            await _emailService.SendVerificationCodeAsync(email, code);

            TempData["RegisterEmail"] = email;
            return RedirectToAction("VerifyEmail", new { email = email });
        }

        // ============================================================
        // 2. 邮箱验证
        // ============================================================

        [HttpGet]
        public IActionResult VerifyEmail(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(string email, string code)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError("", "用户不存在");
                return View();
            }

            if (user.IsEmailVerified)
            {
                TempData["Message"] = "邮箱已验证，请登录";
                return RedirectToAction("Login");
            }

            if (user.VerificationCode != code)
            {
                ModelState.AddModelError("", "验证码错误");
                return View();
            }

            if (user.VerificationCodeExpiry < DateTime.Now)
            {
                ModelState.AddModelError("", "验证码已过期，请重新注册");
                return View();
            }

            // 验证成功
            user.IsEmailVerified = true;
            user.VerificationCode = null;
            user.VerificationCodeExpiry = null;
            await _context.SaveChangesAsync();

            TempData["Message"] = "邮箱验证成功！请登录";
            return RedirectToAction("Login");
        }

        // ============================================================
        // 3. 登录
        // ============================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username || u.Email == username);
            if (user == null)
            {
                ModelState.AddModelError("", "用户名或密码错误");
                return View();
            }

            if (user.IsBanned)
            {
                string banMessage = "您的账号已被封禁";
                if (user.BanExpiry.HasValue)
                {
                    if (user.BanExpiry.Value > DateTime.Now)
                    {
                        banMessage += $"，将于 {user.BanExpiry.Value.ToString("yyyy-MM-dd HH:mm")} 解封";
                    }
                    else
                    {
                        // 封禁已过期，自动解封
                        user.IsBanned = false;
                        user.BanExpiry = null;
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // 永久封禁
                    ModelState.AddModelError("", banMessage);
                    return View();
                }
            }

            if (!user.IsEmailVerified)
            {
                ModelState.AddModelError("", "请先验证邮箱后再登录");
                return View();
            }

            if (!PasswordHelper.VerifyPassword(password, user.PasswordHash))
            {
                ModelState.AddModelError("", "用户名或密码错误");
                return View();
            }

            // 更新最后登录时间
            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // 保存登录状态到 Session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetInt32("IsAdmin", user.IsAdmin ? 1 : 0);

            if (user.IsAdmin)
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        // ============================================================
        // 4. 退出登录
        // ============================================================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}