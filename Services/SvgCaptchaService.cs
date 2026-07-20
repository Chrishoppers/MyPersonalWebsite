using Microsoft.AspNetCore.Http;
using System;
using System.Text;

namespace MyPersonalWebsite.Services
{
    public class SvgCaptchaService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SvgCaptchaService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // 生成随机验证码文本（5位，排除易混淆字符）
        public string GenerateCaptchaText()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            var result = new char[5];
            for (int i = 0; i < 5; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }

        // 生成 SVG 验证码图片（返回 HTML 格式的 SVG）
        public string GenerateSvg(string captchaText)
        {
            var random = new Random();
            var chars = captchaText.ToCharArray();

            // 随机背景色（浅色系）
            var bgR = random.Next(220, 255);
            var bgG = random.Next(220, 255);
            var bgB = random.Next(220, 255);

            // 随机字体颜色
            var fgR = random.Next(10, 80);
            var fgG = random.Next(10, 80);
            var fgB = random.Next(10, 80);

            int width = 280;
            int height = 100;
            int fontSize = 45;

            var svg = new StringBuilder();
            svg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">");

            // 1. 背景
            svg.AppendLine($"  <rect width=\"{width}\" height=\"{height}\" rx=\"12\" fill=\"rgb({bgR},{bgG},{bgB})\" />");

            // 2. 干扰线（3-5条）
            int lineCount = random.Next(3, 6);
            for (int i = 0; i < lineCount; i++)
            {
                var r = random.Next(100, 200);
                var g = random.Next(100, 200);
                var b = random.Next(100, 200);
                var x1 = random.Next(0, width);
                var y1 = random.Next(0, height);
                var x2 = random.Next(0, width);
                var y2 = random.Next(0, height);
                var strokeWidth = random.Next(1, 4);
                svg.AppendLine($"  <line x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\" stroke=\"rgb({r},{g},{b})\" stroke-width=\"{strokeWidth}\" stroke-dasharray=\"{random.Next(3, 10)},{random.Next(2, 6)}\" />");
            }

            // 3. 干扰点（50-100个）
            int dotCount = random.Next(50, 100);
            for (int i = 0; i < dotCount; i++)
            {
                var x = random.Next(0, width);
                var y = random.Next(0, height);
                var r = random.Next(100, 220);
                var g = random.Next(100, 220);
                var b = random.Next(100, 220);
                var radius = random.Next(1, 3);
                svg.AppendLine($"  <circle cx=\"{x}\" cy=\"{y}\" r=\"{radius}\" fill=\"rgb({r},{g},{b})\" opacity=\"0.5\" />");
            }

            // 4. 干扰曲线（2条）
            for (int i = 0; i < 2; i++)
            {
                var r = random.Next(80, 180);
                var g = random.Next(80, 180);
                var b = random.Next(80, 180);
                var p1x = random.Next(0, width);
                var p1y = random.Next(0, height);
                var p2x = random.Next(0, width);
                var p2y = random.Next(0, height);
                var p3x = random.Next(0, width);
                var p3y = random.Next(0, height);
                var p4x = random.Next(0, width);
                var p4y = random.Next(0, height);
                svg.AppendLine($"  <path d=\"M{p1x} {p1y} Q{p2x} {p2y} {p3x} {p3y} T{p4x} {p4y}\" stroke=\"rgb({r},{g},{b})\" stroke-width=\"1.5\" fill=\"none\" opacity=\"0.4\" />");
            }

            // 5. 绘制字符（每个字符独立旋转和偏移）
            int charSpacing = (width - 40) / chars.Length;
            for (int i = 0; i < chars.Length; i++)
            {
                // 每个字符独立颜色（深色系）
                var cr = random.Next(10, 80);
                var cg = random.Next(10, 80);
                var cb = random.Next(10, 80);

                // 随机旋转角度（-25° ~ 25°）
                var angle = random.Next(-25, 25);

                // 随机偏移
                var offsetX = random.Next(-5, 5);
                var offsetY = random.Next(-8, 8);

                // 随机缩放（0.8 ~ 1.1）
                var scale = 0.8 + (random.NextDouble() * 0.3);

                // 字符位置
                var x = 25 + (i * charSpacing) + offsetX;
                var y = height / 2 + 12 + offsetY;

                // 字符宽度自适应
                var charWidth = fontSize * 0.7;

                svg.AppendLine($@"
  <text x=""{x}"" y=""{y}"" 
        font-family=""{GetRandomFont()}"" 
        font-size=""{fontSize * scale}"" 
        font-weight=""{random.Next(600, 900)}""
        fill=""rgb({cr},{cg},{cb})""
        transform=""rotate({angle} {x} {y})""
        text-anchor=""middle""
        dominant-baseline=""central"">
    {chars[i]}
  </text>");
            }

            // 6. 再加一层半透明噪点（增强安全性）
            for (int i = 0; i < 30; i++)
            {
                var x = random.Next(0, width);
                var y = random.Next(0, height);
                var size = random.Next(1, 4);
                svg.AppendLine($"  <rect x=\"{x}\" y=\"{y}\" width=\"{size}\" height=\"{size}\" fill=\"rgb({random.Next(50,150)},{random.Next(50,150)},{random.Next(50,150)})\" opacity=\"0.2\" />");
            }

            svg.AppendLine("</svg>");
            return svg.ToString();
        }

        private string GetRandomFont()
        {
            var fonts = new[]
            {
                "Arial", "Verdana", "Georgia",
                "Comic Sans MS", "Impact",
                "Tahoma", "Trebuchet MS",
                "Courier New"
            };
            return fonts[new Random().Next(fonts.Length)];
        }

        // 生成验证码并存入 Session
        public string GenerateAndStoreCaptcha()
        {
            var text = GenerateCaptchaText();
            _httpContextAccessor.HttpContext?.Session.SetString("SvgCaptchaText", text);
            return text;
        }

        // 验证用户输入
        public bool VerifyCaptcha(string userInput)
        {
            var stored = _httpContextAccessor.HttpContext?.Session.GetString("SvgCaptchaText");
            if (string.IsNullOrEmpty(stored)) return false;

            // 忽略大小写
            return stored.Equals(userInput?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
