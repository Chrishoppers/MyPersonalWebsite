using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Helpers;
using MyPersonalWebsite.Services;
using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MyPersonalWebsite.Controllers
{
    public class AuthController : Controller
    {
        private readonly DataSyncService _dataSync;
        private readonly BrevoEmailService _emailService;
        private readonly SvgCaptchaService _captchaService;
        private readonly RateLimitService _rateLimitService;
        private readonly AppDbContext _context;

        public AuthController(
            DataSyncService dataSync,
            BrevoEmailService emailService,
            SvgCaptchaService captchaService,
            RateLimitService rateLimitService,
            AppDbContext context)
        {
            _dataSync = dataSync;
            _emailService = emailService;
            _captchaService = captchaService;
            _rateLimitService = rateLimitService;
            _context = context;
        }

        // ============================================================
        // 注册
        // ============================================================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password, string captchaAnswer, IFormFile? avatar)
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

            // 处理头像上传
            string? avatarUrl = null;
            if (avatar != null && avatar.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (allowedTypes.Contains(avatar.ContentType) && avatar.Length <= 5 * 1024 * 1024)
                {
                    var fileName = $"{Guid.NewGuid():N}_{avatar.FileName}";
                    var uploadPath = Path.Combine("wwwroot", "images", "avatars");
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await avatar.CopyToAsync(stream);
                    }
                    avatarUrl = $"/images/avatars/{fileName}";
                }
            }

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
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                AvatarUrl = avatarUrl,
                IsAvatarApproved = false,
                AvatarSubmittedAt = avatarUrl != null ? DateTime.Now : null
            };

            await _dataSync.AddUserAsync(user);

            // 发送验证码邮件
            try
            {
                await _emailService.SendVerificationCodeAsync(email, code);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"验证码邮件发送失败: {ex.Message}");
            }

            TempData["RegisterEmail"] = email;
            TempData["RegisterUserId"] = user.Id;
            return RedirectToAction("VerifyEmail", new { email = email });
        }

        // ============================================================
        // 验证邮箱
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
            var user = await _dataSync.GetUserByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError("", "用户不存在");
                return View();
            }

            if (user.IsEmailVerified)
            {
                TempData["Message"] = "邮箱已验证，请等待管理员审核";
                return RedirectToAction("RegisterSuccess");
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

            // ⭐ 邮箱验证通过，但还需要管理员审核
            user.IsEmailVerified = true;
            user.VerificationCode = null;
            user.VerificationCodeExpiry = null;

            await _dataSync.UpdateUserAsync(user);

            // 发送管理员审核通知邮件
try
{
    await _emailService.SendAdminVerificationRequestAsync(user.Username, user.Email, user.Id, user.AvatarUrl);
}
catch (Exception ex)
{
    Console.WriteLine($"管理员审核邮件发送失败: {ex.Message}");
}

            TempData["RegisterEmail"] = email;
            return RedirectToAction("RegisterSuccess");
        }

        // ============================================================
        // 注册成功页面（等待管理员审核）
        // ============================================================
        [HttpGet]
        public IActionResult RegisterSuccess()
        {
            return View();
        }

        // ============================================================
        // 登录
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

            // 检查是否被软删除
            if (user.IsDeleted)
            {
                ModelState.AddModelError("", "账号已被删除");
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

            // ⭐ 检查邮箱是否已验证
            if (!user.IsEmailVerified)
            {
                ModelState.AddModelError("", "请先验证邮箱");
                return View();
            }

            // ⭐ 管理员跳过邮箱审核，普通用户检查审核状态
            if (!user.IsAdmin && !user.IsApproved)
            {
                ModelState.AddModelError("", "您的账号正在等待管理员审核，请耐心等待");
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

        // ============================================================
        // 登出
        // ============================================================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ============================================================
        // 修改密码
        // ============================================================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ModelState.AddModelError("", "请填写所有字段");
                return View();
            }

            if (newPassword.Length < 6)
            {
                ModelState.AddModelError("", "新密码至少6位");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "两次输入的密码不一致");
                return View();
            }

            var user = await _dataSync.GetUserByIdAsync(userId.Value);
            if (user == null)
            {
                ModelState.AddModelError("", "用户不存在");
                return View();
            }

            if (!PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("", "当前密码错误");
                return View();
            }

            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            await _dataSync.UpdateUserAsync(user);

            HttpContext.Session.Clear();
            TempData["Message"] = "✅ 密码修改成功！请使用新密码重新登录";
            return RedirectToAction("Login");
        }

        // ============================================================
        // 忘记密码
        // ============================================================
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

            _context.PasswordResets.Add(reset);
            await _context.SaveChangesAsync();

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

            var user = await _dataSync.GetUserByIdAsync(reset.UserId);
            if (user == null)
            {
                ModelState.AddModelError("", "用户不存在");
                return View();
            }

            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            await _dataSync.UpdateUserAsync(user);

            reset.IsUsed = true;
            await _context.SaveChangesAsync();

            TempData["Message"] = "密码重置成功！请用新密码登录";
            return RedirectToAction("Login");
        }
    }
}
