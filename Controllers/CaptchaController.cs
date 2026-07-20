using Microsoft.AspNetCore.Mvc;
using MyPersonalWebsite.Services;
using System;

namespace MyPersonalWebsite.Controllers
{
    public class CaptchaController : Controller
    {
        private readonly CaptchaImageService _captchaService;

        public CaptchaController(CaptchaImageService captchaService)
        {
            _captchaService = captchaService;
        }

        // 生成验证码图片
        public IActionResult Index()
        {
            // 1. 生成随机文本
            var captchaText = _captchaService.GenerateCaptchaText(5);

            // 2. 存入 Session（用于验证）
            HttpContext.Session.SetString("CaptchaText", captchaText);

            // 3. 生成图片
            var imageBytes = _captchaService.GenerateCaptchaImage(captchaText);

            // 4. 返回图片
            return File(imageBytes, "image/png");
        }

        // 刷新验证码（前端 AJAX 调用）
        public IActionResult Refresh()
        {
            return Index();
        }
    }
}