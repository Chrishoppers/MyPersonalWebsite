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
// 注册 GET
// ============================================================
[HttpGet]
public IActionResult Register()
{
    return View();
}

// ============================================================
// 注册 POST
// ============================================================
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

    // ⭐ 只检查活跃用户（IsDeleted = 0）
    var existingUser = await _dataSync.GetUserByUsernameAsync(username);
    if (existingUser != null)
    {
        ModelState.AddModelError("", "用户名已被使用，请更换");
        return View();
    }

    existingUser = await _dataSync.GetUserByEmailAsync(email);
    if (existingUser != null)
    {
        ModelState.AddModelError("", "邮箱已被注册，请更换");
        return View();
    }

    var code = new Random().Next(100000, 999999).ToString();

    string? avatarData = null;
    if (avatar != null && avatar.Length > 0)
    {
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (allowedTypes.Contains(avatar.ContentType) && avatar.Length <= 5 * 1024 * 1024)
        {
            using var memoryStream = new MemoryStream();
            await avatar.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();
            var base64 = Convert.ToBase64String(imageBytes);
            avatarData = $"data:{avatar.ContentType};base64,{base64}";

            if (avatarData.Length > 500 * 1024)
            {
                ModelState.AddModelError("", "图片过大，请压缩后上传（最大500KB）");
                return View();
            }
        }
    }

    var newUser = new User
    {
        Username = username,
        Email = email,
        PasswordHash = PasswordHelper.HashPassword(password),
        VerificationCode = code,
        VerificationCodeExpiry = DateTime.Now.AddMinutes(10),
        IsEmailVerified = false,
        IsAdmin = false,
        IsApproved = false,
        IsBanned = false,
        IsDeleted = false,
        CreatedAt = DateTime.Now,
        AvatarUrl = avatarData,
        IsAvatarApproved = false,
        AvatarSubmittedAt = avatarData != null ? DateTime.Now : null
    };

    await _dataSync.AddUserAsync(newUser);

    try
    {
        await _emailService.SendVerificationCodeAsync(email, code);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"验证码邮件发送失败: {ex.Message}");
    }

    TempData["RegisterEmail"] = email;
    TempData["VerificationCode"] = code;
    TempData["VerificationCodeExpiry"] = DateTime.Now.AddMinutes(10);
    TempData["RegisterUserId"] = newUser.Id;

    return RedirectToAction("VerifyEmail");
}
// ============================================================
// 验证邮箱 GET
// ============================================================
[HttpGet]
public IActionResult VerifyEmail()
{
    var email = TempData["RegisterEmail"] as string ?? "";
    Console.WriteLine($"📧 VerifyEmail GET: email={email}");
    ViewBag.Email = email;
    return View();
}

// ============================================================
// 验证邮箱 POST - 修复版（无变量冲突）
// ============================================================
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> VerifyEmail(string email, string code)
{
    if (string.IsNullOrEmpty(email))
    {
        email = TempData["RegisterEmail"] as string ?? "";
    }

    Console.WriteLine($"📧 验证邮箱请求: email={email}, code={code}");

    if (string.IsNullOrEmpty(email))
    {
        ModelState.AddModelError("", "邮箱地址丢失，请重新注册");
        ViewBag.Email = email;
        return View();
    }

    // ⭐ 先从 TempData 读取验证码
    var savedCode = TempData["VerificationCode"] as string ?? "";
    var savedExpiry = TempData["VerificationCodeExpiry"] as DateTime?;

    Console.WriteLine($"🔍 保存的验证码: {savedCode}, 过期时间: {savedExpiry}");

    // 如果 TempData 没有，从数据库查
    if (string.IsNullOrEmpty(savedCode) || savedExpiry == null)
    {
        var dbUser = await _dataSync.GetUserByEmailAsync(email);
        if (dbUser != null)
        {
            savedCode = dbUser.VerificationCode ?? "";
            savedExpiry = dbUser.VerificationCodeExpiry;
        }
    }

    if (string.IsNullOrEmpty(savedCode))
    {
        ModelState.AddModelError("", "验证码不存在，请重新注册");
        ViewBag.Email = email;
        return View();
    }

    if (savedExpiry < DateTime.Now)
    {
        ModelState.AddModelError("", "验证码已过期，请重新注册");
        ViewBag.Email = email;
        return View();
    }

    if (savedCode != code)
    {
        ModelState.AddModelError("", "验证码错误");
        ViewBag.Email = email;
        return View();
    }

    // ===== 验证通过 =====

    // ⭐ 使用不同的变量名，避免冲突
    var foundUser = await _dataSync.GetUserByEmailAsync(email);
    if (foundUser == null)
    {
        ModelState.AddModelError("", "用户不存在，请重新注册");
        ViewBag.Email = email;
        return View();
    }

    foundUser.IsEmailVerified = true;
    foundUser.VerificationCode = null;
    foundUser.VerificationCodeExpiry = null;
    await _dataSync.UpdateUserAsync(foundUser);

    try
    {
        await _emailService.SendAdminNewUserVerificationAsync(
            foundUser.Username,
            foundUser.Email,
            foundUser.Id,
            foundUser.AvatarUrl,
            foundUser.CreatedAt
        );
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
// ⭐ 邮件自动登录（点击链接直接登录）
// ============================================================
[HttpGet]
public async Task<IActionResult> AutoLogin(string token)
{
    if (string.IsNullOrEmpty(token))
    {
        TempData["Message"] = "无效的登录链接";
        return RedirectToAction("Login");
    }

    var user = await _dataSync.GetUserByLoginTokenAsync(token);
    if (user == null)
    {
        TempData["Message"] = "登录链接无效或已过期";
        return RedirectToAction("Login");
    }

    // 检查用户是否被封禁或删除
    if (user.IsDeleted)
    {
        TempData["Message"] = "账号已被删除";
        return RedirectToAction("Login");
    }

    if (user.IsBanned)
    {
        if (user.BanExpiry.HasValue && user.BanExpiry.Value > DateTime.Now)
        {
            TempData["Message"] = $"账号已被封禁至 {user.BanExpiry.Value:yyyy-MM-dd HH:mm}";
            return RedirectToAction("Login");
        }
        else if (user.BanExpiry.HasValue && user.BanExpiry.Value <= DateTime.Now)
        {
            // 封禁已过期，自动解封
            user.IsBanned = false;
            user.BanExpiry = null;
        }
        else
        {
            TempData["Message"] = "账号已被封禁";
            return RedirectToAction("Login");
        }
    }

    // 检查用户是否已审核通过
    if (!user.IsAdmin && !user.IsApproved)
    {
        TempData["Message"] = "账号正在等待管理员审核，请耐心等待";
        return RedirectToAction("Login");
    }

    // 清除Token（一次性使用）
    user.LoginToken = null;
    user.LoginTokenExpiry = null;
    await _dataSync.UpdateUserAsync(user);

    // 登录用户
    HttpContext.Session.SetInt32("UserId", user.Id);
    HttpContext.Session.SetString("Username", user.Username);
    HttpContext.Session.SetString("UserEmail", user.Email);
    HttpContext.Session.SetInt32("IsAdmin", user.IsAdmin ? 1 : 0);

    // 跳转到通知中心
    return RedirectToAction("Notifications", "Home");
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

            // 调试日志
            Console.WriteLine($"🔍 登录尝试: 用户名={user.Username}, IsAdmin={user.IsAdmin}, IsApproved={user.IsApproved}, IsEmailVerified={user.IsEmailVerified}, IsBanned={user.IsBanned}, IsDeleted={user.IsDeleted}");

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

            if (!user.IsEmailVerified)
            {
                ModelState.AddModelError("", "请先验证邮箱");
                return View();
            }

            // ⭐ 检查管理员审核状态（管理员直接跳过）
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
