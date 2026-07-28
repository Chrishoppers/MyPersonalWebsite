using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MyPersonalWebsite.Services
{
    public class CaptchaGameService
    {
        private readonly Random _random = new();

        // 颜色映射
        private readonly Dictionary<string, string> _colorMap = new()
        {
            { "red", "🔴 红色" },
            { "blue", "🔵 蓝色" },
            { "green", "🟢 绿色" },
            { "yellow", "🟡 黄色" },
            { "purple", "🟣 紫色" },
            { "orange", "🟠 橙色" },
            { "pink", "🩷 粉色" },
            { "cyan", "🩵 青色" }
        };

        private readonly List<string> _colorValues = new()
        {
            "red", "blue", "green", "yellow", "purple", "orange", "pink", "cyan"
        };

        // ============================================================
        // 生成挑战（根据关卡难度）
        // ============================================================
        public CaptchaChallenge GenerateChallenge(int level)
        {
            // 根据关卡决定题目类型
            int type;
            if (level <= 3)
                type = _random.Next(0, 2);      // 算术 + 文字
            else if (level <= 6)
                type = _random.Next(1, 4);      // 算术 + 笔画 + 颜色
            else if (level <= 10)
                type = _random.Next(0, 5);      // 全部类型
            else
                type = _random.Next(0, 5);      // 全部类型 + 更高难度

            switch (type)
            {
                case 0: return GenerateTextChallenge(level);
                case 1: return GenerateArithmeticChallenge(level);
                case 2: return GenerateStrokeChallenge(level);
                case 3: return GenerateColorChallenge(level);
                case 4: return GenerateFindDifferentChallenge(level);
                default: return GenerateTextChallenge(level);
            }
        }

        // ============================================================
        // 类型0：扭曲文字识别（难度随关卡递增）
        // ============================================================
        private CaptchaChallenge GenerateTextChallenge(int level)
        {
            var length = level <= 3 ? 4 : level <= 8 ? 5 : 6;
            var text = GenerateRandomText(length);
            
            // 难度越高，扭曲越厉害
            var distortion = Math.Min(level * 2, 40);
            var svg = GenerateGameSvg(text, distortion);

            var options = GenerateDistractors(text, 3);

            return new CaptchaChallenge
            {
                Type = 0,
                Level = level,
                Question = GetFunQuestion(level, "👁️", "请输入下方图片中的文字"),
                ImageSvg = svg,
                CorrectAnswer = text.ToUpper(),
                Options = options,
                DisplayType = "image",
                TimeLimit = level > 10 ? 8 : 15,
                FunMessage = GetFunMessage(level)
            };
        }

        // ============================================================
        // 类型1：算术题（难度随关卡递增）
        // ============================================================
        private CaptchaChallenge GenerateArithmeticChallenge(int level)
        {
            var maxNum = 10 + level * 3;
            var a = _random.Next(5, maxNum);
            var b = _random.Next(1, maxNum / 2);
            var op = _random.Next(0, 3);

            int result;
            string opStr;
            string question;

            if (op == 0) { result = a + b; opStr = "+"; question = $"{a} + {b} = ?"; }
            else if (op == 1) { result = a - b; opStr = "−"; question = $"{a} − {b} = ?"; }
            else { result = a * b; opStr = "×"; question = $"{a} × {b} = ?"; }

            var options = GenerateNumberDistractors(result, 3, level);

            return new CaptchaChallenge
            {
                Type = 1,
                Level = level,
                Question = GetFunQuestion(level, "🧮", question),
                CorrectAnswer = result.ToString(),
                Options = options,
                DisplayType = "text",
                TimeLimit = level > 10 ? 6 : 12,
                FunMessage = GetFunMessage(level)
            };
        }

        // ============================================================
        // 类型2：汉字笔画数（难度随关卡递增）
        // ============================================================
        private CaptchaChallenge GenerateStrokeChallenge(int level)
        {
            var strokeMap = GetStrokeMap();
            var keys = new List<char>(strokeMap.Keys);
            
            // 高关卡使用生僻字
            char ch;
            if (level > 8)
            {
                var hardKeys = new List<char> { '龘', '爨', '鬱', '灩', '驫' };
                ch = hardKeys[_random.Next(hardKeys.Count)];
            }
            else
            {
                ch = keys[_random.Next(keys.Count)];
            }

            var stroke = strokeMap.ContainsKey(ch) ? strokeMap[ch] : _random.Next(10, 25);

            var question = $"「{ch}」字有几画？";

            var options = GenerateStrokeDistractors(stroke, 3, level);

            return new CaptchaChallenge
            {
                Type = 2,
                Level = level,
                Question = GetFunQuestion(level, "📝", question),
                CorrectAnswer = stroke.ToString(),
                Options = options,
                DisplayType = "text",
                TimeLimit = level > 10 ? 8 : 15,
                FunMessage = GetFunMessage(level)
            };
        }

        // ============================================================
        // 类型3：颜色识别（难度随关卡递增）
        // ============================================================
        private CaptchaChallenge GenerateColorChallenge(int level)
        {
            var colorIndex = _random.Next(_colorValues.Count);
            var color = _colorValues[colorIndex];
            var colorName = _colorMap[color];

            // 高关卡使用相近颜色混淆
            var options = new List<string> { colorName };
            var wrongColors = new List<string>(_colorValues);
            wrongColors.Remove(color);

            if (level > 5)
            {
                // 高关卡只选相近色
                var similarColors = GetSimilarColors(color);
                foreach (var c in similarColors)
                {
                    if (options.Count < 4 && _colorMap.ContainsKey(c))
                        options.Add(_colorMap[c]);
                }
            }

            while (options.Count < 4)
            {
                var wrong = wrongColors[_random.Next(wrongColors.Count)];
                var wrongName = _colorMap[wrong];
                if (!options.Contains(wrongName))
                    options.Add(wrongName);
            }

            // 打乱选项
            Shuffle(options);

            // 颜色用彩色文字显示
            var colorText = GetColorText(color);

            return new CaptchaChallenge
            {
                Type = 3,
                Level = level,
                Question = GetFunQuestion(level, "🎨", $"下面文字是什么颜色？"),
                DisplayText = colorText,
                CorrectAnswer = colorName,
                Options = options,
                DisplayType = "color",
                TimeLimit = level > 8 ? 6 : 12,
                FunMessage = GetFunMessage(level)
            };
        }

        // ============================================================
        // 类型4：找不同（高关卡专属）
        // ============================================================
        private CaptchaChallenge GenerateFindDifferentChallenge(int level)
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var target = chars[_random.Next(chars.Length)];
            var wrongChar = chars[_random.Next(chars.Length)];
            
            while (wrongChar == target)
                wrongChar = chars[_random.Next(chars.Length)];

            var options = new List<string> { target.ToString() };
            while (options.Count < 4)
            {
                var c = chars[_random.Next(chars.Length)];
                if (!options.Contains(c.ToString()))
                    options.Add(c.ToString());
            }
            Shuffle(options);

            return new CaptchaChallenge
            {
                Type = 4,
                Level = level,
                Question = GetFunQuestion(level, "🔍", "哪个字符与其他的不同？"),
                CorrectAnswer = target.ToString(),
                Options = options,
                DisplayType = "text",
                TimeLimit = level > 8 ? 5 : 10,
                FunMessage = GetFunMessage(level)
            };
        }

        // ============================================================
        // 生成游戏用 SVG（带扭曲）
        // ============================================================
        private string GenerateGameSvg(string text, int distortion)
        {
            int width = 300;
            int height = 80;

            var svg = new StringBuilder();
            svg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">");

            var bgR = _random.Next(230, 255);
            var bgG = _random.Next(230, 255);
            var bgB = _random.Next(230, 255);
            svg.AppendLine($"  <rect width=\"{width}\" height=\"{height}\" rx=\"8\" fill=\"rgb({bgR},{bgG},{bgB})\" />");

            // 干扰线（数量随难度增加）
            int lineCount = 10 + distortion / 2;
            for (int i = 0; i < lineCount; i++)
            {
                var r = _random.Next(100, 220);
                var g = _random.Next(100, 220);
                var b = _random.Next(100, 220);
                var x1 = _random.Next(-20, width + 20);
                var y1 = _random.Next(-20, height + 20);
                var x2 = _random.Next(-20, width + 20);
                var y2 = _random.Next(-20, height + 20);
                svg.AppendLine($"  <line x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\" stroke=\"rgb({r},{g},{b})\" stroke-width=\"{_random.Next(1, 3)}\" opacity=\"0.4\" />");
            }

            // 绘制字符
            var chars = text.ToCharArray();
            int spacing = (width - 40) / chars.Length;

            for (int i = 0; i < chars.Length; i++)
            {
                var cr = _random.Next(10, 80);
                var cg = _random.Next(10, 80);
                var cb = _random.Next(10, 80);
                var angle = _random.Next(-distortion / 2, distortion / 2);
                var fontSize = 36 + distortion / 4;
                var x = 20 + i * spacing + _random.Next(-5, 5);
                var y = height / 2 + 10 + _random.Next(-8, 8);

                svg.AppendLine($@"
  <text x=""{x}"" y=""{y}"" 
        font-family=""Arial, sans-serif"" 
        font-size=""{fontSize}"" 
        font-weight=""bold""
        fill=""rgb({cr},{cg},{cb})""
        transform=""rotate({angle} {x} {y})""
        text-anchor=""middle""
        dominant-baseline=""central"">
    {chars[i]}
  </text>");
            }

            svg.AppendLine("</svg>");
            return svg.ToString();
        }

        // ============================================================
        // 辅助方法
        // ============================================================

        private string GenerateRandomText(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var result = new char[length];
            for (int i = 0; i < length; i++)
                result[i] = chars[_random.Next(chars.Length)];
            return new string(result);
        }

        private List<string> GenerateDistractors(string correct, int count)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var options = new List<string> { correct };
            while (options.Count < count + 1)
            {
                var fake = new char[correct.Length];
                for (int i = 0; i < correct.Length; i++)
                    fake[i] = chars[_random.Next(chars.Length)];
                var fakeStr = new string(fake);
                if (!options.Contains(fakeStr) && fakeStr != correct)
                    options.Add(fakeStr);
            }
            Shuffle(options);
            return options;
        }

        private List<string> GenerateNumberDistractors(int correct, int count, int level)
        {
            var range = 5 + level;
            var options = new List<string> { correct.ToString() };
            while (options.Count < count + 1)
            {
                var fake = correct + _random.Next(-range, range);
                var fakeStr = fake.ToString();
                if (!options.Contains(fakeStr) && fakeStr != correct.ToString())
                    options.Add(fakeStr);
            }
            Shuffle(options);
            return options;
        }

        private List<string> GenerateStrokeDistractors(int correct, int count, int level)
        {
            var options = new List<string> { correct.ToString() };
            var range = level <= 3 ? 3 : level <= 6 ? 5 : 8;
            while (options.Count < count + 1)
            {
                var fake = correct + _random.Next(-range, range);
                if (fake < 1) fake = _random.Next(1, 10);
                var fakeStr = fake.ToString();
                if (!options.Contains(fakeStr) && fakeStr != correct.ToString())
                    options.Add(fakeStr);
            }
            Shuffle(options);
            return options;
        }

        private List<string> GetSimilarColors(string color)
        {
            var similar = new Dictionary<string, List<string>>
            {
                { "red", new List<string> { "pink", "orange", "purple" } },
                { "blue", new List<string> { "cyan", "purple", "green" } },
                { "green", new List<string> { "cyan", "yellow", "blue" } },
                { "yellow", new List<string> { "orange", "green", "pink" } },
                { "purple", new List<string> { "pink", "blue", "red" } },
                { "orange", new List<string> { "yellow", "red", "pink" } },
                { "pink", new List<string> { "red", "purple", "orange" } },
                { "cyan", new List<string> { "blue", "green", "purple" } }
            };
            return similar.ContainsKey(color) ? similar[color] : new List<string>();
        }

        private string GetColorText(string color)
        {
            var colorHex = new Dictionary<string, string>
            {
                { "red", "#FF0000" },
                { "blue", "#0055FF" },
                { "green", "#00AA00" },
                { "yellow", "#DDBB00" },
                { "purple", "#8800CC" },
                { "orange", "#FF6600" },
                { "pink", "#FF4499" },
                { "cyan", "#00CCCC" }
            };

            var displayText = "██████████";
            var hex = colorHex.ContainsKey(color) ? colorHex[color] : "#000000";
            return $"<span style=\"color:{hex};font-weight:bold;font-size:2rem;\">{displayText}</span>";
        }

        private string GetFunQuestion(int level, string icon, string question)
        {
            var prefixes = level <= 3 ? new[] { "😊 小菜一碟", "👍 简单" } :
                          level <= 6 ? new[] { "🤔 有点意思", "🧐 仔细看" } :
                          level <= 10 ? new[] { "😤 来真的了", "🔥 加油" } :
                          new[] { "💪 你是人类吗", "👑 大佬加油", "⚡ 快到极限了" };
            var prefix = prefixes[_random.Next(prefixes.Length)];
            return $"{prefix} · {question}";
        }

        private string GetFunMessage(int level)
        {
            var messages = new Dictionary<int, string[]>
            {
                { 1, new[] { "✨ 轻轻松松！", "🎯 精准命中！" } },
                { 2, new[] { "⚡ 反应不错！", "👀 眼神真好！" } },
                { 3, new[] { "🔥 开始热身了！", "💪 状态不错！" } },
                { 4, new[] { "🧠 脑力全开！", "🎮 游戏开始了！" } },
                { 5, new[] { "🌟 你是人类之光！", "🚀 冲啊！" } },
                { 6, new[] { "⚡ 连击！太强了！", "🔥 根本停不下来！" } },
                { 7, new[] { "👑 人类之王！", "💀 AI已崩溃！" } },
                { 8, new[] { "🤖 你不是AI吧？", "🔱 太逆天了！" } },
                { 9, new[] { "🚨 警报！真人出没！", "⚡ 你就是验证码克星！" } },
                { 10, new[] { "🏆 十连击！传说级！", "🌟 你已经是传奇了！" } }
            };

            var msgs = messages.ContainsKey(level) ? messages[level] : 
                      new[] { "✨ 继续挑战！", "💪 你是最强的！" };
            return msgs[_random.Next(msgs.Length)];
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private Dictionary<char, int> GetStrokeMap()
        {
            return new Dictionary<char, int>
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
            };
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
        public string DisplayType { get; set; } = "text"; // image, text, color
        public int TimeLimit { get; set; } = 15;
        public string FunMessage { get; set; } = "";
    }
}
