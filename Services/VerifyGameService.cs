using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyPersonalWebsite.Services
{
    public class VerifyGameService
    {
        private readonly Random _random = new();

        private readonly List<(string name, string hex)> _colors = new()
        {
            ("红色", "#FF0000"), ("蓝色", "#0055FF"), ("绿色", "#00AA00"),
            ("黄色", "#DDBB00"), ("紫色", "#8800CC"), ("橙色", "#FF6600"),
            ("粉色", "#FF4499"), ("青色", "#00CCCC"), ("棕色", "#8B4513"),
            ("黑色", "#000000"), ("白色", "#FFFFFF"), ("灰色", "#888888")
        };

        private readonly List<string> _idioms = new()
        {
            "一马当先", "龙飞凤舞", "画蛇添足", "守株待兔", "狐假虎威",
            "马到成功", "鸟语花香", "鱼目混珠", "鹤立鸡群", "龙腾虎跃",
            "画龙点睛", "亡羊补牢", "杯弓蛇影", "指鹿为马", "马不停蹄"
        };

        private readonly Dictionary<char, int> _strokeMap = new()
        {
            {'一',1},{'二',2},{'三',3},{'四',5},{'五',4},{'六',4},{'七',2},{'八',2},{'九',2},{'十',2},
            {'人',2},{'大',3},{'天',4},{'地',6},{'日',4},{'月',4},{'水',4},{'火',4},{'山',3},{'石',5}
        };

        private readonly string[] _chineseNumbers = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };

        public CaptchaChallenge GenerateChallenge(int level)
        {
            var typeIndex = (level - 1) / 5;
            if (typeIndex >= 20) typeIndex = 19;

            switch (typeIndex)
            {
                case 0: return GenerateTextChallenge(level);
                case 1: return GenerateArithmeticChallenge(level);
                case 2: return GenerateStrokeChallenge(level);
                case 3: return GenerateColorChallenge(level);
                case 4: return GenerateFindDifferentChallenge(level);
                case 5: return GenerateReverseChallenge(level);
                case 6: return GenerateMissingLetterChallenge(level);
                case 7: return GenerateQuickTapChallenge(level);
                case 8: return GenerateIdiomChallenge(level);
                case 9: return GenerateChineseNumberChallenge(level);
                case 10: return GenerateCaseConversionChallenge(level);
                case 11: return GeneratePinyinChallenge(level);
                case 12: return GenerateInverseColorChallenge(level);
                case 13: return GenerateMirrorChallenge(level);
                case 14: return GenerateKeyboardNeighborChallenge(level);
                case 15: return GenerateSplitCharacterChallenge(level);
                case 16: return GenerateMemoryChallenge(level);
                case 17: return GenerateDirectionChallenge(level);
                case 18: return GenerateCountChallenge(level);
                default: return GenerateUltimateChallenge(level);
            }
        }

        // ============================================================
        // 工具方法
        // ============================================================

        private string GenerateSvg(string text, int distortion, int lineCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"320\" height=\"90\" viewBox=\"0 0 320 90\">");
            sb.AppendLine($"<rect width=\"320\" height=\"90\" rx=\"10\" fill=\"#f0f0f0\"/>");

            for (int i = 0; i < lineCount; i++)
            {
                var r = _random.Next(100, 220);
                var g = _random.Next(100, 220);
                var b = _random.Next(100, 220);
                sb.AppendLine($"<line x1=\"{_random.Next(0, 320)}\" y1=\"{_random.Next(0, 90)}\" x2=\"{_random.Next(0, 320)}\" y2=\"{_random.Next(0, 90)}\" stroke=\"rgb({r},{g},{b})\" stroke-width=\"{_random.Next(1, 3)}\" opacity=\"0.4\"/>");
            }

            var chars = text.ToCharArray();
            var spacing = 280 / chars.Length;
            for (int i = 0; i < chars.Length; i++)
            {
                var r = _random.Next(10, 80);
                var g = _random.Next(10, 80);
                var b = _random.Next(10, 80);
                var angle = _random.Next(-distortion, distortion);
                var x = 20 + i * spacing + _random.Next(-5, 5);
                sb.AppendLine($"<text x=\"{x}\" y=\"50\" font-family=\"Arial, sans-serif\" font-size=\"36\" font-weight=\"bold\" fill=\"rgb({r},{g},{b})\" transform=\"rotate({angle} {x} 50)\" text-anchor=\"middle\" dominant-baseline=\"central\">{chars[i]}</text>");
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private string GenerateQuickSvg(string target)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"320\" height=\"90\" viewBox=\"0 0 320 90\">");
            sb.AppendLine($"<rect width=\"320\" height=\"90\" rx=\"10\" fill=\"#f0f0f0\"/>");
            var r = _random.Next(10, 80);
            var g = _random.Next(10, 80);
            var b = _random.Next(10, 80);
            sb.AppendLine($"<text x=\"160\" y=\"50\" font-family=\"Arial, sans-serif\" font-size=\"48\" font-weight=\"bold\" fill=\"rgb({r},{g},{b})\" text-anchor=\"middle\" dominant-baseline=\"central\">{target}</text>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private List<string> GenerateOptionsString(string correct, int count)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var options = new List<string> { correct };
            while (options.Count < count)
            {
                var fake = new string(Enumerable.Range(0, correct.Length).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
                if (!options.Contains(fake)) options.Add(fake);
            }
            Shuffle(options);
            return options;
        }

        // ⭐ 修复：专门处理 char 参数的重载
        private List<string> GenerateOptionsChar(char correct, int count)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var options = new List<string> { correct.ToString() };
            while (options.Count < count)
            {
                var c = chars[_random.Next(chars.Length)];
                if (!options.Contains(c.ToString())) options.Add(c.ToString());
            }
            Shuffle(options);
            return options;
        }

        private List<string> GenerateNumberOptions(int correct, int count, int level)
        {
            var options = new List<string> { correct.ToString() };
            var range = Math.Max(3, 5 + level / 10);
            while (options.Count < count)
            {
                var fake = correct + _random.Next(-range, range);
                if (fake < 0) fake = _random.Next(1, 20);
                var str = fake.ToString();
                if (!options.Contains(str) && str != correct.ToString()) options.Add(str);
            }
            Shuffle(options);
            return options;
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private int GetPoints(int level) => 10 + level / 5;
        private int GetTimeLimit(int level) => Math.Max(5, 20 - level / 5);

        private string GetMessage(int level)
        {
            var msgs = new[] { "✨ 轻松！", "🔥 继续！", "💪 加油！", "⭐ 太强了！", "🚀 冲！" };
            return msgs[_random.Next(msgs.Length)];
        }

        private string GetUltimateMessage(int level)
        {
            var msgs = new[] { "🏆 终极王者！", "💀 连AI都怕你！", "👑 你是人类之光！", "🚀 太逆天了！", "⚡ 无敌！" };
            return msgs[_random.Next(msgs.Length)];
        }

        // ============================================================
        // 类型0：文字扭曲识别（1-5关）
        // ============================================================
        private CaptchaChallenge GenerateTextChallenge(int level)
        {
            var progress = (level - 1) % 5 + 1;
            var len = Math.Min(3 + progress, 6);
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var text = new string(Enumerable.Range(0, len).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
            var svg = GenerateSvg(text, 5 + progress * 4, 10 + progress * 3);

            return new CaptchaChallenge
            {
                Type = 0,
                Level = level,
                Question = "👁️ 请输入下方图片中的文字",
                ImageSvg = svg,
                CorrectAnswer = text.ToUpper(),
                Options = GenerateOptionsString(text, 4),
                DisplayType = "image",
                TimeLimit = 15,
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型1：算术计算（6-10关）
        // ============================================================
        private CaptchaChallenge GenerateArithmeticChallenge(int level)
        {
            var progress = (level - 6) % 5 + 1;
            var maxNum = 10 + progress * 8;
            var a = _random.Next(5, maxNum);
            var b = _random.Next(1, maxNum / 2);
            var ops = new[] { "+", "-", "×" };
            var op = ops[_random.Next(3)];
            var result = op == "+" ? a + b : op == "-" ? a - b : a * b;

            return new CaptchaChallenge
            {
                Type = 1,
                Level = level,
                Question = $"🧮 {a} {op} {b} = ?",
                CorrectAnswer = result.ToString(),
                Options = GenerateNumberOptions(result, 4, level),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型2：汉字笔画数（11-15关）
        // ============================================================
        private CaptchaChallenge GenerateStrokeChallenge(int level)
        {
            var keys = _strokeMap.Keys.ToList();
            var ch = keys[_random.Next(keys.Count)];
            var stroke = _strokeMap[ch];

            return new CaptchaChallenge
            {
                Type = 2,
                Level = level,
                Question = $"📝 「{ch}」字有几画？",
                CorrectAnswer = stroke.ToString(),
                Options = GenerateNumberOptions(stroke, 4, level),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型3：颜色识别（16-20关）
        // ============================================================
        private CaptchaChallenge GenerateColorChallenge(int level)
        {
            var idx = _random.Next(_colors.Count);
            var color = _colors[idx];

            var options = new List<string> { color.name };
            var pool = _colors.Select(c => c.name).Where(n => n != color.name).ToList();
            while (options.Count < 4 && pool.Any())
            {
                var r = _random.Next(pool.Count);
                options.Add(pool[r]);
                pool.RemoveAt(r);
            }
            Shuffle(options);

            return new CaptchaChallenge
            {
                Type = 3,
                Level = level,
                Question = "🎨 下面文字是什么颜色？",
                DisplayText = $"<span style=\"color:{color.hex};font-size:2.5rem;font-weight:bold;\">██████</span>",
                CorrectAnswer = color.name,
                Options = options,
                DisplayType = "color",
                TimeLimit = GetTimeLimit(level) - 2,
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型4：找不同（21-25关）
        // ============================================================
        private CaptchaChallenge GenerateFindDifferentChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var target = chars[_random.Next(chars.Length)];
            var options = new List<string> { target.ToString() };
            while (options.Count < 4)
            {
                var c = chars[_random.Next(chars.Length)];
                if (!options.Contains(c.ToString())) options.Add(c.ToString());
            }
            Shuffle(options);

            return new CaptchaChallenge
            {
                Type = 4,
                Level = level,
                Question = "🔍 哪个字符与其他的不同？",
                CorrectAnswer = target.ToString(),
                Options = options,
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level) - 2,
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型5：倒序识别（26-30关）
        // ============================================================
        private CaptchaChallenge GenerateReverseChallenge(int level)
        {
            var progress = (level - 26) % 5 + 1;
            var len = Math.Min(3 + progress, 5);
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var text = new string(Enumerable.Range(0, len).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
            var reversed = new string(text.Reverse().ToArray());
            var svg = GenerateSvg(text, 10 + progress * 4, 10 + progress * 3);

            return new CaptchaChallenge
            {
                Type = 5,
                Level = level,
                Question = "🔄 图片中的文字是什么？（倒过来了）",
                ImageSvg = svg,
                CorrectAnswer = reversed.ToUpper(),
                Options = GenerateOptionsString(reversed, 4),
                DisplayType = "image",
                TimeLimit = GetTimeLimit(level) + 2,
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型6：缺失字母（31-35关）
        // ============================================================
        private CaptchaChallenge GenerateMissingLetterChallenge(int level)
        {
            var progress = (level - 31) % 5 + 1;
            var len = Math.Min(3 + progress, 6);
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            var word = new string(Enumerable.Range(0, len).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
            var idx = _random.Next(len);
            var correct = word[idx];
            var display = word.ToCharArray();
            display[idx] = '_';

            return new CaptchaChallenge
            {
                Type = 6,
                Level = level,
                Question = $"🔤 补全单词：{new string(display)}",
                CorrectAnswer = correct.ToString(),
                Options = GenerateOptionsChar(correct, 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型7：快速点击（36-40关）
        // ============================================================
        private CaptchaChallenge GenerateQuickTapChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var target = chars[_random.Next(chars.Length)];
            var options = new List<string> { target.ToString() };
            while (options.Count < 4)
            {
                var c = chars[_random.Next(chars.Length)];
                if (!options.Contains(c.ToString())) options.Add(c.ToString());
            }
            Shuffle(options);
            var svg = GenerateQuickSvg(target);

            return new CaptchaChallenge
            {
                Type = 7,
                Level = level,
                Question = "⚡ 快速找到目标字符！",
                ImageSvg = svg,
                CorrectAnswer = target.ToString(),
                Options = options,
                DisplayType = "image",
                TimeLimit = Math.Max(3, 8 - level / 8),
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型8：成语填空（41-45关）
        // ============================================================
        private CaptchaChallenge GenerateIdiomChallenge(int level)
        {
            var idiom = _idioms[_random.Next(_idioms.Count)];
            var idx = _random.Next(idiom.Length);
            var correct = idiom[idx];
            var display = idiom.ToCharArray();
            display[idx] = '□';

            return new CaptchaChallenge
            {
                Type = 8,
                Level = level,
                Question = $"📖 补全成语：{new string(display)}",
                CorrectAnswer = correct.ToString(),
                Options = GenerateOptionsChar(correct, 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型9：中文数字转阿拉伯（46-50关）
        // ============================================================
        private CaptchaChallenge GenerateChineseNumberChallenge(int level)
        {
            var num = _random.Next(0, 11);
            var display = _chineseNumbers[num];

            return new CaptchaChallenge
            {
                Type = 9,
                Level = level,
                Question = $"🔢 「{display}」对应的数字是？",
                CorrectAnswer = num.ToString(),
                Options = GenerateNumberOptions(num, 4, level),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型10：大小写转换（51-55关）
        // ============================================================
        private CaptchaChallenge GenerateCaseConversionChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            var c = chars[_random.Next(chars.Length)];
            var isUpper = _random.Next(0, 2) == 0;

            return new CaptchaChallenge
            {
                Type = 10,
                Level = level,
                Question = isUpper ? $"🔤 字母「{c}」的小写是？" : $"🔤 字母「{c.ToString().ToLower()}」的大写是？",
                CorrectAnswer = isUpper ? c.ToString().ToLower() : c.ToString(),
                Options = GenerateOptionsChar(isUpper ? char.ToLower(c) : c, 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型11：拼音首字母（56-60关）
        // ============================================================
        private CaptchaChallenge GeneratePinyinChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            var c = chars[_random.Next(chars.Length)];

            return new CaptchaChallenge
            {
                Type = 11,
                Level = level,
                Question = $"🔊 字母「{c}」的读音是？",
                CorrectAnswer = c.ToString(),
                Options = GenerateOptionsChar(c, 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型12：反色识别（61-65关）
        // ============================================================
        private CaptchaChallenge GenerateInverseColorChallenge(int level)
        {
            var colors = new[] { ("黑色", "#000000"), ("白色", "#FFFFFF") };
            var idx = _random.Next(2);
            var color = colors[idx];
            var bgColor = idx == 0 ? "#FFFFFF" : "#000000";

            return new CaptchaChallenge
            {
                Type = 12,
                Level = level,
                Question = "🎨 下面文字是什么颜色？（注意背景）",
                DisplayText = $"<span style=\"color:{color.Item2};background:{bgColor};padding:0.5rem 2rem;border-radius:12px;font-size:2.5rem;font-weight:bold;\">██████</span>",
                CorrectAnswer = color.Item1,
                Options = new List<string> { "黑色", "白色" },
                DisplayType = "color",
                TimeLimit = GetTimeLimit(level) - 2,
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型13：镜像文字（66-70关）
        // ============================================================
        private CaptchaChallenge GenerateMirrorChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            var c = chars[_random.Next(chars.Length)];

            return new CaptchaChallenge
            {
                Type = 13,
                Level = level,
                Question = $"🪞 字母「{c}」的镜像字母是？",
                CorrectAnswer = c.ToString(),
                Options = GenerateOptionsChar(c, 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型14：键盘相邻键（71-75关）
        // ============================================================
        private CaptchaChallenge GenerateKeyboardNeighborChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            var c = chars[_random.Next(chars.Length)];

            return new CaptchaChallenge
            {
                Type = 14,
                Level = level,
                Question = $"⌨️ 键盘上「{c}」的右边键是？",
                CorrectAnswer = c.ToString(),
                Options = GenerateOptionsChar(c, 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型15：汉字拆分（76-80关）
        // ============================================================
        private CaptchaChallenge GenerateSplitCharacterChallenge(int level)
        {
            var chars = new[] { '明', '林', '从', '众', '晶', '森', '焱', '磊', '鑫', '淼' };
            var c = chars[_random.Next(chars.Length)];

            return new CaptchaChallenge
            {
                Type = 15,
                Level = level,
                Question = $"✂️ 「{c}」可以拆成几个相同字？",
                CorrectAnswer = c.ToString().Length.ToString(),
                Options = GenerateNumberOptions(c.ToString().Length, 4, level),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型16：数字记忆（81-85关）
        // ============================================================
        private CaptchaChallenge GenerateMemoryChallenge(int level)
        {
            var progress = (level - 81) % 5 + 1;
            var len = Math.Min(3 + progress, 6);
            var chars = "0123456789";
            var text = new string(Enumerable.Range(0, len).Select(_ => chars[_random.Next(chars.Length)]).ToArray());

            return new CaptchaChallenge
            {
                Type = 16,
                Level = level,
                Question = $"🧠 记住这个数字：{text} （然后输入它）",
                CorrectAnswer = text,
                Options = GenerateOptionsString(text, 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level) + 2,
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型17：方向判断（86-90关）
        // ============================================================
        private CaptchaChallenge GenerateDirectionChallenge(int level)
        {
            var dirs = new[] { "上", "下", "左", "右" };
            var dir = dirs[_random.Next(4)];

            return new CaptchaChallenge
            {
                Type = 17,
                Level = level,
                Question = $"🧭 请选择「{dir}」的相反方向",
                CorrectAnswer = dir == "上" ? "下" : dir == "下" ? "上" : dir == "左" ? "右" : "左",
                Options = new List<string> { "上", "下", "左", "右" },
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level) - 2,
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型18：字符计数（91-95关）
        // ============================================================
        private CaptchaChallenge GenerateCountChallenge(int level)
        {
            var progress = (level - 91) % 5 + 1;
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var textLen = Math.Min(6 + progress, 10);
            var text = new string(Enumerable.Range(0, textLen).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
            var target = chars[_random.Next(chars.Length)];
            var count = text.Count(c => c == target);

            return new CaptchaChallenge
            {
                Type = 18,
                Level = level,
                Question = $"🔢 字符「{target}」在「{text}」中出现几次？",
                CorrectAnswer = count.ToString(),
                Options = GenerateNumberOptions(count, 4, level),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型19：终极混合（96-100关）
        // ============================================================
        private CaptchaChallenge GenerateUltimateChallenge(int level)
        {
            var types = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
            var typeIdx = types[_random.Next(types.Length)];

            CaptchaChallenge challenge;
            switch (typeIdx)
            {
                case 0: challenge = GenerateTextChallenge(level); break;
                case 1: challenge = GenerateArithmeticChallenge(level); break;
                case 2: challenge = GenerateStrokeChallenge(level); break;
                case 3: challenge = GenerateColorChallenge(level); break;
                case 4: challenge = GenerateFindDifferentChallenge(level); break;
                case 5: challenge = GenerateReverseChallenge(level); break;
                case 6: challenge = GenerateMissingLetterChallenge(level); break;
                case 7: challenge = GenerateQuickTapChallenge(level); break;
                case 8: challenge = GenerateIdiomChallenge(level); break;
                case 9: challenge = GenerateChineseNumberChallenge(level); break;
                case 10: challenge = GenerateCaseConversionChallenge(level); break;
                case 11: challenge = GeneratePinyinChallenge(level); break;
                case 12: challenge = GenerateInverseColorChallenge(level); break;
                case 13: challenge = GenerateMirrorChallenge(level); break;
                case 14: challenge = GenerateKeyboardNeighborChallenge(level); break;
                case 15: challenge = GenerateSplitCharacterChallenge(level); break;
                case 16: challenge = GenerateMemoryChallenge(level); break;
                case 17: challenge = GenerateDirectionChallenge(level); break;
                default: challenge = GenerateCountChallenge(level); break;
            }

            challenge.Type = 19;
            challenge.Level = level;
            challenge.TimeLimit = Math.Max(3, GetTimeLimit(level) - 2);
            challenge.Points = GetPoints(level) * 3;
            challenge.Question = $"💀 终极挑战！{challenge.Question}";
            challenge.FunMessage = GetUltimateMessage(level);

            return challenge;
        }
    }

    public class CaptchaChallenge
    {
        public int Type { get; set; }
        public int Level { get; set; }
        public string Question { get; set; } = "";
        public string? ImageSvg { get; set; }
        public string? DisplayText { get; set; }
        public string CorrectAnswer { get; set; } = "";
        public List<string> Options { get; set; } = new();
        public string DisplayType { get; set; } = "text";
        public int TimeLimit { get; set; } = 15;
        public int Points { get; set; } = 10;
        public string FunMessage { get; set; } = "";
    }
}
