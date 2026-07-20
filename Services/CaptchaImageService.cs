using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace MyPersonalWebsite.Services
{
    public class CaptchaImageService
    {
        private readonly Random _random = new Random();

        public string GenerateCaptchaText(int length = 6)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[_random.Next(chars.Length)];
            }
            return new string(result);
        }

        public byte[] GenerateCaptchaImage(string captchaText)
        {
            int width = 320;
            int height = 120;
            int charCount = captchaText.Length;

            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // 1. 复杂背景
            var bgColor1 = Color.FromArgb(
                _random.Next(200, 255),
                _random.Next(200, 255),
                _random.Next(200, 255)
            );
            var bgColor2 = Color.FromArgb(
                _random.Next(180, 230),
                _random.Next(180, 230),
                _random.Next(180, 230)
            );
            using (var bgBrush = new LinearGradientBrush(
                new Rectangle(0, 0, width, height),
                bgColor1, bgColor2,
                _random.Next(0, 360)
            ))
            {
                graphics.FillRectangle(bgBrush, 0, 0, width, height);
            }

            // 背景纹理噪点
            for (int i = 0; i < 800; i++)
            {
                bitmap.SetPixel(
                    _random.Next(0, width),
                    _random.Next(0, height),
                    Color.FromArgb(
                        _random.Next(50, 180),
                        _random.Next(50, 180),
                        _random.Next(50, 180)
                    )
                );
            }

            // 2. 干扰线条
            for (int i = 0; i < 5; i++)
            {
                using (var pen = new Pen(
                    Color.FromArgb(
                        _random.Next(80, 200),
                        _random.Next(80, 200),
                        _random.Next(80, 200)
                    ),
                    _random.Next(2, 4)
                ))
                {
                    var points = new Point[6];
                    for (int j = 0; j < 6; j++)
                    {
                        points[j] = new Point(
                            j * (width / 5) + _random.Next(-10, 10),
                            _random.Next(0, height)
                        );
                    }
                    graphics.DrawCurve(pen, points, 0.8f);
                }
            }

            // 交叉直线
            for (int i = 0; i < 8; i++)
            {
                using (var pen = new Pen(
                    Color.FromArgb(
                        _random.Next(100, 220),
                        _random.Next(100, 220),
                        _random.Next(100, 220)
                    ),
                    _random.Next(1, 3)
                ))
                {
                    graphics.DrawLine(
                        pen,
                        _random.Next(0, width),
                        _random.Next(0, height),
                        _random.Next(0, width),
                        _random.Next(0, height)
                    );
                }
            }

            // 3. 绘制字符
            string[] fontNames = {
                "Arial", "Verdana", "Times New Roman",
                "Georgia", "Comic Sans MS", "Tahoma",
                "Courier New", "Impact"
            };

            int spacing = (width - 40) / charCount;

            for (int i = 0; i < charCount; i++)
            {
                char c = captchaText[i];
                float fontSize = _random.Next(42, 58);

                using (var font = new Font(
                    fontNames[_random.Next(fontNames.Length)],
                    fontSize,
                    FontStyle.Bold
                ))
                {
                    var color = Color.FromArgb(
                        _random.Next(10, 60),
                        _random.Next(10, 60),
                        _random.Next(10, 60)
                    );

                    float x = 20 + (i * spacing) + _random.Next(-8, 8);
                    float y = _random.Next(15, 40);

                    using (var path = new GraphicsPath())
                    {
                        path.AddString(
                            c.ToString(),
                            font.FontFamily,
                            (int)font.Style,
                            font.Size,
                            new PointF(0, 0),
                            StringFormat.GenericDefault
                        );

                        float angle = _random.Next(-35, 35);
                        float scaleX = 0.7f + (_random.Next(0, 60) / 100f);
                        float scaleY = 0.7f + (_random.Next(0, 60) / 100f);

                        using (var matrix = new Matrix())
                        {
                            matrix.Translate(x, y);
                            matrix.Rotate(angle);
                            matrix.Scale(scaleX, scaleY);

                            if (_random.Next(0, 2) == 0)
                            {
                                matrix.Shear(
                                    _random.Next(-30, 30) / 100f,
                                    _random.Next(-30, 30) / 100f
                                );
                            }

                            path.Transform(matrix);
                        }

                        using (var fillBrush = new SolidBrush(color))
                        {
                            graphics.FillPath(fillBrush, path);
                        }

                        if (_random.Next(0, 3) == 0)
                        {
                            using (var borderPen = new Pen(
                                Color.FromArgb(
                                    _random.Next(50, 150),
                                    _random.Next(50, 150),
                                    _random.Next(50, 150)
                                ),
                                1
                            ))
                            {
                                graphics.DrawPath(borderPen, path);
                            }
                        }
                    }
                }
            }

            // 4. 额外干扰元素
            for (int i = 0; i < _random.Next(400, 600); i++)
            {
                bitmap.SetPixel(
                    _random.Next(0, width),
                    _random.Next(0, height),
                    Color.FromArgb(
                        _random.Next(80, 220),
                        _random.Next(80, 220),
                        _random.Next(80, 220)
                    )
                );
            }

            for (int i = 0; i < 15; i++)
            {
                using (var pen = new Pen(
                    Color.FromArgb(
                        _random.Next(100, 200),
                        _random.Next(100, 200),
                        _random.Next(100, 200)
                    ),
                    _random.Next(1, 3)
                ))
                {
                    graphics.DrawEllipse(
                        pen,
                        _random.Next(0, width),
                        _random.Next(0, height),
                        _random.Next(3, 10),
                        _random.Next(3, 10)
                    );
                }
            }

            // 5. 边框（修复变量重复问题）
            using (var borderPen = new Pen(Color.FromArgb(80, 120, 180), 2))
            {
                graphics.DrawRectangle(borderPen, 0, 0, width - 1, height - 1);
            }

            using (var innerPen = new Pen(Color.FromArgb(30, 80, 120, 180), 1))
            {
                graphics.DrawRectangle(innerPen, 3, 3, width - 7, height - 7);
            }

            // 6. 输出图片
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }
}