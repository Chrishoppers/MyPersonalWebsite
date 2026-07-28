using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyPersonalWebsite.Services
{
    public class CaptchaGameService
    {
        private readonly Random _random = new();

        // ===== 50+颜色池 =====
        private readonly List<(string name, string hex)> _colors = new()
        {
            ("红色", "#FF0000"), ("深红", "#CC0000"), ("粉红", "#FF69B4"), ("玫红", "#FF007F"),
            ("橙红", "#FF4500"), ("朱红", "#FF2400"), ("大红", "#DC143C"), ("暗红", "#8B0000"),
            ("蓝色", "#0055FF"), ("深蓝", "#00008B"), ("天蓝", "#00BFFF"), ("藏青", "#000080"),
            ("宝蓝", "#0000CD"), ("湖蓝", "#30D5C8"), ("靛蓝", "#4B0082"), ("蔚蓝", "#007FFF"),
            ("绿色", "#00AA00"), ("深绿", "#006400"), ("翠绿", "#00CD00"), ("墨绿", "#008B00"),
            ("草绿", "#7CFC00"), ("碧绿", "#2ECC71"), ("橄榄", "#808000"), ("青绿", "#008080"),
            ("黄色", "#DDBB00"), ("金色", "#DAA520"), ("橙黄", "#FFA500"), ("柠檬", "#FFF000"),
            ("土黄", "#CC7722"), ("米黄", "#F5DEB3"), ("香槟", "#F7E7CE"), ("芥末", "#C5B358"),
            ("紫色", "#8800CC"), ("紫罗兰", "#8B00FF"), ("茄子", "#69359C"), ("淡紫", "#DDA0DD"),
            ("粉色", "#FFB6C1"), ("桃色", "#FFDAB9"), ("珊瑚", "#FF7F50"), ("玫瑰", "#FF0080"),
            ("橙色", "#FF6600"), ("深橙", "#E65100"), ("杏色", "#FFD700"), ("琥珀", "#FFBF00"),
            ("灰色", "#888888"), ("银色", "#C0C0C0"), ("深灰", "#404040"), ("烟灰", "#708090"),
            ("棕色", "#8B4513"), ("咖啡", "#6F4E37"), ("古铜", "#CD7F32"), ("巧克力", "#7B3F00"),
            ("黑色", "#000000"), ("白色", "#FFFFFF"), ("米色", "#F5F5DC"), ("象牙", "#FFFFF0")
        };

        // ===== 生僻字库 =====
        private readonly (string ch, int stroke)[] _rareChars = new[]
        {
            ("繁", 17), ("體", 23), ("漢", 14), ("學", 16), ("難", 19), ("驗", 23), ("證", 19), ("碼", 15),
            ("龘", 48), ("爨", 30), ("鬱", 29), ("灩", 28), ("驫", 30), ("鸞", 30), ("麤", 33), ("龖", 32),
            ("齉", 36), ("齾", 35), ("龗", 33), ("灪", 29), ("籲", 32), ("爩", 33), ("䨻", 44), ("䲜", 28),
            ("䴙", 25), ("䴘", 24), ("䴗", 23), ("䴖", 22), ("䴕", 21), ("䴔", 20), ("𠀀", 30), ("𠀁", 32),
            ("𪚥", 64), ("𪚦", 62), ("𪚧", 60), ("𪚨", 58), ("𪚩", 56), ("𪚪", 54), ("𪚫", 52), ("𪚬", 50)
        };

        // ===== 成语库 =====
        private readonly List<string> _idioms = new()
        {
            "一马当先", "龙飞凤舞", "画蛇添足", "守株待兔", "狐假虎威", "马到成功", "鸟语花香", "鱼目混珠",
            "鹤立鸡群", "龙腾虎跃", "画龙点睛", "叶公好龙", "亡羊补牢", "杯弓蛇影", "指鹿为马", "鸟尽弓藏",
            "兔死狐悲", "狼吞虎咽", "马不停蹄", "龙争虎斗", "杯水车薪", "抱薪救火", "纸上谈兵", "鸡犬不宁",
            "牛刀小试", "羊入虎口", "虎头蛇尾", "狗急跳墙", "狐朋狗友", "牛鬼蛇神", "蝇头小利", "鹤发童颜",
            "螳臂当车", "鼠目寸光", "虎踞龙盘", "狼心狗肺", "龙潭虎穴", "鸡鸣狗盗", "兔起鹘落", "燕雀安知",
            "鹏程万里", "鹤唳华亭", "鸾凤和鸣", "龙骧虎步", "虎背熊腰", "狼烟四起", "狐疑不决", "牛头马面"
        };

        // ===== 生僻成语 =====
        private readonly List<string> _rareIdioms = new()
        {
            "龘龘齉齾", "爨爨灩灪", "鬱鬱灪灩", "驫驫麤麤", "鸞鸞灩灪",
            "龖龖爨爨", "齉齉齾齾", "灪灪灩灩", "籲籲鬱鬱", "龗龗麤麤",
            "䨻䨻䲜䲜", "䴙䴘䴗䴖", "䴕䴔䴙䴘", "𠀀𠀁𠀂𠀃", "𪚥𪚦𪚧𪚨"
        };

        // ============================================================
        // 主入口
        // ============================================================
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
        // 类型0-9（基础类型，代码省略，保持简洁）
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
                Question = GetQuestion(level, "👁️", "请输入下方图片中的文字"),
                ImageSvg = svg,
                CorrectAnswer = text.ToUpper(),
                Options = GenerateOptions(text, 4),
                DisplayType = "image",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        private CaptchaChallenge GenerateArithmeticChallenge(int level)
        {
            var progress = (level - 6) % 5 + 1;
            var maxNum = 10 + progress * 8;
            var a = _random.Next(5, maxNum);
            var b = _random.Next(1, maxNum / 2);
            var ops = new[] { "+", "-", "×" };
            var op = ops[_random.Next(3)];
            int result = op == "+" ? a + b : op == "-" ? a - b : a * b;

            return new CaptchaChallenge
            {
                Type = 1,
                Level = level,
                Question = GetQuestion(level, "🧮", $"{a} {op} {b} = ?"),
                CorrectAnswer = result.ToString(),
                Options = GenerateNumberOptions(result, 4, level),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        private CaptchaChallenge GenerateStrokeChallenge(int level)
        {
            var chars = "一二三四五六七八九十人大小天地日月水火山石木花草鸟鱼马牛羊虫风云雨雪星光春夏秋冬年好学生学习中国家中";
            var ch = chars[_random.Next(chars.Length)];
            var stroke = GetStroke(ch);

            return new CaptchaChallenge
            {
                Type = 2,
                Level = level,
                Question = GetQuestion(level, "📝", $"「{ch}」字有几画？"),
                CorrectAnswer = stroke.ToString(),
                Options = GenerateNumberOptions(stroke, 4, level),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        private int GetStroke(char ch)
        {
            var map = new Dictionary<char, int>
            {
                {'一',1},{'二',2},{'三',3},{'四',5},{'五',4},{'六',4},{'七',2},{'八',2},{'九',2},{'十',2},
                {'人',2},{'大',3},{'天',4},{'地',6},{'日',4},{'月',4},{'水',4},{'火',4},{'山',3},{'石',5},
                {'木',4},{'花',7},{'草',9},{'鸟',5},{'鱼',8},{'马',3},{'牛',4},{'羊',6},{'虫',6},{'云',4},
                {'风',4},{'雨',8},{'雪',11},{'星',9},{'光',6},{'春',9},{'夏',10},{'秋',9},{'冬',5},{'年',6},
                {'好',6},{'学',8},{'生',5},{'中',4},{'国',8},{'家',10},{'心',4},{'爱',10},{'乐',5},{'安',6}
            };
            return map.ContainsKey(ch) ? map[ch] : _random.Next(3, 12);
        }

        private CaptchaChallenge GenerateColorChallenge(int level)
        {
            var idx = _random.Next(_colors.Count);
            var color = _colors[idx];
            var options = new List<string> { color.name };
            var pool = new List<(string name, string hex)>(_colors);
            pool.RemoveAt(idx);
            while (options.Count < 4 && pool.Any())
            {
                var r = _random.Next(pool.Count);
                options.Add(pool[r].name);
                pool.RemoveAt(r);
            }
            Shuffle(options);

            return new CaptchaChallenge
            {
                Type = 3,
                Level = level,
                Question = GetQuestion(level, "🎨", "下面文字是什么颜色？"),
                DisplayText = $"<span style=\"color:{color.hex};font-size:2.5rem;font-weight:bold;\">██████</span>",
                CorrectAnswer = color.name,
                Options = options,
                DisplayType = "color",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

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
                Question = GetQuestion(level, "🔍", "哪个字符与其他的不同？"),
                CorrectAnswer = target.ToString(),
                Options = options,
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

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
                Question = GetQuestion(level, "🔄", "图片中的文字是什么？（倒过来了）"),
                ImageSvg = svg,
                CorrectAnswer = reversed.ToUpper(),
                Options = GenerateOptions(reversed, 4),
                DisplayType = "image",
                TimeLimit = GetTimeLimit(level) + 2,
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

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
                Question = GetQuestion(level, "🔤", $"补全单词：{new string(display)}"),
                CorrectAnswer = correct.ToString(),
                Options = GenerateOptions(correct.ToString(), 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

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
                Question = GetQuestion(level, "⚡", "快速找到目标字符！"),
                ImageSvg = svg,
                CorrectAnswer = target.ToString(),
                Options = options,
                DisplayType = "image",
                TimeLimit = Math.Max(3, 8 - level / 8),
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

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
                Question = GetQuestion(level, "📖", $"补全成语：{new string(display)}"),
                CorrectAnswer = correct.ToString(),
                Options = GenerateOptions(correct.ToString(), 4),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level) * 2,
                FunMessage = GetMessage(level)
            };
        }

        private CaptchaChallenge GenerateChineseNumberChallenge(int level)
        {
            var cn = new[] { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
            var num = _random.Next(0, 11);
            var display = cn[num];

            return new CaptchaChallenge
            {
                Type = 9,
                Level = level,
                Question = GetQuestion(level, "🔢", $"「{display}」对应的数字是？"),
                CorrectAnswer = num.ToString(),
                Options = GenerateNumberOptions(num, 4, level),
                DisplayType = "text",
                TimeLimit = GetTimeLimit(level),
                Points = GetPoints(level),
                FunMessage = GetMessage(level)
            };
        }

        // ============================================================
        // 类型10-19（高难度类型）
        // ============================================================

        private CaptchaChallenge GenerateHardTextChallenge(int level)
        {
            var progress = (level - 51) % 5 + 1;
            var len = Math.Min(5 + progress, 9);
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var text = new string(Enumerable.Range(0, len).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
            var svg = GenerateSvg(text, 20 + progress * 6, 30 + progress * 5);

            return new CaptchaChallenge
            {
                Type = 10,
                Level = level,
                Question = GetHardQuestion(level, "👁️", "请输入下方极度扭曲的文字"),
                ImageSvg = svg,
                CorrectAnswer = text.ToUpper(),
                Options = GenerateOptions(text, 4 + progress / 2),
                DisplayType = "image",
                TimeLimit = Math.Max(3, 8 - progress),
                Points = GetHardPoints(level),
                FunMessage = GetHardMessage(level)
            };
        }

        private CaptchaChallenge GenerateHardArithmeticChallenge(int level)
        {
            var progress = (level - 56) % 5 + 1;
            var (a, b, c, op, op2) = progress switch
            {
                1 => (_random.Next(100, 500), _random.Next(10, 99), 0, "×", ""),
                2 => (_random.Next(100, 999), _random.Next(10, 99), 0, "÷", ""),
                3 => (_random.Next(100, 999), _random.Next(10, 99), _random.Next(1, 50), "×", "+"),
                4 => (_random.Next(100, 999), _random.Next(10, 99), _random.Next(1, 50), "÷", "-"),
                _ => (_random.Next(100, 999), _random.Next(10, 99), _random.Next(1, 50), "×", "+")
            };

            int result;
            string question;
            if (string.IsNullOrEmpty(op2))
            {
                result = op == "×" ? a * b : a / b;
                question = $"{a} {op} {b} = ?";
            }
            else
            {
                var temp = op == "×" ? a * b : a / b;
                result = op2 == "+" ? temp + c : temp - c;
                question = $"({a} {op} {b}) {op2} {c} = ?";
            }

            return new CaptchaChallenge
            {
                Type = 11,
                Level = level,
                Question = GetHardQuestion(level, "🧮", question),
                CorrectAnswer = result.ToString(),
                Options = GenerateNumberOptions(result, 4 + progress / 2, level),
                DisplayType = "text",
                TimeLimit = Math.Max(3, 6 - progress),
                Points = GetHardPoints(level) * 2,
                FunMessage = GetHardMessage(level)
            };
        }

        private CaptchaChallenge GenerateHardStrokeChallenge(int level)
        {
            var progress = (level - 61) % 5 + 1;
            var idx = Math.Min((progress - 1) * 2 + _random.Next(0, 2), _rareChars.Length - 1);
            var (ch, stroke) = _rareChars[idx];

            return new CaptchaChallenge
            {
                Type = 12,
                Level = level,
                Question = GetHardQuestion(level, "📝", $"「{ch}」字有几画？"),
                CorrectAnswer = stroke.ToString(),
                Options = GenerateNumberOptions(stroke, 4 + progress / 2, level),
                DisplayType = "text",
                TimeLimit = Math.Max(3, 7 - progress),
                Points = GetHardPoints(level) * 2,
                FunMessage = GetHardMessage(level)
            };
        }

        private CaptchaChallenge GenerateHardColorChallenge(int level)
        {
            var progress = (level - 66) % 5 + 1;
            var optionsCount = Math.Min(4 + progress, 7);

            var colorIdx = _random.Next(_colors.Count);
            var correct = _colors[colorIdx];
            var options = new List<(string name, string hex)> { correct };
            var pool = new List<(string name, string hex)>(_colors);

            for (int i = 1; i < optionsCount; i++)
            {
                var nearbyIdx = (colorIdx + i * 3 + _random.Next(0, 2)) % _colors.Count;
                if (nearbyIdx != colorIdx && !options.Contains(_colors[nearbyIdx]))
                    options.Add(_colors[nearbyIdx]);
            }

            while (options.Count < optionsCount)
            {
                var r = _random.Next(_colors.Count);
                if (!options.Contains(_colors[r]))
                    options.Add(_colors[r]);
            }

            Shuffle(options);

            return new CaptchaChallenge
            {
                Type = 13,
                Level = level,
                Question = GetHardQuestion(level, "🎨", "下面文字是什么颜色？（仔细看！）"),
                DisplayText = $"<span style=\"color:{correct.hex};font-size:3rem;font-weight:bold;\">██████</span>",
                CorrectAnswer = correct.name,
                Options = options.Select(o => o.name).ToList(),
                DisplayType = "color",
                TimeLimit = Math.Max(3, 6 - progress),
                Points = GetHardPoints(level) * 2,
                FunMessage = GetHardMessage(level)
            };
        }

        private CaptchaChallenge GenerateHardFindDifferentChallenge(int level)
        {
            var progress = (level - 71) % 5 + 1;
            var optionsCount = Math.Min(6 + progress, 11);
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var target = chars[_random.Next(chars.Length)];

            var options = new List<string> { target.ToString() };
            var similar = GetSimilarChars(target);

            // ⭐ 修复：使用 ToString() 转换 char 为 string
            for (int i = 0; i < optionsCount - 1 && i < similar.Count; i++)
            {
                options.Add(similar[i].ToString());
            }

            while (options.Count < optionsCount)
            {
                var c = chars[_random.Next(chars.Length)];
                if (!options.Contains(c.ToString()) && c != target)
                    options.Add(c.ToString());
            }

            Shuffle(options);

            return new CaptchaChallenge
            {
                Type = 14,
                Level = level,
                Question = GetHardQuestion(level, "🔍", $"从 {optionsCount} 个字符中找出不同的那个！"),
                CorrectAnswer = target.ToString(),
                Options = options,
                DisplayType = "text",
                TimeLimit = Math.Max(4, 8 - progress),
                Points = GetHardPoints(level) * 3,
                FunMessage = GetHardMessage(level)
            };
        }

        private List<char> GetSimilarChars(char c)
        {
            var map = new Dictionary<char, char[]>
            {
                {'A', new[]{'Ä','À','Á','Â','Ã'}},
                {'B', new[]{'8','ß','Ɓ','Ƃ'}},
                {'C', new[]{'Ç','Ć','Č','©'}},
                {'D', new[]{'Ɗ','Ɖ','Ð'}},
                {'E', new[]{'É','È','Ê','Ë','Ē'}},
                {'F', new[]{'Ƒ','ℱ'}},
                {'G', new[]{'Ğ','Ĝ','Ġ','Ɠ'}},
                {'H', new[]{'Ĥ','Ħ','Ȟ'}},
                {'J', new[]{'Ĵ','ȷ'}},
                {'K', new[]{'Ķ','Ƙ'}},
                {'L', new[]{'Ĺ','Ļ','Ł'}},
                {'M', new[]{'Ɯ','ℳ'}},
                {'N', new[]{'Ń','Ň','Ñ','Ɲ'}},
                {'P', new[]{'Ƥ','ℙ'}},
                {'Q', new[]{'ℚ'}},
                {'R', new[]{'Ŕ','Ř','Ʀ'}},
                {'S', new[]{'Ś','Š','Ş','Ƨ'}},
                {'T', new[]{'Ť','Ŧ','Ƭ'}},
                {'W', new[]{'Ŵ','Ɯ'}},
                {'X', new[]{'Ẋ','Ẍ','Ʒ'}},
                {'Y', new[]{'Ÿ','Ý','Ŷ'}},
                {'Z', new[]{'Ź','Ž','Ƶ'}},
                {'2', new[]{'Ƨ','Ȝ'}},
                {'3', new[]{'Ʒ','Ȝ'}},
                {'4', new[]{'4','Ꮞ'}},
                {'5', new[]{'Ƽ'}},
                {'6', new[]{'Ƅ'}},
                {'7', new[]{'Ɓ'}},
                {'8', new[]{'B','ß'}},
                {'9', new[]{'Ɣ'}}
            };
            return map.ContainsKey(c) ? map[c].ToList() : new List<char> { c };
        }

        private CaptchaChallenge GenerateHardReverseChallenge(int level)
        {
            var progress = (level - 76) % 5 + 1;
            var len = Math.Min(5 + progress, 10);
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var text = new string(Enumerable.Range(0, len).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
            var reversed = new string(text.Reverse().ToArray());
            var svg = GenerateSvg(text, 30 + progress * 5, 30 + progress * 5);

            return new CaptchaChallenge
            {
                Type = 15,
                Level = level,
                Question = GetHardQuestion(level, "🔄", "图片中的文字（倒过来了！）"),
                ImageSvg = svg,
                CorrectAnswer = reversed.ToUpper(),
                Options = GenerateOptions(reversed, 4 + progress / 2),
                DisplayType = "image",
                TimeLimit = Math.Max(3, 7 - progress),
                Points = GetHardPoints(level) * 3,
                FunMessage = GetHardMessage(level)
            };
        }

        private CaptchaChallenge GenerateHardMissingLetterChallenge(int level)
        {
            var progress = (level - 81) % 5 + 1;
            var len = Math.Min(5 + progress, 9);
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
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
                Question = GetHardQuestion(level, "🔤", $"补全两个缺失字母：{new string(display)}"),
                CorrectAnswer = correct,
                Options = GenerateOptions(correct, 4 + progress / 2),
                DisplayType = "text",
                TimeLimit = Math.Max(3, 6 - progress),
                Points = GetHardPoints(level) * 3,
                FunMessage = GetHardMessage(level)
            };
        }

        private CaptchaChallenge GenerateHardQuickTapChallenge(int level)
        {
            var progress = (level - 86) % 5 + 1;
            var optionsCount = Math.Min(4 + progress * 2, 12);
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var target = chars[_random.Next(chars.Length)];

            var options = new List<string> { target.ToString() };
            while (options.Count < optionsCount)
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
                Question = GetHardQuestion(level, "⚡", $"从 {optionsCount} 个选项中快速找到目标！"),
                ImageSvg = svg,
                CorrectAnswer = target.ToString(),
                Options = options,
                DisplayType = "image",
                TimeLimit = Math.Max(2, 5 - progress / 2),
                Points = GetHardPoints(level) * 4,
                FunMessage = GetHardMessage(level)
            };
        }

        private CaptchaChallenge GenerateHardIdiomChallenge(int level)
        {
            var progress = (level - 91) % 5 + 1;
            var idx = Math.Min(progress - 1, _rareIdioms.Count - 1);
            var idiom = _rareIdioms[idx];

            var pos = _random.Next(idiom.Length);
            var correct = idiom[pos];
            var display = idiom.ToCharArray();
            display[pos] = '□';

            return new CaptchaChallenge
            {
                Type = 18,
                Level = level,
                Question = GetHardQuestion(level, "📖", $"补全生僻成语：{new string(display)}"),
                CorrectAnswer = correct.ToString(),
                Options = GenerateOptions(correct.ToString(), 4 + progress / 2),
                DisplayType = "text",
                TimeLimit = Math.Max(3, 6 - progress),
                Points = GetHardPoints(level) * 4,
                FunMessage = GetHardMessage(level)
            };
        }

        private CaptchaChallenge GenerateUltimateChallenge(int level)
        {
            var progress = (level - 96) % 5 + 1;
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
            challenge.TimeLimit = Math.Max(2, 4 - progress);
            challenge.Points = GetHardPoints(level) * 5;
            challenge.Question = GetHardQuestion(level, "💀", $"⚡ 终极BOSS挑战！{challenge.Question.Replace("⚡", "").Replace("💀", "")}");
            challenge.FunMessage = GetUltimateMessage(level);

            return challenge;
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
                sb.AppendLine($"<line x1=\"{_random.Next(-20, 340)}\" y1=\"{_random.Next(-20, 110)}\" x2=\"{_random.Next(-20, 340)}\" y2=\"{_random.Next(-20, 110)}\" stroke=\"rgb({r},{g},{b})\" stroke-width=\"{_random.Next(1, 3)}\" opacity=\"0.4\"/>");
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
                sb.AppendLine($"<text x=\"{x}\" y=\"50\" font-family=\"Arial, sans-serif\" font-size=\"{36 + distortion / 3}\" font-weight=\"bold\" fill=\"rgb({r},{g},{b})\" transform=\"rotate({angle} {x} 50)\" text-anchor=\"middle\" dominant-baseline=\"central\">{chars[i]}</text>");
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private string GenerateQuickSvg(string target)
        {
            return GenerateSvg(target, 10, 10);
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

        private string GetQuestion(int level, string icon, string text)
        {
            var tags = level <= 20 ? "😊" : level <= 40 ? "🤔" : level <= 60 ? "😤" : level <= 80 ? "💪" : "💀";
            return $"{tags} {icon} {text}";
        }

        private string GetHardQuestion(int level, string icon, string text)
        {
            var tags = level <= 55 ? "😤" : level <= 70 ? "💪" : level <= 85 ? "🔥" : "💀";
            return $"{tags} {icon} {text}";
        }

        private string GetMessage(int level)
        {
            var msgs = new[] { "✨ 轻松！", "🔥 继续！", "💪 加油！", "⭐ 太强了！", "🚀 冲！" };
            return msgs[_random.Next(msgs.Length)];
        }

        private string GetHardMessage(int level)
        {
            var msgs = new[]
            {
                "⚡ 速度！", "🔥 燃起来了！", "💪 坚持住！", "👑 你是人类之王！",
                "💀 AI已崩溃！", "🚀 超神！", "⭐ 完美！", "🎯 精准！"
            };
            return msgs[_random.Next(msgs.Length)];
        }

        private string GetUltimateMessage(int level)
        {
            var msgs = new[]
            {
                "🏆 你赢了！", "💀 连AI都怕你！", "👑 终极王者！",
                "🚀 人类之光！", "⚡ 太逆天了！", "💪 无敌！"
            };
            return msgs[_random.Next(msgs.Length)];
        }

        private int GetPoints(int level)
        {
            return 10 + level / 5;
        }

        private int GetHardPoints(int level)
        {
            return 20 + level / 2;
        }

        private int GetTimeLimit(int level)
        {
            return Math.Max(5, 20 - level / 5);
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
