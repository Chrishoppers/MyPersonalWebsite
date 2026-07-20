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
        private readonly SvgCaptchaService _captchaService;  // ⭐ 替换
        private readonly RateLimitService _rateLimitService;

        public AuthController(
            AppDbContext context,
            EmailService emailService,
            SvgCaptchaService captchaService,   // ⭐ 替换
            RateLimitService rateLimitService)
        {
            _context = context;
            _emailService = emailService;
            _captchaService = captchaService;
            _rateLimitService = rateLimitService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password, string captchaAnswer)
        {
            // IP限流
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

            // ... 其余注册逻辑保持不变 ...
        }
    }
}
