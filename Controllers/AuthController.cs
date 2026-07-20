using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Helpers;
using MyPersonalWebsite.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace MyPersonalWebsite.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly SvgCaptchaService _captchaService;
        private readonly RateLimitService _rateLimitService;

        public AuthController(
            AppDbContext context,
            EmailService emailService,
            SvgCaptchaService captchaService,
            RateLimitService rateLimitService)
        {
            _context = context;
            _emailService = emailService;
            _captchaService = captchaService;
            _rateLimitService = rateLimitService;
        }
        // ============================================================
// 忘记密码
// ============================================================

// 显示忘记密码页面
[HttpGet]
public IActionResult ForgotPassword()
{
    return View();
}

// 发送重置邮件
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ForgotPassword(string email)
{
    if (string.IsNullOrEmpty(email))
    {
        ModelState.AddModelError("", "请输入邮箱");
        return View();
    }

    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user == null)
    {
        ModelState.AddModelError("", "该邮箱未注册");
        return View();
    }

    // 生成6位数字验证码
    var token = new Random().Next(100000, 999999).ToString();

    // 保存到数据库
    var reset = new PasswordReset
    {
        UserId = user.Id,
        Token = token,
        Email = user.Email,
        CreatedAt = DateTime.Now,
        ExpiresAt = DateTime.Now.AddMinutes(10),
        IsUsed = false
    };

    _context.PasswordResets.Add(reset);
    await _context.SaveChangesAsync();

    // ⭐ 后台发送邮件（不等待）
    _ = Task.Run(async () =>
    {
        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"邮件发送失败: {ex.Message}");
        }
    });

    ViewBag.Message = "验证码已发送到您的邮箱，请查收（如未收到请检查垃圾箱）";
    ViewBag.Email = user.Email;
    return View("ResetPassword");
}

// 显示重置密码页面
[HttpGet]
public IActionResult ResetPassword()
{
    return View();
}

// 重置密码
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ResetPassword(string email, string token, string newPassword)
{
    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword))
    {
        ModelState.AddModelError("", "请填写完整信息");
        return View();
    }

    if (newPassword.Length < 6)
    {
        ModelState.AddModelError("", "密码至少6位");
        return View();
    }

    var reset = await _context.PasswordResets
        .FirstOrDefaultAsync(r => r.Email == email && r.Token == token && !r.IsUsed);

    if (reset == null)
    {
        ModelState.AddModelError("", "验证码无效或已使用");
        return View();
    }

    if (reset.ExpiresAt < DateTime.Now)
    {
        ModelState.AddModelError("", "验证码已过期，请重新获取");
        return View();
    }

    // 更新密码
    var user = await _context.Users.FindAsync(reset.UserId);
    if (user != null)
    {
        user.PasswordHash = PasswordHelper.HashPassword(newPassword);
        await _context.SaveChangesAsync();
    }

    // 标记验证码已使用
    reset.IsUsed = true;
    await _context.SaveChangesAsync();

    TempData["Message"] = "密码重置成功！请用新密码登录";
    return RedirectToAction("Login");
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

            // ⭐ 验证 SVG 验证码
            if (!_captchaService.VerifyCaptcha(captchaAnswer))
            {
                ModelState.AddModelError("", "验证码错误，请重新输入");
                return View();
            }

            // 清除验证码（防止重复使用）
            HttpContext.Session.Remove("SvgCaptchaText");

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
