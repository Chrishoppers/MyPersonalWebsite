using Microsoft.AspNetCore.Http;
using System;
using System.Text;

namespace MyPersonalWebsite.Services
{
    public class SvgCaptchaService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Random _random = new();

        // 字体列表
        private readonly string[] _fonts = new[]
        {
            "Arial, sans-serif",
            "Verdana, sans-serif",
            "Georgia, serif",
            "Times New Roman, serif",
            "Comic Sans MS, cursive",
            "Impact, sans-serif",
            "Courier New, monospace",
            "Trebuchet MS, sans-serif",
            "Franklin Gothic Medium, sans-serif",
            "Palatino Linotype, serif"
        };

        // ============================================================
        // 生成验证码（排除易混淆字符：0/O, 1/I/l）
        // ============================================================
        public string GenerateCaptchaText()
        {
            // ⭐ 排除易混淆字符：0, O, 1, I
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var result = new char[6];
            for (int i = 0; i < 6; i++)
            {
                result[i] = chars[_random.Next(chars.Length)];
            }
            return new string(result);
        }

        // ============================================================
        // 生成高难度 SVG
        // ============================================================
        public string GenerateSvg(string captchaText)
        {
            int width = 320;
            int height = 120;

            var svg = new StringBuilder();
            svg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">");

            // ===== 1. 背景 =====
            var bgR = _random.Next(230, 255);
            var bgG = _random.Next(230, 255);
            var bgB = _random.Next(230, 255);
            svg.AppendLine($"  <rect width=\"{width}\" height=\"{height}\" rx=\"12\" fill=\"rgb({bgR},{bgG},{bgB})\" />");

            // ===== 2. 背景网格噪点 =====
            for (int x = 0; x < width; x += 6)
            {
                for (int y = 0; y < height; y += 6)
                {
                    if (_random.Next(0, 100) > 75)
                    {
                        var r = _random.Next(150, 220);
                        var g = _random.Next(150, 220);
                        var b = _random.Next(150, 220);
                        svg.AppendLine($"  <rect x=\"{x}\" y=\"{y}\" width=\"2\" height=\"2\" fill=\"rgb({r},{g},{b})\" opacity=\"0.4\" />");
                    }
                }
            }

            // ===== 3. 背景斜条纹 =====
            for (int i = -height; i < width + height; i += 15)
            {
                var alpha = _random.Next(10, 40);
                svg.AppendLine($"  <line x1=\"{i}\" y1=\"0\" x2=\"{i + 25}\" y2=\"{height}\" stroke=\"rgba(0,0,0,{alpha/255f:F2})\" stroke-width=\"1\" opacity=\"0.3\" />");
            }

            // ===== 4. 大量干扰线（40-60条） =====
            int lineCount = _random.Next(40, 65);
            for (int i = 0; i < lineCount; i++)
            {
                var r = _random.Next(80, 220);
                var g = _random.Next(80, 220);
                var b = _random.Next(80, 220);
                var x1 = _random.Next(-30, width + 30);
                var y1 = _random.Next(-30, height + 30);
                var x2 = _random.Next(-30, width + 30);
                var y2 = _random.Next(-30, height + 30);
                var strokeWidth = _random.Next(1, 3);
                var opacity = 0.2 + _random.NextDouble() * 0.4;
                svg.AppendLine($"  <line x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\" stroke=\"rgb({r},{g},{b})\" stroke-width=\"{strokeWidth}\" opacity=\"{opacity:F2}\" />");
            }

            // ===== 5. 波浪干扰曲线（4-6条） =====
            int curveCount = _random.Next(4, 7);
            for (int i = 0; i < curveCount; i++)
            {
                var r = _random.Next(80, 180);
                var g = _random.Next(80, 180);
                var b = _random.Next(80, 180);
                var p1x = _random.Next(0, width);
                var p1y = _random.Next(0, height);
                var p2x = _random.Next(0, width);
                var p2y = _random.Next(0, height);
                var p3x = _random.Next(0, width);
                var p3y = _random.Next(0, height);
                var p4x = _random.Next(0, width);
                var p4y = _random.Next(0, height);
                var strokeWidth = _random.Next(1, 3);
                var opacity = 0.3 + _random.NextDouble() * 0.3;
                svg.AppendLine($"  <path d=\"M{p1x} {p1y} Q{p2x} {p2y} {p3x} {p3y} T{p4x} {p4y}\" stroke=\"rgb({r},{g},{b})\" stroke-width=\"{strokeWidth}\" fill=\"none\" opacity=\"{opacity:F2}\" />");
            }

            // ===== 6. 随机噪点（300-500个） =====
            int dotCount = _random.Next(300, 500);
            for (int i = 0; i < dotCount; i++)
            {
                var x = _random.Next(0, width);
                var y = _random.Next(0, height);
                var r = _random.Next(80, 220);
                var g = _random.Next(80, 220);
                var b = _random.Next(80, 220);
                var size = _random.Next(1, 4);
                var opacity = 0.2 + _random.NextDouble() * 0.3;
                svg.AppendLine($"  <circle cx=\"{x}\" cy=\"{y}\" r=\"{size}\" fill=\"rgb({r},{g},{b})\" opacity=\"{opacity:F2}\" />");
            }

            // ===== 7. 绘制字符（高度扭曲） =====
            var chars = captchaText.ToCharArray();
            int charCount = chars.Length;
            int totalWidth = width - 50;
            int startX = 25;
            int spacing = totalWidth / charCount;

            for (int i = 0; i < charCount; i++)
            {
                // 每个字符随机颜色（深色系）
                var cr = _random.Next(5, 70);
                var cg = _random.Next(5, 70);
                var cb = _random.Next(5, 70);

                // 随机旋转 -30° ~ 30°
                var angle = _random.Next(-32, 32);

                // 随机垂直偏移 -15 ~ 15
                var offsetY = _random.Next(-15, 15);

                // 随机字体大小（38-55px）
                var fontSize = _random.Next(38, 56);

                // 随机字体
                var font = _fonts[_random.Next(_fonts.Length)];

                // 随机缩放（0.75-1.15）
                var scale = 0.75 + _random.NextDouble() * 0.4;

                // 字符位置
                var x = startX + (i * spacing) + _random.Next(-5, 5);
                var y = height / 2 + 12 + offsetY;

                // 文字阴影（增加识别难度）
                var shadowX = _random.Next(1, 3);
                var shadowY = _random.Next(1, 3);

                // 随机水平拉伸（0.8-1.2）
                var scaleX = 0.8 + _random.NextDouble() * 0.4;

                // 字符倾斜（skewX）
                var skewX = _random.Next(-15, 15);

                // 先画阴影（灰色，增加干扰）
                svg.AppendLine($@"
  <text x=""{x + shadowX}"" y=""{y + shadowY}"" 
        font-family=""{font}"" 
        font-size=""{fontSize * scale:F1}"" 
        font-weight=""{_random.Next(400, 900)}""
        fill=""rgb({_random.Next(150, 220)},{_random.Next(150, 220)},{_random.Next(150, 220)})""
        transform=""rotate({angle} {x} {y}) skewX({skewX}) scale({scaleX:F2}, 1)""
        text-anchor=""middle""
        dominant-baseline=""central"">
    {chars[i]}
  </text>");

                // 再画主字符
                svg.AppendLine($@"
  <text x=""{x}"" y=""{y}"" 
        font-family=""{font}"" 
        font-size=""{fontSize * scale:F1}"" 
        font-weight=""{_random.Next(400, 900)}""
        fill=""rgb({cr},{cg},{cb})""
        transform=""rotate({angle} {x} {y}) skewX({skewX}) scale({scaleX:F2}, 1)""
        text-anchor=""middle""
        dominant-baseline=""central"">
    {chars[i]}
  </text>");
            }

            svg.AppendLine("</svg>");
            return svg.ToString();
        }

        // ============================================================
        // 存储验证码到 Session
        // ============================================================
        public string GenerateAndStoreCaptcha()
        {
            var text = GenerateCaptchaText();
            _httpContextAccessor.HttpContext?.Session.SetString("SvgCaptchaText", text);
            return text;
        }

        // ============================================================
        // 验证用户输入
        // ============================================================
        public bool VerifyCaptcha(string userInput)
        {
            var stored = _httpContextAccessor.HttpContext?.Session.GetString("SvgCaptchaText");
            if (string.IsNullOrEmpty(stored)) return false;

            // 忽略大小写和前后空格
            var input = userInput?.Trim();
            return string.Equals(input, stored, StringComparison.OrdinalIgnoreCase);
        }
    }
}
