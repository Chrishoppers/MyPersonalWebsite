using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyPersonalWebsite.Services
{
    public class CaptchaGameService
    {
        private readonly Random _random = new();

        // ===== 颜色池 =====
        private readonly List<(string name, string hex)> _colors = new()
        {
            ("红色", "#FF0000"), ("蓝色", "#0055FF"), ("绿色", "#00AA00"),
            ("黄色", "#DDBB00"), ("紫色", "#8800CC"), ("橙色", "#FF6600"),
            ("粉色", "#FF4499"), ("青色", "#00CCCC"), ("棕色", "#8B4513"),
            ("黑色", "#000000"), ("白色", "#FFFFFF"), ("灰色", "#888888")
        };

        // ===== 成语库 =====
        private readonly List<string> _idioms = new()
        {
            "一马当先", "龙飞凤舞", "画蛇添足", "守株待兔", "狐假虎威",
            "马到成功", "鸟语花香", "鱼目混珠", "鹤立鸡群", "龙腾虎跃",
            "画龙点睛", "亡羊补牢", "杯弓蛇影", "指鹿为马", "马不停蹄"
        };

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
                case 10: return GenerateHardTextChallenge(level);
                case 11: return GenerateHardArithmeticChallenge(level);
                case 12: return GenerateHardStrokeChallenge(level);
                case 13: return GenerateHardColorChallenge(level);
                case 14: return GenerateHardFindDifferentChallenge(level);
                case 15: return GenerateHardReverseChallenge(level);
                case 16: return GenerateHardMissingLetterChallenge(level);
                case 17: return GenerateHardQuickTapChallenge(level);
                case 18: return GenerateHardIdiomChallenge(level);
                default: return GenerateUltimateChallenge(level);
            }
        }

        // ============================================================
        // 类型0：文字识别（1-5关）
        // ============================================================
        private CaptchaChallenge GenerateTextChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var len = 4;
            var text = new string(Enumerable.Range(0, len).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
            var svg = GenerateSvg(text);

            return new CaptchaChallenge
            {
                Type = 0,
                Level = level,
                Question = "👁️ 请输入下方图片中的文字",
                ImageSvg = svg,
                CorrectAnswer = text.ToUpper(),
                Options = GenerateOptions(text, 4),
                DisplayType = "image",
                TimeLimit = 15,
                Points = 10 + level,
                FunMessage = "✨ 继续！"
            };
        }

        // ============================================================
        // 类型1：算术（6-10关）
        // ============================================================
        private CaptchaChallenge GenerateArithmeticChallenge(int level)
        {
            var a = _random.Next(5, 20);
            var b = _random.Next(1, 15);
            var ops = new[] { "+", "-", "×" };
            var op = ops[_random.Next(3)];
            int result = op == "+" ? a + b : op == "-" ? a - b : a * b;

            return new CaptchaChallenge
            {
                Type = 1,
                Level = level,
                Question = $"🧮 {a} {op} {b} = ?",
                CorrectAnswer = result.ToString(),
                Options = GenerateNumberOptions(result, 4),
                DisplayType = "text",
                TimeLimit = 12,
                Points = 10 + level,
                FunMessage = "🧠 算对了！"
            };
        }

        // ============================================================
        // 类型2：笔画数（11-15关）
        // ============================================================
        private CaptchaChallenge GenerateStrokeChallenge(int level)
        {
            var chars = "一二三四五六七八九十人大小天地日月";
            var ch = chars[_random.Next(chars.Length)];
            var stroke = GetStroke(ch);

            return new CaptchaChallenge
            {
                Type = 2,
                Level = level,
                Question = $"📝 「{ch}」字有几画？",
                CorrectAnswer = stroke.ToString(),
                Options = GenerateNumberOptions(stroke, 4),
                DisplayType = "text",
                TimeLimit = 12,
                Points = 10 + level,
                FunMessage = "✍️ 厉害！"
            };
        }

        private int GetStroke(char ch)
        {
            var map = new Dictionary<char, int>
            {
                {'一',1},{'二',2},{'三',3},{'四',5},{'五',4},{'六',4},{'七',2},{'八',2},{'九',2},{'十',2},
                {'人',2},{'大',3},{'天',4},{'地',6},{'日',4},{'月',4},{'水',4},{'火',4},{'山',3},{'石',5},
                {'木',4},{'花',7},{'草',9},{'鸟',5},{'鱼',8},{'马',3},{'牛',4},{'羊',6},{'虫',6},{'云',4}
            };
            return map.ContainsKey(ch) ? map[ch] : 5;
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
                TimeLimit = 10,
                Points = 10 + level,
                FunMessage = "🎯 好眼力！"
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
                TimeLimit = 10,
                Points = (10 + level) * 2,
                FunMessage = "👀 火眼金睛！"
            };
        }

        // ============================================================
        // 类型5：倒序识别（26-30关）
        // ============================================================
        private CaptchaChallenge GenerateReverseChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var len = 3 + (level % 3);
            var text = new string(Enumerable.Range(0, len).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
            var reversed = new string(text.Reverse().ToArray());
            var svg = GenerateSvg(text);

            return new CaptchaChallenge
            {
                Type = 5,
                Level = level,
                Question = "🔄 图片中的文字是什么？（倒过来了）",
                ImageSvg = svg,
                CorrectAnswer = reversed.ToUpper(),
                Options = GenerateOptions(reversed, 4),
                DisplayType = "image",
                TimeLimit = 12,
                Points = (10 + level) * 2,
                FunMessage = "🔄 反过来了！"
            };
        }

        // ============================================================
        // 类型6：缺失字母（31-35关）
        // ============================================================
        private CaptchaChallenge GenerateMissingLetterChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            var len = 4 + (level % 3);
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
                Options = GenerateOptions(correct.ToString(), 4),
                DisplayType = "text",
                TimeLimit = 10,
                Points = 10 + level,
                FunMessage = "📖 继续！"
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
                TimeLimit = 5,
                Points = (10 + level) * 2,
                FunMessage = "⚡ 好快！"
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
                Options = GenerateOptions(correct.ToString(), 4),
                DisplayType = "text",
                TimeLimit = 10,
                Points = (10 + level) * 2,
                FunMessage = "📚 成语大师！"
            };
        }

        // ============================================================
        // 类型9：中文数字（46-50关）
        // ============================================================
        private CaptchaChallenge GenerateChineseNumberChallenge(int level)
        {
            var cn = new[] { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
            var num = _random.Next(0, 11);
            var display = cn[num];

            return new CaptchaChallenge
            {
                Type = 9,
                Level = level,
                Question = $"🔢 「{display}」对应的数字是？",
                CorrectAnswer = num.ToString(),
                Options = GenerateNumberOptions(num, 4),
                DisplayType = "text",
                TimeLimit = 8,
                Points = 10 + level,
                FunMessage = "🔢 正确！"
            };
        }

        // ============================================================
        // 类型10：高难度扭曲文字（51-55关）
        // ============================================================
        private CaptchaChallenge GenerateHardTextChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var len = 5 + (level % 3);
            var text = new string(Enumerable.Range(0, len).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
            var svg = GenerateHardSvg(text);

            return new CaptchaChallenge
            {
                Type = 10,
                Level = level,
                Question = "👁️ 请输入下方极度扭曲的文字",
                ImageSvg = svg,
                CorrectAnswer = text.ToUpper(),
                Options = GenerateOptions(text, 4),
                DisplayType = "image",
                TimeLimit = 8,
                Points = (10 + level) * 2,
                FunMessage = "🔥 太强了！"
            };
        }

        // ============================================================
        // 类型11：超大数算术（56-60关）
        // ============================================================
        private CaptchaChallenge GenerateHardArithmeticChallenge(int level)
        {
            var a = _random.Next(100, 500);
            var b = _random.Next(10, 99);
            var ops = new[] { "×", "+", "-" };
            var op = ops[_random.Next(3)];
            int result = op == "×" ? a * b : op == "+" ? a + b : a - b;

            return new CaptchaChallenge
            {
                Type = 11,
                Level = level,
                Question = $"🧮 {a} {op} {b} = ?",
                CorrectAnswer = result.ToString(),
                Options = GenerateNumberOptions(result, 4),
                DisplayType = "text",
                TimeLimit = 6,
                Points = (10 + level) * 2,
                FunMessage = "🧠 心算大师！"
            };
        }

        // ============================================================
        // 类型12：生僻字笔画（61-65关）
        // ============================================================
        private CaptchaChallenge GenerateHardStrokeChallenge(int level)
        {
            var chars = new[] { '繁', '體', '漢', '學', '難', '驗', '證', '碼' };
            var ch = chars[_random.Next(chars.Length)];
            var stroke = GetHardStroke(ch);

            return new CaptchaChallenge
            {
                Type = 12,
                Level = level,
                Question = $"📝 「{ch}」字有几画？",
                CorrectAnswer = stroke.ToString(),
                Options = GenerateNumberOptions(stroke, 4),
                DisplayType = "text",
                TimeLimit = 8,
                Points = (10 + level) * 2,
                FunMessage = "💪 汉字大师！"
            };
        }

        private int GetHardStroke(char ch)
        {
            var map = new Dictionary<char, int>
            {
                {'繁',17},{'體',23},{'漢',14},{'學',16},{'難',19},
                {'驗',23},{'證',19},{'碼',15}
            };
            return map.ContainsKey(ch) ? map[ch] : 15;
        }

        // ============================================================
        // 类型13：相近色识别（66-70关）
        // ============================================================
        private CaptchaChallenge GenerateHardColorChallenge(int level)
        {
            var colorPairs = new List<(string name, string hex, string similar)>
            {
                ("红色", "#FF0000", "#CC3333"),
                ("深红", "#8B0000", "#AA2222"),
                ("蓝色", "#0055FF", "#3366DD"),
                ("深蓝", "#00008B", "#2222AA"),
                ("绿色", "#00AA00", "#33CC33"),
                ("墨绿", "#008B00", "#22AA22")
            };

            var pair = colorPairs[_random.Next(colorPairs.Count)];
            var options = new List<string> { pair.name, GetColorName(pair.similar) };
            while (options.Count < 4)
            {
                var extra = _colors[_random.Next(_colors.Count)].name;
                if (!options.Contains(extra)) options.Add(extra);
            }
            Shuffle(options);

            return new CaptchaChallenge
            {
                Type = 13,
                Level = level,
                Question = "🎨 下面文字是什么颜色？（仔细看！）",
                DisplayText = $"<span style=\"color:{pair.hex};font-size:3rem;font-weight:bold;\">██████</span>",
                CorrectAnswer = pair.name,
                Options = options,
                DisplayType = "color",
                TimeLimit = 6,
                Points = (10 + level) * 3,
                FunMessage = "🎯 好眼力！"
            };
        }

        private string GetColorName(string hex)
        {
            var map = new Dictionary<string, string>
            {
                {"#CC3333", "浅红"}, {"#AA2222", "暗红"},
                {"#3366DD", "浅蓝"}, {"#2222AA", "靛蓝"},
                {"#33CC33", "浅绿"}, {"#22AA22", "暗绿"}
            };
            return map.ContainsKey(hex) ? map[hex] : "未知";
        }

        // ============================================================
        // 类型14：超难找不同（71-75关）⭐ 完全重写，不用 char[]
        // ============================================================
        private CaptchaChallenge GenerateHardFindDifferentChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var target = chars[_random.Next(chars.Length)];
            var options = new List<string> { target.ToString() };

            // ⭐ 手动生成相似字符（全部是 string）
            var similar = new List<string>();
            if ("AEIOU".Contains(target))
            {
                similar.Add("Ä"); similar.Add("À"); similar.Add("Á");
            }
            else if ("BCDFGHJKLMNPQRSTVWXYZ".Contains(target))
            {
                similar.Add("ß"); similar.Add("Ɓ"); similar.Add("Ƃ");
            }
            else if (char.IsDigit(target))
            {
                similar.Add("Ƨ"); similar.Add("Ʒ");
            }

            // 添加相似字符到选项
            foreach (var s in similar)
            {
                if (options.Count >= 6) break;
                if (!options.Contains(s)) options.Add(s);
            }

            while (options.Count < 6)
            {
                var c = chars[_random.Next(chars.Length)];
                if (!options.Contains(c.ToString())) options.Add(c.ToString());
            }
            Shuffle(options);

            return new CaptchaChallenge
            {
                Type = 14,
                Level = level,
                Question = "🔍 从6个字符中找出不同的那个！",
                CorrectAnswer = target.ToString(),
                Options = options,
                DisplayType = "text",
                TimeLimit = 6,
                Points = (10 + level) * 3,
                FunMessage = "🔍 太厉害了！"
            };
        }

        // ============================================================
        // 类型15：倒序+最大扭曲（76-80关）
        // ============================================================
        private CaptchaChallenge GenerateHardReverseChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var len = 5 + (level % 3);
            var text = new string(Enumerable.Range(0, len).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
            var reversed = new string(text.Reverse().ToArray());
            var svg = GenerateHardSvg(text);

            return new CaptchaChallenge
            {
                Type = 15,
                Level = level,
                Question = "🔄 图片中的文字（倒过来了！）",
                ImageSvg = svg,
                CorrectAnswer = reversed.ToUpper(),
                Options = GenerateOptions(reversed, 4),
                DisplayType = "image",
                TimeLimit = 6,
                Points = (10 + level) * 3,
                FunMessage = "🔄 倒着也能认出！"
            };
        }

        // ============================================================
        // 类型16：双缺失字母（81-85关）
        // ============================================================
        private CaptchaChallenge GenerateHardMissingLetterChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            var len = 6 + (level % 3);
            var word = new string(Enumerable.Range(0, len).Select(_ => chars[_random.Next(chars.Length)]).ToArray());

            var idx1 = _random.Next(len);
            var idx2 = _random.Next(len);
            while (idx2 == idx1) idx2 = _random.Next(len);

            var correct1 = word[idx1];
            var correct2 = word[idx2];
            var display = word.ToCharArray();
            display[idx1] = '_';
            display[idx2] = '_';

            var correct = $"{correct1}{correct2}";

            return new CaptchaChallenge
            {
                Type = 16,
                Level = level,
                Question = $"🔤 补全两个缺失字母：{new string(display)}",
                CorrectAnswer = correct,
                Options = GenerateOptions(correct, 4),
                DisplayType = "text",
                TimeLimit = 6,
                Points = (10 + level) * 3,
                FunMessage = "🔤 字母大师！"
            };
        }

        // ============================================================
        // 类型17：极速点击（86-90关）
        // ============================================================
        private CaptchaChallenge GenerateHardQuickTapChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var target = chars[_random.Next(chars.Length)];
            var options = new List<string> { target.ToString() };
            while (options.Count < 6)
            {
                var c = chars[_random.Next(chars.Length)];
                if (!options.Contains(c.ToString())) options.Add(c.ToString());
            }
            Shuffle(options);
            var svg = GenerateQuickSvg(target);

            return new CaptchaChallenge
            {
                Type = 17,
                Level = level,
                Question = "⚡ 从6个选项中快速找到目标！",
                ImageSvg = svg,
                CorrectAnswer = target.ToString(),
                Options = options,
                DisplayType = "image",
                TimeLimit = 4,
                Points = (10 + level) * 4,
                FunMessage = "⚡ 神速！"
            };
        }

        // ============================================================
        // 类型18：生僻成语（91-95关）
        // ============================================================
        private CaptchaChallenge GenerateHardIdiomChallenge(int level)
        {
            var idioms = new[] { "龘龘齉齾", "爨爨灩灪", "鬱鬱灪灩", "驫驫麤麤", "鸞鸞灩灪" };
            var idiom = idioms[_random.Next(idioms.Length)];

            var pos = _random.Next(idiom.Length);
            var correct = idiom[pos];
            var display = idiom.ToCharArray();
            display[pos] = '□';

            return new CaptchaChallenge
            {
                Type = 18,
                Level = level,
                Question = $"📖 补全生僻成语：{new string(display)}",
                CorrectAnswer = correct.ToString(),
                Options = GenerateOptions(correct.ToString(), 4),
                DisplayType = "text",
                TimeLimit = 6,
                Points = (10 + level) * 4,
                FunMessage = "🏆 生僻字大师！"
            };
        }

        // ============================================================
        // 类型19：终极BOSS（96-100关）
        // ============================================================
        private CaptchaChallenge GenerateUltimateChallenge(int level)
        {
            var types = new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18 };
            var typeIdx = types[_random.Next(types.Length)];

            CaptchaChallenge challenge;
            switch (typeIdx)
            {
                case 10: challenge = GenerateHardTextChallenge(level); break;
                case 11: challenge = GenerateHardArithmeticChallenge(level); break;
                case 12: challenge = GenerateHardStrokeChallenge(level); break;
                case 13: challenge = GenerateHardColorChallenge(level); break;
                case 14: challenge = GenerateHardFindDifferentChallenge(level); break;
                case 15: challenge = GenerateHardReverseChallenge(level); break;
                case 16: challenge = GenerateHardMissingLetterChallenge(level); break;
                case 17: challenge = GenerateHardQuickTapChallenge(level); break;
                default: challenge = GenerateHardIdiomChallenge(level); break;
            }

            challenge.Type = 19;
            challenge.Level = level;
            challenge.TimeLimit = 4;
            challenge.Points = (10 + level) * 5;
            challenge.Question = $"💀 终极BOSS挑战！{challenge.Question}";
            challenge.FunMessage = GetUltimateMessage(level);

            return challenge;
        }

        // ============================================================
        // 工具方法
        // ============================================================

        private string GenerateSvg(string text)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"320\" height=\"90\" viewBox=\"0 0 320 90\">");
            sb.AppendLine($"<rect width=\"320\" height=\"90\" rx=\"10\" fill=\"#f0f0f0\"/>");

            for (int i = 0; i < 10; i++)
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
                var angle = _random.Next(-15, 15);
                var x = 20 + i * spacing + _random.Next(-5, 5);
                sb.AppendLine($"<text x=\"{x}\" y=\"50\" font-family=\"Arial, sans-serif\" font-size=\"36\" font-weight=\"bold\" fill=\"rgb({r},{g},{b})\" transform=\"rotate({angle} {x} 50)\" text-anchor=\"middle\" dominant-baseline=\"central\">{chars[i]}</text>");
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private string GenerateHardSvg(string text)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"320\" height=\"90\" viewBox=\"0 0 320 90\">");
            sb.AppendLine($"<rect width=\"320\" height=\"90\" rx=\"10\" fill=\"#f0f0f0\"/>");

            for (int i = 0; i < 20; i++)
            {
                var r = _random.Next(100, 220);
                var g = _random.Next(100, 220);
                var b = _random.Next(100, 220);
                sb.AppendLine($"<line x1=\"{_random.Next(0, 320)}\" y1=\"{_random.Next(0, 90)}\" x2=\"{_random.Next(0, 320)}\" y2=\"{_random.Next(0, 90)}\" stroke=\"rgb({r},{g},{b})\" stroke-width=\"{_random.Next(1, 3)}\" opacity=\"0.5\"/>");
            }

            var chars = text.ToCharArray();
            var spacing = 280 / chars.Length;
            for (int i = 0; i < chars.Length; i++)
            {
                var r = _random.Next(10, 80);
                var g = _random.Next(10, 80);
                var b = _random.Next(10, 80);
                var angle = _random.Next(-25, 25);
                var x = 20 + i * spacing + _random.Next(-8, 8);
                var y = 50 + _random.Next(-10, 10);
                sb.AppendLine($"<text x=\"{x}\" y=\"{y}\" font-family=\"Arial, sans-serif\" font-size=\"40\" font-weight=\"bold\" fill=\"rgb({r},{g},{b})\" transform=\"rotate({angle} {x} 50)\" text-anchor=\"middle\" dominant-baseline=\"central\">{chars[i]}</text>");
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

        private List<string> GenerateOptions(string correct, int count)
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

        private List<string> GenerateNumberOptions(int correct, int count)
        {
            var options = new List<string> { correct.ToString() };
            while (options.Count < count)
            {
                var fake = correct + _random.Next(-5, 6);
                if (fake < 0) fake = _random.Next(1, 20);
                var str = fake.ToString();
                if (!options.Contains(str)) options.Add(str);
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

        private string GetUltimateMessage(int level)
        {
            var msgs = new[] { "🏆 终极王者！", "💀 连AI都怕你！", "👑 你是人类之光！", "🚀 太逆天了！", "⚡ 无敌！" };
            return msgs[_random.Next(msgs.Length)];
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
