using Microsoft.AspNetCore.Mvc;
using MyPersonalWebsite.Services;
using System;

namespace MyPersonalWebsite.Controllers
{
    public class CaptchaController : Controller
    {
        private readonly SvgCaptchaService _svgCaptchaService;

        public CaptchaController(SvgCaptchaService svgCaptchaService)
        {
            _svgCaptchaService = svgCaptchaService;
        }

        // 返回 SVG 验证码图片
        public IActionResult Index()
        {
            var text = _svgCaptchaService.GenerateAndStoreCaptcha();
            var svg = _svgCaptchaService.GenerateSvg(text);

            // 返回 SVG 格式
            return Content(svg, "image/svg+xml");
        }

        // 刷新验证码（前端 AJAX 调用）
        public IActionResult Refresh()
        {
            return Index();
        }
    }
}
