using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Helpers;
using MyPersonalWebsite.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Controllers
{
    public class AuthController : Controller
    {
        private readonly DataSyncService _dataSync;
        private readonly BrevoEmailService _emailService;
        private readonly SvgCaptchaService _captchaService;
        private readonly RateLimitService _rateLimitService;

        public AuthController(
            DataSyncService dataSync,
            BrevoEmailService emailService,
            SvgCaptchaService captchaService,
            RateLimitService rateLimitService)
        {
            _dataSync = dataSync;
            _emailService = emailService;
            _captchaService = captchaService;
            _rateLimitService = rateLimitService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password, string captchaAnswer)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!_rateLimitService.CanRegister(clientIp))
            {
                var remainMinutes = _rateLimitService.GetRemainingMinutes(clientIp);
                ModelState.AddModelError("", $"注册尝试过于频繁，请等待 {remainMinutes} 分钟后再试");
                return View();
            }

            if (!_captchaService.VerifyCaptcha(captchaAnswer))
            {
                ModelState.AddModelError("", "验证码错误，请重新输入");
                return View();
            }
            HttpContext.Session.Remove("SvgCaptchaText");

            var existingUser = await _dataSync.GetUserByUsernameAsync(username);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "用户名已被使用");
                return View();
            }

            existingUser = await _dataSync.GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "邮箱已被注册");
                return View();
            }

            var code = new Random().Next(100000, 999999).ToString();

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

            await _dataSync.AddUserAsync(user);

            try
            {
                await _emailService.SendAdminNewUserNotificationAsync(username, email);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"管理员通知邮件发送失败: {ex.Message}");
            }

            await _emailService.SendVerificationCodeAsync(email, code);

            TempData["RegisterEmail"] = email;
            return RedirectToAction("VerifyEmail", new { email = email });
        }

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
            var user = await _dataSync.GetUserByEmailAsync(email);
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

            user.IsEmailVerified = true;
            user.VerificationCode = null;
            user.VerificationCodeExpiry = null;

            await _dataSync.UpdateUserAsync(user);

            TempData["Message"] = "邮箱验证成功！请登录";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _dataSync.GetUserByUsernameAsync(username);
            if (user == null)
            {
                user = await _dataSync.GetUserByEmailAsync(username);
            }

            if (user == null)
            {
                ModelState.AddModelError("", "用户名或密码错误");
                return View();
            }

            if (user.IsBanned)
            {
                string banMessage = "您的账号已被封禁";
                if (user.BanExpiry.HasValue && user.BanExpiry.Value > DateTime.Now)
                {
                    banMessage += $"，将于 {user.BanExpiry.Value.ToString("yyyy-MM-dd HH:mm")} 解封";
                }
                else if (user.BanExpiry.HasValue && user.BanExpiry.Value <= DateTime.Now)
                {
                    user.IsBanned = false;
                    user.BanExpiry = null;
                    await _dataSync.UpdateUserAsync(user);
                }
                else
                {
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

            user.LastLoginAt = DateTime.Now;
            await _dataSync.UpdateUserAsync(user);

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

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "请输入邮箱");
                return View();
            }

            var user = await _dataSync.GetUserByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError("", "该邮箱未注册");
                return View();
            }

            var token = new Random().Next(100000, 999999).ToString();

            var reset = new PasswordReset
            {
                UserId = user.Id,
                Token = token,
                Email = user.Email,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(10),
                IsUsed = false
            };

            // TODO: 保存 reset 到数据库

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, token);
                ViewBag.Message = "验证码已发送到您的邮箱，请查收";
                ViewBag.Email = user.Email;
                return View("ResetPassword");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
                ViewBag.Message = $"邮件发送失败，请联系管理员（验证码：{token}）";
                ViewBag.Email = user.Email;
                return View("ResetPassword");
            }
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }

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

    // ⭐ 查找有效的重置记录
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

    // ⭐ 更新用户密码
    var user = await _dataSync.GetUserByIdAsync(reset.UserId);
    if (user == null)
    {
        ModelState.AddModelError("", "用户不存在");
        return View();
    }

    user.PasswordHash = PasswordHelper.HashPassword(newPassword);
    await _dataSync.UpdateUserAsync(user);  // ⭐ 双写

    // ⭐ 标记验证码已使用
    reset.IsUsed = true;
    await _context.SaveChangesAsync();

    TempData["Message"] = "密码重置成功！请用新密码登录";
    return RedirectToAction("Login");
}
    }
}
