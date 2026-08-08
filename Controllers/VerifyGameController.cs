using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MyPersonalWebsite.Controllers
{
    public class VerifyGameController : Controller
    {
        private readonly DataSyncService _dataSync;
        private readonly Random _random = new();
        private readonly string[] _fontFamilies;

        private static HashSet<int> _usedTypes = new HashSet<int>();

        // ============================================================
        // 颜色映射表（供前端和后台共用）
        // ============================================================
        private readonly Dictionary<string, string> _colorHex = new()
        {
            {"红色", "#FF0000"}, {"深红", "#8B0000"}, {"粉色", "#FF69B4"}, {"浅粉", "#FFB6C1"},
            {"蓝色", "#0000FF"}, {"深蓝", "#00008B"}, {"天蓝", "#87CEEB"}, {"青色", "#00CED1"},
            {"绿色", "#00AA00"}, {"深绿", "#006400"}, {"草绿", "#7CFC00"}, {"黄色", "#FFD700"},
            {"金色", "#DAA520"}, {"橙色", "#FF6600"}, {"深橙", "#CC5500"}, {"紫色", "#8800CC"},
            {"深紫", "#4B0082"}, {"棕色", "#8B4513"}, {"灰色", "#808080"}, {"黑色", "#000000"},
            {"白色", "#FFFFFF"}, {"银色", "#C0C0C0"}, {"紫罗兰", "#EE82EE"}, {"靛蓝", "#4B0082"},
            {"玫瑰红", "#FF007F"}, {"柠檬黄", "#FFF700"}, {"薄荷绿", "#98FF98"}, {"珊瑚橙", "#FF7F50"},
            {"象牙白", "#FFFFF0"}, {"巧克力棕", "#D2691E"}, {"琥珀金", "#FFBF00"}, {"翠绿", "#50C878"},
            {"宝石蓝", "#0F52BA"}, {"玛瑙红", "#C04000"}, {"珍珠白", "#F5F5F5"}, {"青蓝", "#00BFFF"},
            {"紫红", "#C71585"}, {"橙红", "#FF4500"}, {"黄绿", "#9ACD32"}, {"蓝紫", "#8A2BE2"},
            {"粉红", "#FFB6C1"}, {"米色", "#F5F5DC"}, {"卡其", "#F0E68C"}, {"珊瑚", "#FF7F50"},
            {"青绿", "#008B8B"}, {"靛青", "#4B0082"}, {"藏青", "#000080"}, {"酒红", "#800020"},
            {"橄榄绿", "#556B2F"}, {"石板灰", "#708090"}, {"杏色", "#FFDAB9"}, {"薰衣草", "#E6E6FA"},
            {"紫藤", "#C9A0DC"}, {"樱花粉", "#FFB7C5"}, {"奶油", "#FFFDD0"},
            {"浅绿", "#90EE90"}, {"浅蓝", "#ADD8E6"}, {"浅紫", "#D8BFD8"}, {"浅黄", "#FFFFE0"},
            {"浅橙", "#FFDAB9"}, {"浅灰", "#D3D3D3"}, {"深灰", "#A9A9A9"}, {"墨绿", "#006400"},
            {"海军蓝", "#000080"}, {"克莱因蓝", "#002FA7"}, {"蒂芙尼蓝", "#81D8D0"},
            {"马卡龙粉", "#FFB5C5"}, {"马卡龙蓝", "#A7D8DE"}, {"马卡龙黄", "#FDE8B6"},
            {"马卡龙紫", "#C9B1E0"}, {"马卡龙绿", "#B5D4C5"}, {"莫兰迪粉", "#DDB5B5"},
            {"莫兰迪蓝", "#A8BCCD"}, {"莫兰迪绿", "#B5C4B5"}, {"莫兰迪紫", "#C4B5D4"},
            {"荧光粉", "#FF1493"}, {"荧光绿", "#00FF00"}, {"荧光黄", "#CCFF00"}, {"荧光橙", "#FF6B00"},
            {"暗红", "#8B1A1A"}, {"暗蓝", "#1A2A6B"}, {"暗绿", "#1A4A2A"}, {"暗紫", "#4A1A6B"},
            {"桃色", "#FFDAB9"}, {"珊瑚粉", "#F08080"}, {"橙黄", "#FFA500"}, {"柠檬", "#FFF44F"},
            {"海蓝", "#2E8B57"}, {"天空", "#87CEEB"}, {"薰衣草紫", "#E6E6FA"},
        };

        private readonly string[] _colorNames;
        private readonly string[] _singleColorWords = { "红", "蓝", "绿", "黄", "紫", "橙", "粉", "青", "灰", "棕" };

        private readonly Dictionary<string, string> _colorDisplayName = new()
        {
            {"红色", "红色"}, {"深红", "深红"}, {"粉色", "粉色"}, {"浅粉", "浅粉"},
            {"蓝色", "蓝色"}, {"深蓝", "深蓝"}, {"天蓝", "天蓝"}, {"青色", "青色"},
            {"绿色", "绿色"}, {"深绿", "深绿"}, {"草绿", "草绿"}, {"黄色", "黄色"},
            {"金色", "金色"}, {"橙色", "橙色"}, {"深橙", "深橙"}, {"紫色", "紫色"},
            {"深紫", "深紫"}, {"棕色", "棕色"}, {"灰色", "灰色"}, {"黑色", "黑色"},
            {"白色", "白色"}, {"银色", "银色"}, {"紫罗兰", "紫罗兰"}, {"靛蓝", "靛蓝"},
            {"玫瑰红", "玫瑰红"}, {"柠檬黄", "柠檬黄"}, {"薄荷绿", "薄荷绿"}, {"珊瑚橙", "珊瑚橙"},
            {"象牙白", "象牙白"}, {"巧克力棕", "巧克力棕"}, {"琥珀金", "琥珀金"}, {"翠绿", "翠绿"},
            {"宝石蓝", "宝石蓝"}, {"玛瑙红", "玛瑙红"}, {"珍珠白", "珍珠白"}, {"青蓝", "青蓝"},
            {"紫红", "紫红"}, {"橙红", "橙红"}, {"黄绿", "黄绿"}, {"蓝紫", "蓝紫"},
            {"粉红", "粉红"}, {"米色", "米色"}, {"卡其", "卡其"}, {"珊瑚", "珊瑚"},
            {"青绿", "青绿"}, {"靛青", "靛青"}, {"藏青", "藏青"}, {"酒红", "酒红"},
            {"橄榄绿", "橄榄绿"}, {"石板灰", "石板灰"}, {"杏色", "杏色"}, {"薰衣草", "薰衣草"},
            {"紫藤", "紫藤"}, {"樱花粉", "樱花粉"}, {"奶油", "奶油"}, {"浅绿", "浅绿"},
            {"浅蓝", "浅蓝"}, {"浅紫", "浅紫"}, {"浅黄", "浅黄"}, {"浅橙", "浅橙"},
            {"浅灰", "浅灰"}, {"深灰", "深灰"}, {"墨绿", "墨绿"}, {"海军蓝", "海军蓝"},
            {"克莱因蓝", "克莱因蓝"}, {"蒂芙尼蓝", "蒂芙尼蓝"}, {"马卡龙粉", "马卡龙粉"},
            {"马卡龙蓝", "马卡龙蓝"}, {"马卡龙黄", "马卡龙黄"}, {"马卡龙紫", "马卡龙紫"},
            {"马卡龙绿", "马卡龙绿"}, {"莫兰迪粉", "莫兰迪粉"}, {"莫兰迪蓝", "莫兰迪蓝"},
            {"莫兰迪绿", "莫兰迪绿"}, {"莫兰迪紫", "莫兰迪紫"}, {"荧光粉", "荧光粉"},
            {"荧光绿", "荧光绿"}, {"荧光黄", "荧光黄"}, {"荧光橙", "荧光橙"},
            {"暗红", "暗红"}, {"暗蓝", "暗蓝"}, {"暗绿", "暗绿"}, {"暗紫", "暗紫"},
            {"桃色", "桃色"}, {"珊瑚粉", "珊瑚粉"}, {"橙黄", "橙黄"}, {"柠檬", "柠檬"},
            {"海蓝", "海蓝"}, {"天空", "天空"}, {"薰衣草紫", "薰衣草紫"},
        };

        // ============================================================
        // 真/假判断题库
        // ============================================================
        private readonly (string statement, bool isTrue, int minLevel, int maxLevel)[] _trueFalseQuestions = new (string, bool, int, int)[]
        {
            // ... (保持原样，由于篇幅省略，您原有的数据不变)
            ("地球是球体", true, 1, 20), ("太阳从东方升起", true, 1, 20),
            ("水在标准大气压下0度结冰", true, 1, 20), ("成年人体内有206块骨头", true, 1, 20),
            ("熊猫是中国的特有物种", true, 1, 20), ("北京是中国的首都", true, 1, 20),
            ("东京是日本的首都", true, 1, 20), ("金字塔位于埃及", true, 1, 20),
            ("长城在中国", true, 1, 20), ("珠穆朗玛峰是世界最高峰", true, 1, 20),
            ("亚马孙河在南美洲", true, 1, 20), ("撒哈拉沙漠在非洲", true, 1, 20),
            ("澳大利亚是一个大洲", true, 1, 20), ("人类有23对染色体", true, 1, 20),
            ("蜜蜂能生产蜂蜜", true, 1, 20), ("地球绕太阳公转", true, 1, 20),
            ("月亮是地球的卫星", true, 1, 20), ("太阳是恒星", true, 1, 20),
            ("一年有365天", true, 1, 20), ("一天有24小时", true, 1, 20),
            ("一周有7天", true, 1, 20), ("一年有12个月", true, 1, 20),
            ("一个小时有60分钟", true, 1, 20), ("一分钟有60秒", true, 1, 20),
            ("氧气是人类生存必需的", true, 1, 20), ("水是无色无味的", true, 1, 20),
            ("空气有重量", true, 1, 20), ("声音在真空中无法传播", true, 1, 20),
            ("光速是已知最快的速度", true, 1, 20), ("植物需要阳光才能生长", true, 1, 20),
            ("太阳从西方升起", false, 1, 20), ("月亮是恒星", false, 1, 20),
            ("蜘蛛是昆虫", false, 1, 20), ("巴黎是英国的首都", false, 1, 20),
            ("埃菲尔铁塔位于罗马", false, 1, 20), ("企鹅生活在北极", false, 1, 20),
            ("鲨鱼是哺乳动物", false, 1, 20), ("狗是卵生动物", false, 1, 20),
            ("鸟是哺乳动物", false, 1, 20), ("蛇是哺乳动物", false, 1, 20),
            ("青蛙是哺乳动物", false, 1, 20), ("蝙蝠是鸟类", false, 1, 20),
            ("海豚是鱼类", false, 1, 20), ("鲸鱼是鱼类", false, 1, 20),
            ("地球是平的", false, 1, 20), ("太阳绕着地球转", false, 1, 20),
            ("人类有3只眼睛", false, 1, 20), ("人有尾巴", false, 1, 20),
            ("植物会走路", false, 1, 20), ("石头会说话", false, 1, 20),
            ("水是黑色的", false, 1, 20), ("所有花都是红色的", false, 1, 20),
            ("飞鱼能在空中滑翔", true, 1, 20),

            // 困难 (21-40) - 由于篇幅，保留原有数据的关键部分
            ("鲸鱼是哺乳动物", true, 21, 40), ("光速是宇宙中已知的最快速度", true, 21, 40),
            ("2是最小的质数", true, 21, 40), ("1不是质数", true, 21, 40),
            ("0不是正整数", true, 21, 40), ("光年是距离单位", true, 21, 40),
            // ... 其余数据保持原样，这里省略以节省篇幅
        };

        // ============================================================
        // 找规律题库
        // ============================================================
        private readonly (string pattern, int answer, int minLevel, int maxLevel)[] _patternQuestions = new (string, int, int, int)[]
        {
            ("2, 4, 6, ?, 10", 8, 1, 20), ("1, 3, 5, ?, 9", 7, 1, 20),
            ("10, 20, 30, ?, 50", 40, 1, 20), ("5, 10, 15, ?, 25", 20, 1, 20),
            ("1, 2, 4, ?, 11", 7, 1, 20), ("3, 6, 9, ?, 15", 12, 1, 20),
            ("2, 4, 8, ?, 32", 16, 1, 20), ("1, 4, 9, ?, 25", 16, 1, 20),
            ("2, 6, 12, ?, 30", 20, 1, 20), ("3, 7, 11, ?, 19", 15, 1, 20),
            ("1, 2, 3, 5, ?, 13", 8, 1, 20), ("2, 3, 5, 8, ?, 21", 13, 1, 20),
            ("1, 3, 7, 15, ?, 63", 31, 1, 20), ("1, 2, 6, 24, ?, 720", 120, 1, 20),
            ("2, 6, 18, 54, ?", 162, 1, 20), ("3, 9, 27, 81, ?", 243, 1, 20),
            ("1, 3, 5, 7, ?, 11", 9, 1, 20), ("2, 4, 6, 8, ?, 12", 10, 1, 20),
            ("5, 10, 20, 40, ?", 80, 1, 20), ("1, 2, 3, 4, ?, 6", 5, 1, 20),
            ("2, 5, 8, 11, ?, 17", 14, 1, 20), ("3, 6, 12, 24, ?", 48, 1, 20),
            ("4, 8, 12, 16, ?", 20, 1, 20), ("5, 15, 45, 135, ?", 405, 1, 20),
            ("1, 2, 4, 8, 16, ?", 32, 1, 20), ("2, 4, 6, 10, 16, ?", 26, 1, 20),
            ("1, 3, 6, 10, 15, ?", 21, 1, 20), ("1, 4, 10, 20, 35, ?", 56, 1, 20),
            ("2, 5, 10, 17, 26, ?", 37, 1, 20), ("1, 2, 6, 15, 31, ?", 56, 1, 20),
            // 困难 (21-40)
            ("2, 5, 10, 17, ?, 37", 26, 21, 40), ("1, 3, 6, 10, ?, 21", 15, 21, 40),
            ("2, 6, 12, 20, ?, 42", 30, 21, 40), ("1, 2, 5, 10, ?, 26", 17, 21, 40),
            ("3, 8, 15, 24, ?, 48", 35, 21, 40), ("2, 3, 5, 9, ?, 33", 17, 21, 40),
            ("1, 4, 13, 40, ?, 364", 121, 21, 40), ("2, 5, 11, 23, ?, 95", 47, 21, 40),
            ("1, 3, 8, 19, ?, 81", 42, 21, 40), ("4, 9, 16, 25, ?, 49", 36, 21, 40),
            ("1, 8, 27, 64, ?, 216", 125, 21, 40), ("2, 8, 18, 32, ?, 72", 50, 21, 40),
            ("1, 5, 9, 13, ?, 21", 17, 21, 40), ("3, 7, 11, 15, ?, 23", 19, 21, 40),
            ("2, 7, 12, 17, ?, 27", 22, 21, 40), ("4, 11, 18, 25, ?, 39", 32, 21, 40),
            // ... 其余数据保持原样
        };

        // ============================================================
        // 构造方法
        // ============================================================
        public VerifyGameController(DataSyncService dataSync)
        {
            _dataSync = dataSync;
            _fontFamilies = new[] { "Arial", "Times New Roman", "Georgia", "Verdana", "Impact", "Comic Sans MS", "Courier New", "Trebuchet MS" };
            _colorNames = _colorHex.Keys.ToArray();
        }

        // ============================================================
        // 入口方法
        // ============================================================
        [HttpGet]
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            return View();
        }

        // ============================================================
        // 保存分数
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> SaveScore(int score, int level, int maxCombo, int passed)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { success = false, message = "请先登录" });

            try
            {
                var stats = await _dataSync.GetUserGameStatsAsync(userId.Value);
                if (stats == null)
                {
                    stats = new UserGameStats
                    {
                        UserId = userId.Value,
                        TotalPoints = score,
                        MaxCombo = maxCombo,
                        MaxLevel = level,
                        GamesPlayed = 1,
                        UpdatedAt = DateTime.Now
                    };
                    await _dataSync.AddUserGameStatsAsync(stats);
                }
                else
                {
                    if (score > stats.TotalPoints) stats.TotalPoints = score;
                    if (maxCombo > stats.MaxCombo) stats.MaxCombo = maxCombo;
                    if (level > stats.MaxLevel) stats.MaxLevel = level;
                    stats.GamesPlayed += 1;
                    stats.UpdatedAt = DateTime.Now;
                    await _dataSync.UpdateUserGameStatsAsync(stats);
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存分数失败: {ex.Message}");
                return Json(new { success = false, message = "保存失败" });
            }
        }

        // ============================================================
        // 获取排行榜
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetRanking()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var allStats = await _dataSync.GetAllUserGameStatsAsync();
                var users = await _dataSync.GetAllUsersAsync();

                var ranking = allStats
                    .Where(s => s.MaxLevel > 0)
                    .OrderByDescending(s => s.TotalPoints)
                    .ThenByDescending(s => s.MaxLevel)
                    .Take(100)
                    .Select((s, index) =>
                    {
                        var user = users.FirstOrDefault(u => u.Id == s.UserId);
                        return new
                        {
                            userId = s.UserId,
                            username = user?.Username ?? "已删除用户",
                            avatarUrl = user?.AvatarUrl,
                            isAvatarApproved = user?.IsAvatarApproved ?? false,
                            totalPoints = s.TotalPoints,
                            maxCombo = s.MaxCombo,
                            maxLevel = s.MaxLevel,
                            gamesPlayed = s.GamesPlayed,
                            rank = index + 1,
                            isMe = s.UserId == userId
                        };
                    })
                    .ToList();

                return Json(new { success = true, data = ranking });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取排行榜失败: {ex.Message}");
                return Json(new { success = false, data = new List<object>() });
            }
        }

        // ============================================================
        // ⭐ 获取挑战 - 核心方法
        // ============================================================
        [HttpGet]
        public IActionResult GetChallenge(int level)
        {
            try
            {
                var challenge = GenerateChallenge(level);
                return Json(new { success = true, data = challenge });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // ⭐ 随机类型生成器
        // ============================================================
        private object GenerateChallenge(int level)
        {
            int difficulty = level;

            if (_usedTypes.Count >= 20)
            {
                _usedTypes.Clear();
            }

            var availableTypes = Enumerable.Range(0, 20).Where(i => !_usedTypes.Contains(i)).ToList();
            if (availableTypes.Count == 0)
            {
                _usedTypes.Clear();
                availableTypes = Enumerable.Range(0, 20).ToList();
            }

            int typeIndex = availableTypes[_random.Next(availableTypes.Count)];
            _usedTypes.Add(typeIndex);

            int typesCompleted = _usedTypes.Count;

            string[] typeNames = new string[]
            {
                "文字识别",      // 0
                "算术计算",      // 1
                "汉字笔画",      // 2
                "颜色识别",      // 3
                "找不同",        // 4
                "倒序识别",      // 5
                "空缺字母",      // 6
                "时间计算",      // 7
                "成语填空",      // 8
                "数字记忆",      // 9
                "找规律",        // 10
                "颜色混合",      // 11
                "真假判断",      // 12
                "数字华容道",    // 13
                "立体三视图",    // 14
                "图形计数",      // 15
                "日期推理",      // 16
                "图形旋转",      // 17
                "1A2B猜数字",    // 18
                "颜色三重干扰"   // 19
            };

            object result;
            switch (typeIndex)
            {
                case 0: result = GenerateTextRecognition(level, difficulty, typesCompleted); break;
                case 1: result = GenerateArithmetic(level, difficulty, typesCompleted); break;
                case 2: result = GenerateStrokeCount(level, difficulty, typesCompleted); break;
                case 3: result = GenerateColorRecognition(level, difficulty, typesCompleted); break;
                case 4: result = GenerateFindDifferent(level, difficulty, typesCompleted); break;
                case 5: result = GenerateReverseText(level, difficulty, typesCompleted); break;
                case 6: result = GenerateMissingLetter(level, difficulty, typesCompleted); break;
                case 7: result = GenerateTimeCalculation(level, difficulty, typesCompleted); break;
                case 8: result = GenerateIdiomFill(level, difficulty, typesCompleted); break;
                case 9: result = GenerateMemoryChallenge(level, difficulty, typesCompleted); break;
                case 10: result = GeneratePatternRecognition(level, difficulty, typesCompleted); break;
                case 11: result = GenerateColorMix(level, difficulty, typesCompleted); break;
                case 12: result = GenerateTrueFalse(level, difficulty, typesCompleted); break;
                case 13: result = GeneratePuzzle(level, difficulty, typesCompleted); break;
                case 14: result = GenerateThreeViewCounting(level, difficulty, typesCompleted); break;
                case 15: result = GenerateShapeCount(level, difficulty, typesCompleted); break;
                case 16: result = GenerateDateReasoning(level, difficulty, typesCompleted); break;
                case 17: result = GenerateRotateShape(level, difficulty, typesCompleted); break;
                case 18: result = Generate1A2B(level, difficulty, typesCompleted); break;
                default: result = GenerateTripleColorInterference(level, difficulty, typesCompleted); break;
            }

            var dict = result as Dictionary<string, object>;
            if (dict == null)
            {
                var props = result.GetType().GetProperties();
                dict = new Dictionary<string, object>();
                foreach (var prop in props)
                {
                    dict[prop.Name] = prop.GetValue(result);
                }
            }
            dict["typeName"] = typeNames[typeIndex];

            return dict;
        }

        // ============================================================
        // 工具方法
        // ============================================================

        private string GetDifficultyLabel(int difficulty)
        {
            if (difficulty >= 81) return "🔱 传说";
            if (difficulty >= 61) return "💀 地狱";
            if (difficulty >= 41) return "🔥 噩梦";
            if (difficulty >= 21) return "⚡ 困难";
            return "📝 入门";
        }

        private string GetFunMessage(string type)
        {
            var funMessages = new Dictionary<string, string[]>
            {
                {"text", new[]{"👁️ 神级视力！", "🔍 显微镜级！", "🎯 精准狙击！"}},
                {"arithmetic", new[]{"🧮 人形计算器！", "💡 爱因斯坦！", "🤓 数学之神！"}},
                {"stroke", new[]{"📝 文字学家！", "✍️ 汉字活字典！", "🏯 甲骨文专家！"}},
                {"color", new[]{"🎨 色彩之神！", "🌈 火眼金睛！", "✨ 审美大师！"}},
                {"findDifferent", new[]{"🔍 人形扫描仪！", "🎯 鹰眼！", "👀 像素级观察！"}},
                {"reverse", new[]{"🔄 人形反转器！", "🧠 超脑！", "💪 空间掌控者！"}},
                {"missingLetter", new[]{"🔤 人形词典！", "📚 词汇之王！", "✍️ 拼写之神！"}},
                {"timeCalc", new[]{"⏰ 时间大师！", "🕐 精准计算！", "⌚ 人形时钟！"}},
                {"idiom", new[]{"📖 成语活字典！", "🏯 国学大师！", "✍️ 文学宗师！"}},
                {"memory", new[]{"🧠 照相机记忆！", "💪 超脑！", "✨ 过目不忘！"}},
                {"pattern", new[]{"📐 规律大师！", "🧠 模式识别！", "🎯 数学之眼！"}},
                {"colorMix", new[]{"🎨 色彩炼金术！", "🌈 颜色魔法师！", "✨ 视觉艺术！"}},
                {"trueFalse", new[]{"⚖️ 真相之神！", "🧐 明察秋毫！", "🎯 一语中的！"}},
                {"puzzle", new[]{"🧩 拼图大师！", "🎯 空间掌控者！", "✨ 华容道之王！"}},
                {"threeView", new[]{"📐 三视图大师！", "🧠 立体思维！", "🎯 空间感知！"}},
                {"shapeCount", new[]{"📐 立体视觉！", "🧠 空间感知！", "✨ 三维大师！"}},
                {"dateReasoning", new[]{"📅 日历大师！", "⏰ 时间掌控者！", "🧠 日期天才！"}},
                {"rotate", new[]{"🔄 空间之神！", "🧠 超脑！", "✨ 旋转之王！"}},
                {"abgame", new[]{"🧠 推理大师！", "🔢 数字天才！", "🎯 精准打击！", "💡 逻辑王者！"}},
                {"tripleColor", new[]{"🎯 三重干扰通关！", "🌈 视觉之神！", "✨ 不是人类！"}},
            };
            if (funMessages.ContainsKey(type))
            {
                var msgs = funMessages[type];
                return msgs[_random.Next(msgs.Length)];
            }
            return "🎉 太棒了！";
        }

        private string GenerateSvgForText(string text, int distortion, int lineCount, int difficulty)
        {
            // ... 保持原样，篇幅限制省略
            int width = 360;
            int height = 120;
            int charCount = text.Length;

            var svg = new StringBuilder();
            svg.AppendLine($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>");

            int bgR = _random.Next(180, 250);
            int bgG = _random.Next(180, 250);
            int bgB = _random.Next(180, 250);

            svg.AppendLine($"<defs>");
            svg.AppendLine($"<filter id='distort' x='-20%' y='-20%' width='140%' height='140%'>");
            svg.AppendLine($"<feTurbulence type='fractalNoise' baseFrequency='{0.02 + difficulty / 600f:F2}' numOctaves='3' result='noise'/>");
            svg.AppendLine($"<feDisplacementMap in='SourceGraphic' in2='noise' scale='{5 + difficulty / 4}' xChannelSelector='R' yChannelSelector='G'/>");
            svg.AppendLine($"</filter>");

            if (difficulty > 40)
            {
                float blur = 0.2f + difficulty / 60f;
                svg.AppendLine($"<filter id='blur'><feGaussianBlur stdDeviation='{blur:F1}'/></filter>");
            }
            svg.AppendLine($"</defs>");

            svg.AppendLine($"<rect width='{width}' height='{height}' rx='10' fill='rgb({bgR},{bgG},{bgB})'/>");

            for (int i = 0; i < lineCount; i++)
            {
                int r = _random.Next(80, 230);
                int g = _random.Next(80, 230);
                int b = _random.Next(80, 230);
                svg.AppendLine($"<line x1='{_random.Next(-50,width+50)}' y1='{_random.Next(-50,height+50)}' x2='{_random.Next(-50,width+50)}' y2='{_random.Next(-50,height+50)}' stroke='rgb({r},{g},{b})' stroke-width='{_random.Next(1,4)}' opacity='{0.1 + _random.NextDouble() * 0.5:F2}'/>");
            }

            for (int i = 0; i < lineCount / 2; i++)
            {
                int r = _random.Next(80, 220);
                int g = _random.Next(80, 220);
                int b = _random.Next(80, 220);
                svg.AppendLine($"<path d='M{_random.Next(0,width)} {_random.Next(0,height)} Q{_random.Next(0,width)} {_random.Next(0,height)} {_random.Next(0,width)} {_random.Next(0,height)}' stroke='rgb({r},{g},{b})' stroke-width='{_random.Next(1,3)}' fill='none' opacity='{0.1 + _random.NextDouble() * 0.3:F2}'/>");
            }

            string allChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()_+-=<>?/";
            for (int i = 0; i < 20 + difficulty * 2; i++)
            {
                char fc = allChars[_random.Next(allChars.Length)];
                svg.AppendLine($"<text x='{_random.Next(0,width)}' y='{_random.Next(0,height)}' font-family='Arial' font-size='{_random.Next(10,25)}' fill='rgb({_random.Next(150,220)},{_random.Next(150,220)},{_random.Next(150,220)})' opacity='{0.03 + _random.NextDouble() * 0.1:F2}' text-anchor='middle' dominant-baseline='central'>{fc}</text>");
            }

            for (int i = 0; i < 200 + difficulty * 10; i++)
            {
                svg.AppendLine($"<circle cx='{_random.Next(0,width)}' cy='{_random.Next(0,height)}' r='{_random.Next(1,4)}' fill='rgb({_random.Next(100,220)},{_random.Next(100,220)},{_random.Next(100,220)})' opacity='{0.1 + _random.NextDouble() * 0.3:F2}'/>");
            }

            int spacing = (width - 60) / charCount;
            int startX = 30;

            for (int i = 0; i < charCount; i++)
            {
                char ch = text[i];
                int angle = _random.Next(-45 - difficulty / 4, 45 + difficulty / 4);
                int fontSize = _random.Next(32, 50);
                int x = startX + i * spacing + _random.Next(-12, 12);
                int y = height / 2 + 18 + _random.Next(-20, 20);

                int colorOffset = 15 + difficulty / 3;
                int r = Math.Min(255, Math.Max(50, bgR - _random.Next(-colorOffset, colorOffset)));
                int g = Math.Min(255, Math.Max(50, bgG - _random.Next(-colorOffset, colorOffset)));
                int b = Math.Min(255, Math.Max(50, bgB - _random.Next(-colorOffset, colorOffset)));

                float scaleX = 0.6f + (float)_random.NextDouble() * 1.0f;
                float scaleY = 0.6f + (float)_random.NextDouble() * 1.0f;
                int skewX = _random.Next(-25, 25);

                string font = _fontFamilies[_random.Next(_fontFamilies.Length)];
                string filter = difficulty > 40 ? "filter='url(#blur)'" : "";

                svg.AppendLine($"<text x='{x}' y='{y}' font-family='{font}' font-size='{fontSize}' font-weight='{_random.Next(400,900)}' fill='rgb({r},{g},{b})' transform='rotate({angle} {x} {y}) scale({scaleX:F2},{scaleY:F2}) skewX({skewX})' text-anchor='middle' dominant-baseline='central' {filter} opacity='{0.7 + _random.NextDouble() * 0.3:F2}'>{ch}</text>");
            }

            svg.AppendLine("</svg>");
            return svg.ToString();
        }

        private List<string> GenerateOptions(string correct, int count)
        {
            var options = new List<string> { correct };
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()_+-=<>?/";

            while (options.Count < count)
            {
                string fake = "";
                for (int i = 0; i < correct.Length; i++)
                {
                    fake += chars[_random.Next(chars.Length)];
                }
                if (!options.Contains(fake) && fake != correct)
                {
                    options.Add(fake);
                }
            }
            return options.OrderBy(_ => _random.Next()).ToList();
        }

        private List<string> GenerateNumberOptions(int correct, int count, int range)
        {
            var options = new List<string> { correct.ToString() };
            while (options.Count < count)
            {
                int fake = correct + _random.Next(-range, range + 1);
                if (fake < 0) fake = _random.Next(1, 30);
                string str = fake.ToString();
                if (!options.Contains(str) && fake != correct)
                {
                    options.Add(str);
                }
            }
            return options.OrderBy(_ => _random.Next()).ToList();
        }

        // ============================================================
        // 颜色混合辅助方法
        // ============================================================
        private string MixColorsRGB(string color1Name, string color2Name)
        {
            if (!_colorHex.ContainsKey(color1Name) || !_colorHex.ContainsKey(color2Name))
                return "灰色";

            try
            {
                var c1 = System.Drawing.ColorTranslator.FromHtml(_colorHex[color1Name]);
                var c2 = System.Drawing.ColorTranslator.FromHtml(_colorHex[color2Name]);

                int r = (c1.R + c2.R) / 2;
                int g = (c1.G + c2.G) / 2;
                int b = (c1.B + c2.B) / 2;

                if (r == c1.R && g == c1.G && b == c1.B)
                    return color1Name;
                if (r == c2.R && g == c2.G && b == c2.B)
                    return color2Name;

                return FindClosestColorName(r, g, b);
            }
            catch
            {
                return "灰色";
            }
        }

        private string FindClosestColorName(int r, int g, int b)
        {
            string closestName = "灰色";
            double closestDistance = double.MaxValue;

            foreach (var kv in _colorHex)
            {
                try
                {
                    var target = System.Drawing.ColorTranslator.FromHtml(kv.Value);
                    double distance = Math.Sqrt(
                        Math.Pow(r - target.R, 2) +
                        Math.Pow(g - target.G, 2) +
                        Math.Pow(b - target.B, 2)
                    );
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestName = kv.Key;
                    }
                }
                catch { }
            }

            return closestName;
        }

        // ⭐ 生成颜色混合显示HTML（不显示混合结果，只显示问号）
        private string GenerateColorMixDisplay(string[] colorNames, string? resultName, int size = 50)
        {
            var sb = new StringBuilder();
            sb.Append("<div style='display:flex;align-items:center;gap:12px;justify-content:center;padding:12px 0;flex-wrap:wrap;'>");

            foreach (var color in colorNames)
            {
                if (_colorHex.ContainsKey(color))
                {
                    var hex = _colorHex[color];
                    sb.Append($"<div style='display:flex;flex-direction:column;align-items:center;gap:4px;'>");
                    sb.Append($"<div style='width:{size}px;height:{size}px;border-radius:8px;background:{hex};border:1px solid rgba(255,255,255,0.06);box-shadow:0 4px 12px rgba(0,0,0,0.1);'></div>");
                    sb.Append($"<span style='color:rgba(255,255,255,0.08);font-size:0.5rem;'>{color}</span>");
                    sb.Append($"</div>");
                }
                if (color != colorNames.Last())
                {
                    sb.Append("<span style='color:rgba(255,255,255,0.08);font-size:1.2rem;'>+</span>");
                }
            }

            sb.Append("<span style='color:rgba(255,255,255,0.06);font-size:1.2rem;'>→</span>");

            // 显示问号
            sb.Append("<div style='display:flex;flex-direction:column;align-items:center;gap:4px;'>");
            sb.Append($"<div style='width:{size + 10}px;height:{size + 10}px;border-radius:8px;background:rgba(255,255,255,0.02);border:2px dashed rgba(255,255,255,0.06);display:flex;align-items:center;justify-content:center;font-size:1.5rem;color:rgba(255,255,255,0.08);'>?</div>");
            sb.Append($"<span style='color:rgba(255,255,255,0.04);font-size:0.4rem;'>混合结果</span>");
            sb.Append("</div>");

            sb.Append("</div>");
            return sb.ToString();
        }

        // ⭐ 生成颜色选项HTML（色块+名称）
        private string GenerateColorOptionsHtml(List<string> colorNames, int size = 56)
        {
            var sb = new StringBuilder();
            sb.Append("<div style='display:grid;grid-template-columns:repeat(3,1fr);gap:8px;max-width:360px;margin:0 auto;'>");

            foreach (var color in colorNames)
            {
                var hex = _colorHex.ContainsKey(color) ? _colorHex[color] : "#808080";
                var displayName = _colorDisplayName.ContainsKey(color) ? _colorDisplayName[color] : color;
                sb.Append($"<div class='color-option' data-color='{color}' style='display:flex;flex-direction:column;align-items:center;gap:6px;padding:10px;border:2px solid rgba(255,255,255,0.04);border-radius:14px;background:rgba(255,255,255,0.02);cursor:pointer;transition:all 0.3s ease;'>");
                sb.Append($"<div style='width:{size}px;height:{size}px;border-radius:10px;background:{hex};border:1px solid rgba(255,255,255,0.06);'></div>");
                sb.Append($"<span style='color:rgba(255,255,255,0.5);font-size:0.9rem;font-weight:600;'>{displayName}</span>");
                sb.Append("</div>");
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        // ============================================================
        // 题型 0：文字识别
        // ============================================================
        private object GenerateTextRecognition(int level, int difficulty, int typesCompleted)
        {
            int length = Math.Min(4 + difficulty / 8, 16);
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()_+-=<>?/";
            string text = "";
            for (int i = 0; i < length; i++) text += chars[_random.Next(chars.Length)];

            int lineCount = 30 + difficulty * 3;
            int distortion = 5 + difficulty / 2;
            var svg = GenerateSvgForText(text, distortion, lineCount, difficulty);

            var options = GenerateOptions(text, 4 + Math.Min(difficulty / 10, 4));
            int timeLimit = Math.Max(3, 18 - difficulty / 6);

            return new Dictionary<string, object>
            {
                ["type"] = "text",
                ["level"] = level,
                ["question"] = $"👁️ 识别下方图片中的文字（{length}位）",
                ["imageSvg"] = svg,
                ["correctAnswer"] = text,
                ["options"] = options,
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("text")
            };
        }

        // ============================================================
        // 题型 1：算术计算
        // ============================================================
        private object GenerateArithmetic(int level, int difficulty, int typesCompleted)
        {
            int maxNum = 10 + difficulty * 5;
            int a = _random.Next(5, maxNum);
            int b = _random.Next(2, Math.Max(3, maxNum / 3));

            char[] ops = difficulty > 60 ? new[] { '+', '-', '×', '÷', '^' } :
                         difficulty > 30 ? new[] { '+', '-', '×', '÷' } : new[] { '+', '-', '×' };

            char op = ops[_random.Next(ops.Length)];
            int result;

            switch (op)
            {
                case '+': result = a + b; break;
                case '-': result = a - b; break;
                case '×': result = a * b; break;
                case '÷': result = a / b; break;
                case '^': result = (int)Math.Pow(a % 10, Math.Min(b % 3 + 1, 3)); break;
                default: result = a + b; break;
            }

            if (result < 0) result = Math.Abs(result);
            if (result == 0) result = a + b;
            if (result > 99999) result = a + b;

            var options = GenerateNumberOptions(result, 4 + Math.Min(difficulty / 10, 3), Math.Max(5, 15 + result / 10));
            int timeLimit = Math.Max(3, 14 - difficulty / 6);

            return new Dictionary<string, object>
            {
                ["type"] = "arithmetic",
                ["level"] = level,
                ["question"] = $"🧮 {a} {op} {b} = ?",
                ["correctAnswer"] = result.ToString(),
                ["options"] = options,
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("arithmetic")
            };
        }

        // ============================================================
        // ⭐ 题型 11：颜色混合（重点修复）
        // ============================================================
        private object GenerateColorMix(int level, int difficulty, int typesCompleted)
        {
            int colorCount, optionCount;

            if (difficulty <= 20) { colorCount = 2; optionCount = 4; }
            else if (difficulty <= 40) { colorCount = 3; optionCount = 5; }
            else if (difficulty <= 60) { colorCount = 4; optionCount = 6; }
            else if (difficulty <= 80) { colorCount = 5; optionCount = 6; }
            else { colorCount = 6; optionCount = 6; }

            var availableColors = _colorNames.ToList();
            var selectedColors = new List<string>();
            var usedIndices = new HashSet<int>();

            for (int i = 0; i < colorCount; i++)
            {
                int idx;
                int attempts = 0;
                do
                {
                    idx = _random.Next(availableColors.Count);
                    attempts++;
                } while (usedIndices.Contains(idx) && attempts < 50);

                usedIndices.Add(idx);
                selectedColors.Add(availableColors[idx]);
            }

            string resultColor = selectedColors[0];
            for (int i = 1; i < selectedColors.Count; i++)
            {
                resultColor = MixColorsRGB(resultColor, selectedColors[i]);
            }

            var allColors = _colorNames.ToList();
            var options = new List<string> { resultColor };

            // 生成干扰项（与结果颜色相近的）
            var similarPool = allColors
                .Where(c => c != resultColor && !selectedColors.Contains(c))
                .ToList();

            if (_colorHex.ContainsKey(resultColor))
            {
                var targetHex = _colorHex[resultColor];
                var targetColor = System.Drawing.ColorTranslator.FromHtml(targetHex);

                similarPool = similarPool
                    .Where(c => _colorHex.ContainsKey(c))
                    .OrderBy(c =>
                    {
                        try
                        {
                            var cHex = _colorHex[c];
                            var cColor = System.Drawing.ColorTranslator.FromHtml(cHex);
                            return Math.Sqrt(
                                Math.Pow(cColor.R - targetColor.R, 2) +
                                Math.Pow(cColor.G - targetColor.G, 2) +
                                Math.Pow(cColor.B - targetColor.B, 2)
                            );
                        }
                        catch { return 9999; }
                    })
                    .ToList();

                int takeCount = Math.Min(similarPool.Count, optionCount - 1);
                var selectedOptions = similarPool.Take(takeCount).ToList();

                if (selectedOptions.Count < optionCount - 1)
                {
                    var extra = similarPool.Skip(takeCount).Take(optionCount - 1 - selectedOptions.Count).ToList();
                    selectedOptions.AddRange(extra);
                }

                selectedOptions = selectedOptions.OrderBy(_ => _random.Next()).Take(optionCount - 1).ToList();

                while (selectedOptions.Count < optionCount - 1 && allColors.Count > 0)
                {
                    var extra = allColors.Where(c => c != resultColor && !selectedOptions.Contains(c) && !selectedColors.Contains(c))
                        .OrderBy(_ => _random.Next())
                        .FirstOrDefault();
                    if (extra != null) selectedOptions.Add(extra);
                    else break;
                }

                options.AddRange(selectedOptions);
            }

            options = options.OrderBy(_ => _random.Next()).ToList();

            // ⭐ 生成显示HTML（色块展示混合过程）
            var displayHtml = GenerateColorMixDisplay(selectedColors.ToArray(), null);

            // ⭐ 生成选项HTML（色块+名称）
            var optionsHtml = GenerateColorOptionsHtml(options);

            int timeLimit = Math.Max(8, 20 - difficulty / 10);

            return new Dictionary<string, object>
            {
                ["type"] = "colorMix",
                ["level"] = level,
                ["question"] = $"🎨 以下颜色混合后是什么颜色？",
                ["displayHtml"] = displayHtml,
                ["optionsHtml"] = optionsHtml,  // ⭐ 关键：前端直接渲染
                ["options"] = options,
                ["correctAnswer"] = resultColor,
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("colorMix")
            };
        }

        // ============================================================
        // 以下题型保持原有实现，篇幅限制省略详细代码
        // 实际项目中请保留完整的其他题型实现
        // ============================================================

        // 题型 2：汉字笔画
        private object GenerateStrokeCount(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "stroke",
                ["level"] = level,
                ["question"] = "📝 汉字「人」有几画？",
                ["correctAnswer"] = "2",
                ["options"] = new List<string>{"1","2","3","4"},
                ["timeLimit"] = 10,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("stroke")
            };
        }

        // 题型 3：颜色识别
        private object GenerateColorRecognition(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "color",
                ["level"] = level,
                ["question"] = $"🎨 下面文字是什么颜色？",
                ["displayHtml"] = "<span style='color:#FF0000;font-size:3rem;font-weight:bold;'>红</span>",
                ["correctAnswer"] = "红色",
                ["options"] = new List<string>{"红色","蓝色","绿色","黄色"},
                ["timeLimit"] = 8,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("color")
            };
        }

        // 题型 4：找不同
        private object GenerateFindDifferent(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "findDifferent",
                ["level"] = level,
                ["question"] = $"🔍 记住下面的字符，然后找出被更改的那个！",
                ["originalDisplay"] = "ABCDE",
                ["displayTime"] = 3,
                ["shuffledDisplay"] = "ABXDE",
                ["shuffledPos"] = 2,
                ["correctAnswer"] = "C",
                ["options"] = new List<string>{"A","B","C","D","E"},
                ["timeLimit"] = 10,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("findDifferent")
            };
        }

        // 题型 5：倒序识别
        private object GenerateReverseText(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "reverse",
                ["level"] = level,
                ["question"] = $"🔄 图片中的文字是什么？（倒过来了）",
                ["imageSvg"] = GenerateSvgForText("HELLO", 5, 30, difficulty),
                ["correctAnswer"] = "OLLEH",
                ["options"] = new List<string>{"HELLO","OLLEH","HLELO","OLELH"},
                ["timeLimit"] = 10,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("reverse")
            };
        }

        // 题型 6：空缺字母
        private object GenerateMissingLetter(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "missingLetter",
                ["level"] = level,
                ["question"] = $"🔤 补全英文单词：<br><span style='font-size:1.8rem;font-weight:bold;letter-spacing:6px;font-family:monospace;color:#8B5CF6;'>A P P _ E</span>",
                ["correctAnswer"] = "L",
                ["options"] = new List<string>{"L","M","N","O"},
                ["timeLimit"] = 10,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("missingLetter")
            };
        }

        // 题型 7：时间计算
        private object GenerateTimeCalculation(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "timeCalc",
                ["level"] = level,
                ["question"] = $"⏰ 从 08:00 到 12:30 经过了多少时间？",
                ["correctAnswer"] = "4小时30分钟",
                ["options"] = new List<string>{"4小时30分钟","4小时","5小时","3小时30分钟"},
                ["timeLimit"] = 10,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("timeCalc")
            };
        }

        // 题型 8：成语填空
        private object GenerateIdiomFill(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "idiom",
                ["level"] = level,
                ["question"] = $"📖 补全成语（缺1个字）：<span style='font-size:1.8rem;font-weight:bold;'>一马当_</span>",
                ["correctAnswer"] = "先",
                ["options"] = new List<string>{"先","前","后","中"},
                ["timeLimit"] = 10,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("idiom")
            };
        }

        // 题型 9：数字记忆
        private object GenerateMemoryChallenge(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "memory",
                ["level"] = level,
                ["question"] = $"🧠 记住下面的数字",
                ["displayNumber"] = "3847",
                ["memoryTime"] = 3,
                ["correctAnswer"] = "3847",
                ["keyboardRows"] = new List<List<string>>{
                    new List<string>{"1","2","3"},
                    new List<string>{"4","5","6"},
                    new List<string>{"7","8","9"},
                    new List<string>{"","0","⌫"}
                },
                ["timeLimit"] = 15,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("memory")
            };
        }

        // 题型 10：找规律
        private object GeneratePatternRecognition(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "pattern",
                ["level"] = level,
                ["question"] = $"📐 找规律填空：<br><span style='font-size:1.8rem;font-weight:bold;color:#fff;'>2, 4, 6, ?, 10</span>",
                ["correctAnswer"] = "8",
                ["options"] = new List<string>{"6","7","8","9"},
                ["timeLimit"] = 15,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("pattern")
            };
        }

        // 题型 12：真假判断
        private object GenerateTrueFalse(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "trueFalse",
                ["level"] = level,
                ["question"] = $"⚖️ 判断以下陈述是否正确：<br><span style='font-size:1.3rem;color:#fff;font-weight:500;'>地球是平的</span>",
                ["correctAnswer"] = "❌ 假的",
                ["options"] = new List<string>{"✅ 真的","❌ 假的"},
                ["timeLimit"] = 30,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("trueFalse")
            };
        }

        // 题型 13：数字华容道
        private object GeneratePuzzle(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            var puzzle = new int[3][] {
                new int[]{1,2,3},
                new int[]{4,5,6},
                new int[]{7,8,0}
            };
            return new Dictionary<string, object>
            {
                ["type"] = "puzzle",
                ["level"] = level,
                ["question"] = $"🧩 将数字按顺序排列（1-8），空格为0<br><span style='color:rgba(255,255,255,0.15);font-size:0.8rem;'>点击数字移动，限时60秒</span>",
                ["puzzle"] = puzzle,
                ["correctAnswer"] = "solved",
                ["timeLimit"] = 60,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("puzzle")
            };
        }

        // 题型 14：立体三视图
        private object GenerateThreeViewCounting(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "threeView",
                ["level"] = level,
                ["question"] = $"📐 根据三视图，计算共有多少个正方体？<br><span style='color:rgba(255,255,255,0.12);font-size:0.7rem;'>■ 代表一个正方体</span>",
                ["topView"] = "<div>俯视图</div>",
                ["frontView"] = "<div>主视图</div>",
                ["sideView"] = "<div>左视图</div>",
                ["correctAnswer"] = "8",
                ["options"] = new List<string>{"6","7","8","9"},
                ["timeLimit"] = 20,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("threeView")
            };
        }

        // 题型 15：图形计数
        private object GenerateShapeCount(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "shapeCount",
                ["level"] = level,
                ["question"] = $"🔢 以下图形中，「●」出现了几次？<br><span style='font-size:2rem;letter-spacing:8px;'>● ■ ● ▲ ●</span>",
                ["correctAnswer"] = "3",
                ["options"] = new List<string>{"2","3","4","5"},
                ["timeLimit"] = 10,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("shapeCount")
            };
        }

        // 题型 16：日期推理
        private object GenerateDateReasoning(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "dateReasoning",
                ["level"] = level,
                ["question"] = $"📅 2026年8月7日是星期五，8月15日是星期几？",
                ["correctAnswer"] = "星期六",
                ["options"] = new List<string>{"星期五","星期六","星期日","星期一"},
                ["timeLimit"] = 15,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("dateReasoning")
            };
        }

        // 题型 17：图形旋转
        private object GenerateRotateShape(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            return new Dictionary<string, object>
            {
                ["type"] = "rotate",
                ["level"] = level,
                ["question"] = $"🔄 观察下图，旋转后的角度是多少？<br><span style='color:rgba(255,255,255,0.12);font-size:0.7rem;'>左：原图 → 右：旋转后</span>",
                ["originalSvg"] = "<svg width='100' height='100'><polygon points='50,15 85,85 15,85' fill='#8B5CF6' opacity='0.85'/></svg>",
                ["rotatedSvg"] = "<svg width='100' height='100'><polygon points='50,15 85,85 15,85' fill='#8B5CF6' opacity='0.85' transform='rotate(90,50,50)'/></svg>",
                ["correctAnswer"] = "90°",
                ["options"] = new List<string>{"45°","90°","135°","180°"},
                ["timeLimit"] = 15,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("rotate")
            };
        }

        // 题型 18：1A2B猜数字
        private object Generate1A2B(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            int digitCount = difficulty <= 20 ? 3 : difficulty <= 40 ? 4 : difficulty <= 60 ? 5 : 6;
            int maxAttempts = difficulty > 80 ? 10 : 15;
            string password = GenerateUniqueDigitsWithFirstNonZero(digitCount);

            return new Dictionary<string, object>
            {
                ["type"] = "abgame",
                ["level"] = level,
                ["typeName"] = "1A2B猜数字",
                ["digitCount"] = digitCount,
                ["maxAttempts"] = maxAttempts,
                ["password"] = password,
                ["timeLimit"] = 200,
                ["typesCompleted"] = typesCompleted,
                ["question"] = $"🎯 猜一个 {digitCount} 位不重复数字（第一位不能是0）",
                ["funMessage"] = GetFunMessage("abgame")
            };
        }

        private string GenerateUniqueDigitsWithFirstNonZero(int count)
        {
            var digits = "0123456789".ToCharArray().ToList();
            var result = new List<char>();
            var used = new HashSet<char>();

            var firstCandidates = digits.Where(d => d != '0').ToList();
            char first = firstCandidates[_random.Next(firstCandidates.Count)];
            result.Add(first);
            used.Add(first);

            var remaining = digits.Where(d => !used.Contains(d)).ToList();
            for (int i = 1; i < count; i++)
            {
                int idx = _random.Next(remaining.Count);
                result.Add(remaining[idx]);
                remaining.RemoveAt(idx);
            }

            return new string(result.ToArray());
        }

        // 题型 19：颜色三重干扰
        private object GenerateTripleColorInterference(int level, int difficulty, int typesCompleted)
        {
            // ... 保持原有实现
            var pureColors = new Dictionary<string, string>
            {
                {"红色", "#FF0000"},
                {"蓝色", "#0000FF"},
                {"绿色", "#00AA00"},
                {"黄色", "#FFD700"},
                {"紫色", "#8800CC"},
                {"橙色", "#FF6600"},
                {"粉色", "#FF69B4"},
                {"青色", "#00CED1"},
            };

            var colorPool = pureColors.ToArray();
            var selectedColors = new List<KeyValuePair<string, string>>();

            while (selectedColors.Count < 3)
            {
                var c = colorPool[_random.Next(colorPool.Length)];
                if (!selectedColors.Any(x => x.Key == c.Key))
                {
                    selectedColors.Add(c);
                }
            }

            string displayWord = _singleColorWords[_random.Next(_singleColorWords.Length)];

            var shuffledColors = selectedColors.OrderBy(_ => _random.Next()).ToList();
            var wordColor = shuffledColors[0];
            var bgColor = shuffledColors[1];
            var meaningColor = shuffledColors[2];

            int maxAttempts = 50;
            int attempts = 0;
            while ((bgColor.Key == wordColor.Key || meaningColor.Key == wordColor.Key || meaningColor.Key == bgColor.Key) && attempts < maxAttempts)
            {
                shuffledColors = selectedColors.OrderBy(_ => _random.Next()).ToList();
                wordColor = shuffledColors[0];
                bgColor = shuffledColors[1];
                meaningColor = shuffledColors[2];
                attempts++;
            }

            if (bgColor.Key == wordColor.Key || meaningColor.Key == wordColor.Key || meaningColor.Key == bgColor.Key)
            {
                var allColors = pureColors.ToArray();
                wordColor = allColors[_random.Next(allColors.Length)];
                do { bgColor = allColors[_random.Next(allColors.Length)]; } while (bgColor.Key == wordColor.Key);
                do { meaningColor = allColors[_random.Next(allColors.Length)]; } while (meaningColor.Key == wordColor.Key || meaningColor.Key == bgColor.Key);
            }

            string[] questions = new[]
            {
                $"字的颜色是什么？",
                $"背景是什么颜色？",
                $"「{displayWord}」这个字本身是什么颜色的？"
            };

            int qIndex = _random.Next(3);
            string questionText = questions[qIndex];
            string correctAnswer = qIndex == 0 ? wordColor.Key : qIndex == 1 ? bgColor.Key : meaningColor.Key;

            var options = new List<string> { correctAnswer };
            int optionCount = 4 + Math.Min(difficulty / 10, 3);

            var pureColorNames = pureColors.Keys.ToList();
            var shuffled = pureColorNames.Where(c => c != correctAnswer).OrderBy(_ => _random.Next()).ToList();

            for (int i = 0; i < Math.Min(optionCount - 1, shuffled.Count); i++)
            {
                options.Add(shuffled[i]);
            }

            options = options.OrderBy(_ => _random.Next()).ToList();

            string displayHtml = $"<div style='background:{bgColor.Value};padding:2rem 3.5rem;border-radius:20px;border:3px solid rgba(255,255,255,0.05);display:inline-block;box-shadow:0 0 60px {bgColor.Value}30;'>";
            displayHtml += $"<span style='color:{wordColor.Value};font-size:4rem;font-weight:900;text-shadow:0 0 50px {wordColor.Value}50;letter-spacing:10px;'>{displayWord}</span>";
            displayHtml += "</div>";

            int timeLimit = Math.Max(5, 20 - difficulty / 7);

            return new Dictionary<string, object>
            {
                ["type"] = "tripleColor",
                ["level"] = level,
                ["question"] = $"🎯 颜色三重干扰！<br><span style='font-size:0.9rem;color:rgba(255,255,255,0.3);'>{questionText}</span>",
                ["displayHtml"] = displayHtml,
                ["correctAnswer"] = correctAnswer,
                ["options"] = options.OrderBy(_ => _random.Next()).ToList(),
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("tripleColor")
            };
        }
    }
}
