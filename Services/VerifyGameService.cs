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
            ("黑色", "#000000"), ("白色", "#FFFFFF"), ("灰色", "#888888"),
            ("金色", "#DAA520"), ("银色", "#C0C0C0"), ("深红", "#8B0000"),
            ("天蓝", "#00BFFF"), ("墨绿", "#008B00"), ("紫罗兰", "#8B00FF")
        };

        private readonly List<string> _idioms = new()
        {
            "一马当先", "龙飞凤舞", "画蛇添足", "守株待兔", "狐假虎威",
            "马到成功", "鸟语花香", "鱼目混珠", "鹤立鸡群", "龙腾虎跃",
            "画龙点睛", "亡羊补牢", "杯弓蛇影", "指鹿为马", "马不停蹄",
            "龙争虎斗", "杯水车薪", "纸上谈兵", "鸡犬不宁", "牛刀小试",
            "羊入虎口", "虎头蛇尾", "狗急跳墙", "狐朋狗友", "牛鬼蛇神",
            "蝇头小利", "鹤发童颜", "螳臂当车", "鼠目寸光", "虎踞龙盘"
        };

        private readonly Dictionary<char, int> _strokeMap = new()
        {
            {'一',1},{'二',2},{'三',3},{'四',5},{'五',4},{'六',4},{'七',2},{'八',2},{'九',2},{'十',2},
            {'人',2},{'大',3},{'天',4},{'地',6},{'日',4},{'月',4},{'水',4},{'火',4},{'山',3},{'石',5},
            {'木',4},{'花',7},{'草',9},{'鸟',5},{'鱼',8},{'马',3},{'牛',4},{'羊',6},{'虫',6},{'云',4},
            {'风',4},{'雨',8},{'雪',11},{'星',9},{'光',6},{'春',9},{'夏',10},{'秋',9},{'冬',5},{'年',6},
            {'好',6},{'学',8},{'生',5},{'中',4},{'国',8},{'家',10},{'心',4},{'爱',10},{'乐',5},{'安',6},
            {'永',5},{'远',7},{'梦',11},{'想',13},{'飞',3},{'行',6},{'白',5},{'黑',12},{'红',6},{'绿',11},
            {'蓝',13},{'紫',12},{'金',8},{'银',11},{'龙',5},{'虎',8},{'凤',4},{'凰',11},{'喜',12},{'欢',6},
            {'笑',10},{'哭',10},{'甜',11},{'苦',8},{'美',9},{'丽',7},{'明',8},{'亮',9},{'新',13},{'旧',5},
            {'高',10},{'低',7},{'长',4},{'短',12},{'快',7},{'慢',14},{'多',6},{'少',4},{'真',10},{'假',11},
            {'善',12},{'恶',10},{'清',11},{'浊',10},{'深',11},{'浅',8},{'浓',10},{'淡',11},{'远',7},{'近',7},
            {'繁',17},{'體',23},{'漢',14},{'學',16},{'難',19},{'驗',23},{'證',19},{'碼',15},
            {'龘',48},{'爨',30},{'鬱',29},{'灩',28},{'驫',30},{'鸞',30},{'麤',33},{'龖',32},
            {'齉',36},{'齾',35},{'龗',33},{'灪',29},{'籲',32},{'爩',33},{'䨻',44},{'䲜',28}
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
        // 类型0-18 方法保持不变，这里省略重复代码...
        // ============================================================

        // ⭐ 修复：所有 char 转 string 的方法
        private List<string> GenerateOptionsFromChar(char correct, int count)
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

        // ⭐ 修复：所有 char 转 string 的通用方法
        private List<string> GenerateOptions(char correct, int count)
        {
            return GenerateOptionsFromChar(correct, count);
        }

        // 原 GenerateOptions(string correct, int count) 保持不变
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
        // 类型0：文字扭曲识别
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
                Options = GenerateOptions(text, 4),
                DisplayType = "image",
                TimeLimit = 15,
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型1：算术计算
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
        // 类型2：汉字笔画数
        // ============================================================
        private CaptchaChallenge GenerateStrokeChallenge(int level)
        {
            var progress = (level - 11) % 5 + 1;
            var keys = _strokeMap.Keys.ToList();
            var ch = keys[_random.Next(keys.Count)];
            var stroke = _strokeMap[ch];

            if (progress == 5)
            {
                var hardKeys = new[] { '龘', '爨', '鬱', '灩', '驫', '鸞', '麤', '龖' };
                ch = hardKeys[_random.Next(hardKeys.Length)];
                stroke = _strokeMap.ContainsKey(ch) ? _strokeMap[ch] : _random.Next(25, 50);
            }

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
        // 类型3：颜色识别
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
        // 类型4：找不同
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
        // 类型5：倒序识别
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
                Options = GenerateOptions(reversed, 4),
                DisplayType = "image",
                TimeLimit = GetTimeLimit(level) + 2,
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型6：缺失字母
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
                Options = GenerateOptions(correct.ToString(), 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型7：快速点击
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
        // 类型8：成语填空
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
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型9：中文数字转阿拉伯
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
        // 类型10：大小写转换
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
                Options = GenerateOptions(isUpper ? c.ToString().ToLower() : c.ToString(), 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型11：拼音首字母
        // ============================================================
        private CaptchaChallenge GeneratePinyinChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            var c = chars[_random.Next(chars.Length)];
            var pinyin = new Dictionary<char, string> {
                {'A',"ei"},{'B',"bi"},{'C',"xi"},{'D',"di"},{'E',"yi"},{'F',"efu"},{'G',"ji"},{'H',"eichi"},
                {'J',"jie"},{'K',"kei"},{'L',"el"},{'M',"em"},{'N',"en"},{'P',"pi"},{'Q',"qiu"},{'R',"ar"},
                {'S',"es"},{'T',"ti"},{'W',"dabuliu"},{'X',"eks"},{'Y',"wai"},{'Z',"zi"}
            };

            return new CaptchaChallenge
            {
                Type = 11,
                Level = level,
                Question = $"🔊 字母「{c}」的拼音是？",
                CorrectAnswer = pinyin.ContainsKey(c) ? pinyin[c] : c.ToString().ToLower(),
                Options = GenerateOptions(pinyin.ContainsKey(c) ? pinyin[c] : c.ToString().ToLower(), 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型12：反色识别
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
        // 类型13：镜像文字
        // ============================================================
        private CaptchaChallenge GenerateMirrorChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            var c = chars[_random.Next(chars.Length)];
            var mirror = new Dictionary<char, char> {
                {'A','A'},{'B','B'},{'C','C'},{'D','D'},{'E','E'},{'H','H'},{'I','I'},{'M','M'},
                {'O','O'},{'T','T'},{'U','U'},{'V','V'},{'W','W'},{'X','X'},{'Y','Y'}
            };

            return new CaptchaChallenge
            {
                Type = 13,
                Level = level,
                Question = $"🪞 字母「{c}」的镜像字母是？",
                CorrectAnswer = mirror.ContainsKey(c) ? c.ToString() : c.ToString(),
                Options = GenerateOptions(mirror.ContainsKey(c) ? c.ToString() : c.ToString(), 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型14：键盘相邻键
        // ============================================================
        private CaptchaChallenge GenerateKeyboardNeighborChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            var c = chars[_random.Next(chars.Length)];
            var neighbors = new Dictionary<char, char[]> {
                {'A', new[]{'Q','W','S','Z'}},
                {'B', new[]{'G','H','N','V'}},
                {'C', new[]{'X','D','F','V'}},
                {'D', new[]{'S','E','F','C','X'}},
                {'E', new[]{'W','R','D','S'}},
                {'F', new[]{'D','R','G','V','C'}},
                {'G', new[]{'F','T','H','B','V'}},
                {'H', new[]{'G','Y','J','N','B'}},
                {'J', new[]{'H','U','K','M','N'}},
                {'K', new[]{'J','I','L','M'}},
                {'L', new[]{'K','O','P'}},
                {'M', new[]{'N','J','K'}},
                {'N', new[]{'B','H','J','M'}},
                {'P', new[]{'O','L'}},
                {'Q', new[]{'W','A'}},
                {'R', new[]{'E','T','F','D'}},
                {'S', new[]{'A','W','D','X','Z'}},
                {'T', new[]{'R','Y','G','F'}},
                {'U', new[]{'Y','I','J','H'}},
                {'V', new[]{'C','F','G','B'}},
                {'W', new[]{'Q','E','S','A'}},
                {'X', new[]{'Z','S','D','C'}},
                {'Y', new[]{'T','U','H','G'}},
                {'Z', new[]{'A','S','X'}}
            };

            var neighbor = neighbors.ContainsKey(c) ? neighbors[c][_random.Next(neighbors[c].Length)] : c;

            return new CaptchaChallenge
            {
                Type = 14,
                Level = level,
                Question = $"⌨️ 键盘上「{c}」的右边键是？",
                CorrectAnswer = neighbor.ToString(),
                Options = GenerateOptions(neighbor.ToString(), 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型15：汉字拆分
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
        // 类型16：数字记忆
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
                Options = GenerateOptions(text, 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level) + 2,
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型17：方向判断
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
        // 类型18：字符计数
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
        // 类型19：终极混合
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
