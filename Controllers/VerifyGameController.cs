using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyPersonalWebsite.Controllers
{
    public class VerifyGameController : Controller
    {
        private readonly DataSyncService _dataSync;
        private readonly Random _random = new();

        // ============================================================
        // 汉字笔画数据库（精确）
        // ============================================================
        private static readonly Dictionary<char, int> StrokeCount = new()
        {
            // 简单字（1-5画）
            {'一',1},{'二',2},{'三',3},{'十',2},{'人',2},{'大',3},{'上',3},{'下',3},{'口',3},{'山',3},
            {'中',4},{'日',4},{'月',4},{'水',4},{'火',4},{'木',4},{'天',4},{'不',4},{'开',4},{'心',4},
            {'五',4},{'六',4},{'文',4},{'方',4},{'王',4},{'车',4},{'龙',5},{'东',5},{'生',5},{'白',5},
            {'田',5},{'石',5},{'目',5},{'且',5},{'由',5},{'甲',5},{'申',5},{'电',5},{'回',6},{'地',6},
            {'羊',6},{'老',6},{'西',6},{'有',6},{'年',6},{'成',6},{'百',6},{'竹',6},{'米',6},{'红',6},
            {'全',6},{'合',6},{'安',6},{'字',6},{'平',5},{'左',5},{'右',5},{'兄',5},{'母',5},{'民',5},

            // 中等字（6-10画）
            {'行',6},{'列',6},{'此',6},{'自',6},{'至',6},{'舟',6},{'羽',6},{'耳',6},{'虫',6},{'血',6},
            {'羊',6},{'行',6},{'七',2},{'九',2},{'八',2},{'四',5},{'国',8},{'和',8},{'的',8},{'你',7},
            {'我',7},{'他',5},{'她',6},{'们',5},{'是',9},{'在',6},{'这',7},{'那',7},{'来',7},{'去',5},
            {'学',8},{'生',5},{'校',10},{'园',7},{'书',4},{'写',5},{'画',8},{'家',10},{'美',9},{'丽',7},
            {'春',9},{'夏',10},{'秋',9},{'冬',5},{'南',9},{'北',5},{'城',9},{'市',5},{'风',4},{'雨',8},
            {'雪',11},{'星',9},{'花',7},{'草',9},{'树',9},{'林',8},{'森',12},{'竹',6},{'梅',11},{'兰',5},

            // 复杂字（10-15画）
            {'菊',11},{'荷',10},{'红',6},{'黄',11},{'蓝',13},{'绿',11},{'白',5},{'黑',12},{'紫',12},
            {'诚',8},{'信',9},{'敬',12},{'爱',10},{'善',12},{'良',7},{'猫',11},{'熊',14},{'象',12},
            {'窗',12},{'楼',13},{'桥',10},{'船',11},{'路',13},{'港',12},{'海',10},{'湖',12},{'流',10},
            {'峰',10},{'岭',8},{'谷',7},{'岩',8},{'洞',9},{'泉',9},{'溪',13},{'池',6},{'梦',11},{'望',11},
            {'数',13},{'感',13},{'解',13},{'决',6},{'游',12},{'戏',6},{'运',7},{'动',11},{'智',12},{'慧',15},

            // 超复杂字（16+画）
            {'露',21},{{'霜',17},{{'霞',17},{{'霸',21},{{'魔',20},{{'警',19},{{'耀',20},{{'爆',19},
            {'攀',19},{{'变',19},{{'蛮',23},{{'湾',25},{{'镶',25},{{'囊',22},{{'嚷',23},{{'壤',20},
            {'懿',22},{{'懿',22},{{'囊',22},{{'罐',23},{{'噩',16},{{'嚣',21},{{'龘',33},{{'灏',25}
        };

        // 字形相似度检测（用于干扰项）
        private static readonly Dictionary<char, char[]> SimilarChars = new()
        {
            // 简单字干扰
            {'人', new[]{'入','八','大'}},
            {'大', new[]{'太','犬','天'}},
            {'天', new[]{'大','夫','无'}},
            {'日', new[]{'曰','目','白'}},
            {'月', new[]{'用','目','丹'}},
            {'木', new[]{'术','本','未'}},
            {'王', new[]{'玉','主','正'}},
            {'土', new[]{'士','王','干'}},
            {'田', new[]{'由','甲','申'}},
            {'白', new[]{'日','自','百'}},
            {'自', new[]{'目','白','首'}},
            {'我', new[]{'找','伐','成'}},
            {'你', new[]{'您','尔','称'}},
            {'他', new[]{'她','地','也'}},
            {'的', new[]{'得','确','的'}},
            {'是', new[]{'足','走','定'}},
            {'不', new[]{'下','上','木'}},
            {'了', new[]{'子','于','孑'}},
            {'在', new[]{'存','左','右'}},
            {'学', new[]{'觉','校','举'}},
            {'生', new[]{'牛','先','姓'}},
            {'校', new[]{'校','较','铰'}},
            {'海', new[]{'每','悔','诲'}},
            {'湖', new[]{'糊','胡','蝴'}},
            {'路', new[]{'露','陆','骆'}},
            {'爱', new[]{'受','暖','缓'}},
            {'善', new[]{'美','羡','养'}},
            {'国', new[]{'园','圆','图'}},
            {'家', new[]{'嫁','稼','加'}},
            {'春', new[]{'看','着','香'}},
            {'秋', new[]{'伙','愁','排'}},
            {'风', new[]{'凤','凡','几'}},
            {'雨', new[]{'雪','雷','零'}},
            {'雪', new[]{'云','雨','零'}},
            {'星', new[]{'醒','生','胜'}},
            {'花', new[]{'化','华','草'}},
            {'草', new[]{'早','华','花'}},
            {'树', new[]{'对','村','权'}},
            {'林', new[]{'木','森','彬'}},
            {'森', new[]{'林','木','众'}},
            {'龙', new[]{'尤','庞','宠'}},
            {'虎', new[]{'虚','虑','虞'}},
            {'象', new[]{'像','橡','豫'}},
            {'猫', new[]{'描','苗','锚'}},
            {'熊', new[]{'能','态','雄'}},
            {'窗', new[]{'囱','穿','空'}},
            {'楼', new[]{'搂','缕','镂'}},
            {'桥', new[]{'娇','轿','侨'}},
            {'船', new[]{'沿','铅','舷'}},
            {'港', new[]{'巷','共','洪'}},
            {'峰', new[]{'锋','蜂','逢'}},
            {'岩', new[]{'山','石','宕'}},
            {'泉', new[]{'白','水','原'}},
            {'溪', new[]{'奚','蹊','鸡'}},
            {'梦', new[]{'梦','林','夕'}},
            {'望', new[]{'忘','芒','亡'}},
            {'智', new[]{'知','哲','答'}},
            {'慧', new[]{'惠','穗','心'}},
            {'暴', new[]{'瀑','爆','晒'}},
            {'攀', new[]{'樊','潘','番'}},
            {'变', new[]{'弯','恋','蛮'}},
            {'魔', new[]{'摩','磨','麟'}},
            {'警', new[]{'惊','鲸','敬'}},
            {'耀', new[]{'跃','岳','光'}},
            {'镶', new[]{'让','壤','襄'}},
            {'囊', new[]{'嚷','壤','瓤'}},
            {'懿', new[]{'壹','懿','肆'}},
            {'罐', new[]{'灌','观','鹳'}},
            {'噩', new[]{'鄂','鳄','恶'}},
            {'嚣', new[]{'器','喧','具'}},
            {'龘', new[]{'龙','龘','爨'}},
            {'灏', new[]{'景','影','浩'}},
        };

        // 趣味反馈语
        private static readonly Dictionary<string, string[]> FunMessages = new()
        {
            {"text", new[]{"👍 眼神不错！", "👀 火眼金睛！", "🎯 精准识别！", "🔍 细节大师！"}},
            {"arithmetic", new[]{"🧮 计算天才！", "💡 数学真好！", "🤓 逻辑满分！", "✨ 聪明如你！"}},
            {"stroke", new[]{"📝 文化大师！", "✍️ 汉字通！", "🏯 书法家！", "📖 国学达人！"}},
            {"color", new[]{"🎨 色彩高手！", "🌈 火眼金睛！", "✨ 审美满分！", "🖌️ 艺术家！"}},
            {"findDifferent", new[]{"🔍 眼力超群！", "🎯 观察大师！", "👀 细节控！", "⭐ 侦探级！"}},
            {"reverse", new[]{"🔄 逆向思维！", "🧠 大脑太强！", "💪 空间感满分！", "✨ 天才！"}},
            {"missingLetter", new[]{"🔤 词汇大师！", "📚 学霸！", "✍️ 拼写天才！", "📝 英语通！"}},
            {"quickTap", new[]{"⚡ 手速超快！", "💨 闪电侠！", "🔥 反应满分！", "🎮 游戏达人！"}},
            {"idiom", new[]{"📖 成语大师！", "🏯 国学通！", "✍️ 文学天才！", "📚 行走的词典！"}},
            {"chineseNumber", new[]{"🔢 数字天才！", "🧮 数学真好！", "💡 逻辑满分！", "✨ 聪明如你！"}},
            {"caseConversion", new[]{"🔤 字母通！", "📚 词汇大师！", "✍️ 拼写天才！", "📝 英语达人！"}},
            {"pinyin", new[]{"🔊 语音天才！", "🎙️ 发音标准！", "📢 朗读大师！", "✨ 语言学家！"}},
            {"inverseColor", new[]{"🎨 色彩大师！", "🌈 视觉天才！", "✨ 审美满分！", "🖌️ 艺术家！"}},
            {"mirror", new[]{"🪞 空间大师！", "🧠 大脑太强！", "💪 逻辑满分！", "✨ 天才！"}},
            {"keyboard", new[]{"⌨️ 键盘大师！", "💨 手速超快！", "🔥 打字天才！", "🎮 游戏达人！"}},
            {"countChar", new[]{"🔢 计数天才！", "🧮 数学真好！", "💡 逻辑满分！", "✨ 聪明如你！"}},
            {"memory", new[]{"🧠 记忆大师！", "💪 大脑超强！", "✨ 天才记忆力！", "📚 学霸！"}},
            {"direction", new[]{"🧭 方向感满分！", "🗺️ 导航天才！", "✨ 空间大师！", "💪 逻辑满分！"}},
            {"logic", new[]{"💡 逻辑天才！", "🧠 大脑太强！", "✨ 推理大师！", "📊 分析满分！"}},
            {"ultimate", new[]{"💀 太强了！", "🔥 终极王者！", "👑 人类之光！", "✨ 完美通关！"}},
        };

        public VerifyGameController(DataSyncService dataSync)
        {
            _dataSync = dataSync;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.Username = HttpContext.Session.GetString("Username");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveScore(int score, int level, int maxCombo, int passed)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

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

        [HttpGet]
        public async Task<IActionResult> GetRanking()
        {
            try
            {
                var allStats = await _dataSync.GetAllUserGameStatsAsync();
                var users = await _dataSync.GetAllUsersAsync();

                var ranking = allStats
                    .Where(s => s.TotalPoints > 0)
                    .OrderByDescending(s => s.TotalPoints)
                    .ThenByDescending(s => s.MaxLevel)
                    .Take(50)
                    .Select(s =>
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
                            gamesPlayed = s.GamesPlayed
                        };
                    })
                    .ToList();

                return Json(new { success = true, data = ranking });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取排行榜失败: {ex.Message}");
                return Json(new { success = false, data = new System.Collections.Generic.List<object>() });
            }
        }

        // ============================================================
        // 🎮 游戏 API：获取挑战
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
        // 🧠 挑战生成器（20种类型 × 5关 = 100关，难度渐进）
        // ============================================================
        private object GenerateChallenge(int level)
        {
            int typeIndex = (level - 1) % 20;
            int subLevel = ((level - 1) / 20) + 1;

            // ⭐ 关键：难度随总关卡指数增长
            int difficulty = level;  // 1-100

            switch (typeIndex)
            {
                case 0: return GenerateTextRecognition(level, difficulty);
                case 1: return GenerateArithmetic(level, difficulty);
                case 2: return GenerateStrokeCount(level, difficulty);
                case 3: return GenerateColorRecognition(level, difficulty);
                case 4: return GenerateFindDifferent(level, difficulty);
                case 5: return GenerateReverseText(level, difficulty);
                case 6: return GenerateMissingLetter(level, difficulty);
                case 7: return GenerateQuickTap(level, difficulty);
                case 8: return GenerateIdiomFill(level, difficulty);
                case 9: return GenerateChineseToNumber(level, difficulty);
                case 10: return GenerateCaseConversion(level, difficulty);
                case 11: return GeneratePinyinMatch(level, difficulty);
                case 12: return GenerateInverseColor(level, difficulty);
                case 13: return GenerateMirrorLetter(level, difficulty);
                case 14: return GenerateKeyboardNeighbor(level, difficulty);
                case 15: return GenerateCharacterCount(level, difficulty);
                case 16: return GenerateMemoryChallenge(level, difficulty);
                case 17: return GenerateDirection(level, difficulty);
                case 18: return GenerateMathLogic(level, difficulty);
                default: return GenerateUltimate(level, difficulty);
            }
        }

        // ============================================================
        // ⭐ 1. 文字识别（难度渐进：3位→10位，扭曲+干扰递增）
        // ============================================================
        private object GenerateTextRecognition(int level, int difficulty)
        {
            // 长度：3-10位
            int length = Math.Min(3 + difficulty / 10, 10);

            // 字符池：随难度增加使用更相似的字符
            string text = "";
            for (int i = 0; i < length; i++)
            {
                text += GetRandomChar(difficulty);
            }

            // 干扰线：10-80条
            int lineCount = 10 + difficulty * 3;

            // 扭曲程度：5-50度
            int distortion = 5 + difficulty / 2;

            // 模糊度（额外干扰）
            int noiseLevel = Math.Min(difficulty / 5, 20);

            var svg = GenerateSvg(text, distortion, lineCount, noiseLevel);

            var options = GenerateTextOptions(text, difficulty);

            // 时间限制：随难度递减
            int timeLimit = Math.Max(5, 18 - difficulty / 6);

            return new
            {
                type = "text",
                level = level,
                question = $"👁️ 请输入下方图片中的文字（{length}位）",
                imageSvg = svg,
                correctAnswer = text,
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("text")
            };
        }

        private char GetRandomChar(int difficulty)
        {
            // 低难度：字母数字混合
            // 高难度：加入相似字符
            var pool = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

            // 难度20以上加入更多混淆字符
            if (difficulty > 20)
            {
                pool = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()-_+=<>?/".ToCharArray();
            }

            return pool[_random.Next(pool.Length)];
        }

        private List<string> GenerateTextOptions(string correct, int difficulty)
        {
            var options = new List<string> { correct };
            int count = 4 + Math.Min(difficulty / 10, 4);

            while (options.Count < count)
            {
                string fake = "";
                for (int i = 0; i < correct.Length; i++)
                {
                    // 高概率生成相似字符
                    if (_random.Next(100) < 50 + difficulty / 3)
                    {
                        fake += GetSimilarChar(correct[i]);
                    }
                    else
                    {
                        fake += GetRandomChar(difficulty);
                    }
                }
                if (!options.Contains(fake))
                {
                    options.Add(fake);
                }
            }
            return options.OrderBy(_ => _random.Next()).ToList();
        }

        private char GetSimilarChar(char c)
        {
            var map = new Dictionary<char, char[]>
            {
                {'0', new[]{'O','Q','D','C'}},
                {'O', new[]{'0','Q','D','C'}},
                {'Q', new[]{'0','O','D','C'}},
                {'D', new[]{'0','O','Q','C'}},
                {'C', new[]{'G','O','Q','0'}},
                {'G', new[]{'C','O','Q','6'}},
                {'1', new[]{'I','L','!','|'}},
                {'I', new[]{'1','L','!','|'}},
                {'L', new[]{'1','I','J','|'}},
                {'J', new[]{'L','I','T','Y'}},
                {'T', new[]{'Y','7','L','J'}},
                {'Y', new[]{'V','T','7','J'}},
                {'V', new[]{'Y','U','W','M'}},
                {'U', new[]{'V','J','L','I'}},
                {'W', new[]{'M','V','N','U'}},
                {'M', new[]{'W','N','V','U'}},
                {'N', new[]{'M','H','K','W'}},
                {'H', new[]{'N','K','A','M'}},
                {'K', new[]{'H','N','M','X'}},
                {'X', new[]{'K','*','+','x'}},
                {'A', new[]{'4','H','R','K'}},
                {'R', new[]{'P','A','K','B'}},
                {'P', new[]{'R','B','9','D'}},
                {'B', new[]{'8','3','P','R'}},
                {'3', new[]{'8','B','E','S'}},
                {'8', new[]{'3','B','6','S'}},
                {'6', new[]{'8','9','G','S'}},
                {'9', new[]{'6','8','P','G'}},
                {'S', new[]{'5','$','8','3'}},
                {'5', new[]{'S','$','8','3'}},
                {'2', new[]{'Z','7','L','I'}},
                {'Z', new[]{'2','7','N','M'}},
                {'7', new[]{'2','Z','T','Y'}},
                {'4', new[]{'A','H','K','R'}},
                {'E', new[]{'3','B','F','P'}},
                {'F', new[]{'E','P','T','Y'}},
                {'$', new[]{'S','5','8','3'}},
                {'!', new[]{'1','I','L','|'}},
                {'@', new[]{'0','O','Q','D'}},
                {'#', new[]{'H','K','M','N'}},
                {'%', new[]{'S','5','8','3'}},
                {'^', new[]{'V','Y','T','W'}},
                {'&', new[]{'8','3','B','6'}},
                {'*', new[]{'X','+','K','H'}},
                {'(', new[]{'C','G','O','Q'}},
                {')', new[]{'O','C','G','Q'}},
                {'-', new[]{'_','=','+','T'}},
                {'_', new[]{'-','=','+','T'}},
                {'=', new[]{'-','_','+','T'}},
                {'+', new[]{'*','X','K','H'}},
                {'<', new[]{'C','G','O','Q'}},
                {'>', new[]{'C','G','O','Q'}},
                {'?', new[]{'7','Z','2','T'}},
            };

            if (map.ContainsKey(c))
            {
                var similar = map[c];
                return similar[_random.Next(similar.Length)];
            }
            return GetRandomChar(50);
        }

        // ============================================================
        // SVG 生成（增强版：更多干扰）
        // ============================================================
        private string GenerateSvg(string text, int distortion, int lineCount, int noiseLevel = 0)
        {
            int width = 320;
            int height = 100;

            var svg = new System.Text.StringBuilder();
            svg.AppendLine($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>");

            // 背景（随机颜色）
            int bgR = _random.Next(220, 255);
            int bgG = _random.Next(220, 255);
            int bgB = _random.Next(220, 255);
            svg.AppendLine($"<rect width='{width}' height='{height}' rx='10' fill='rgb({bgR},{bgG},{bgB})'/>");

            // 干扰线
            for (int i = 0; i < lineCount; i++)
            {
                int r = _random.Next(100, 220);
                int g = _random.Next(100, 220);
                int b = _random.Next(100, 220);
                int x1 = _random.Next(-30, width + 30);
                int y1 = _random.Next(-30, height + 30);
                int x2 = _random.Next(-30, width + 30);
                int y2 = _random.Next(-30, height + 30);
                int w = _random.Next(1, 4);
                double alpha = 0.2 + _random.NextDouble() * 0.4;
                svg.AppendLine($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='rgb({r},{g},{b})' stroke-width='{w}' opacity='{alpha:F2}'/>");
            }

            // 噪点
            int dotCount = 100 + noiseLevel * 10;
            for (int i = 0; i < dotCount; i++)
            {
                int x = _random.Next(0, width);
                int y = _random.Next(0, height);
                int r = _random.Next(150, 220);
                int g = _random.Next(150, 220);
                int b = _random.Next(150, 220);
                int size = _random.Next(1, 4);
                svg.AppendLine($"<circle cx='{x}' cy='{y}' r='{size}' fill='rgb({r},{g},{b})' opacity='0.3'/>");
            }

            // 字符
            int spacing = (width - 40) / text.Length;
            int startX = 20;

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                int r = _random.Next(5, 70);
                int g = _random.Next(5, 70);
                int b = _random.Next(5, 70);
                int angle = _random.Next(-distortion, distortion);
                int fontSize = _random.Next(36, 52);
                int x = startX + i * spacing + _random.Next(-8, 8);
                int y = height / 2 + 15 + _random.Next(-12, 12);

                // 模糊效果（高难度）
                string filter = "";
                if (noiseLevel > 8)
                {
                    filter = $"filter='url(#blur{i})'";
                    svg.AppendLine($"<defs><filter id='blur{i}'><feGaussianBlur stdDeviation='{0.2 + noiseLevel / 50}'/></filter></defs>");
                }

                svg.AppendLine($"<text x='{x}' y='{y}' font-family='Arial,sans-serif' font-size='{fontSize}' font-weight='bold' fill='rgb({r},{g},{b})' transform='rotate({angle} {x} {y})' text-anchor='middle' dominant-baseline='central' {filter}>{ch}</text>");
            }

            svg.AppendLine("</svg>");
            return svg.ToString();
        }

        // ============================================================
        // 2. 算术（渐难：大数+混合运算）
        // ============================================================
        private object GenerateArithmetic(int level, int difficulty)
        {
            int maxNum = 10 + difficulty * 5;
            int a = _random.Next(5, maxNum);
            int b = _random.Next(1, Math.Max(2, maxNum / 2));

            string[] ops;
            if (difficulty > 60) ops = new[] { '+', '-', '×', '÷', '^' };
            else if (difficulty > 30) ops = new[] { '+', '-', '×', '÷' };
            else if (difficulty > 15) ops = new[] { '+', '-', '×' };
            else ops = new[] { '+', '-' };

            string op = ops[_random.Next(ops.Length)];
            int result;

            switch (op)
            {
                case '+': result = a + b; break;
                case '-': result = a - b; break;
                case '×': result = a * b; break;
                case '÷': result = a / b; break;
                case '^': result = (int)Math.Pow(a, Math.Min(b, 3)); break;
                default: result = a + b; break;
            }

            if (result < 0) result = Math.Abs(result);
            if (result == 0) result = a + b;

            var options = GenerateNumberOptions(result, 4 + difficulty / 10, difficulty);

            int timeLimit = Math.Max(4, 15 - difficulty / 8);

            return new
            {
                type = "arithmetic",
                level = level,
                question = $"🧮 {a} {op} {b} = ?",
                correctAnswer = result.ToString(),
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("arithmetic")
            };
        }

        private List<string> GenerateNumberOptions(int correct, int count, int difficulty)
        {
            var options = new List<string> { correct.ToString() };
            int range = Math.Max(2, 5 + difficulty / 5);

            while (options.Count < count)
            {
                int fake = correct + _random.Next(-range, range + 1);
                if (fake < 0) fake = _random.Next(1, 20);
                if (fake > 99999) fake = correct - _random.Next(1, 10);
                if (fake < 0) fake = 1;
                string str = fake.ToString();
                if (!options.Contains(str) && fake != correct)
                {
                    options.Add(str);
                }
            }
            return options.OrderBy(_ => _random.Next()).ToList();
        }

        // ============================================================
        // 3. 汉字笔画（渐难：从简单字到超复杂字）
        // ============================================================
        private object GenerateStrokeCount(int level, int difficulty)
        {
            // 根据难度选择汉字
            var allChars = StrokeCount.Keys.ToArray();
            char ch;

            if (difficulty < 20)
            {
                // 简单字（1-5画）
                var simple = allChars.Where(c => StrokeCount[c] <= 5).ToArray();
                ch = simple[_random.Next(simple.Length)];
            }
            else if (difficulty < 40)
            {
                // 中等字（6-10画）
                var medium = allChars.Where(c => StrokeCount[c] >= 6 && StrokeCount[c] <= 10).ToArray();
                ch = medium[_random.Next(medium.Length)];
            }
            else if (difficulty < 60)
            {
                // 复杂字（11-15画）
                var complex = allChars.Where(c => StrokeCount[c] >= 11 && StrokeCount[c] <= 15).ToArray();
                ch = complex[_random.Next(complex.Length)];
            }
            else if (difficulty < 80)
            {
                // 超复杂字（16-20画）
                var ultra = allChars.Where(c => StrokeCount[c] >= 16 && StrokeCount[c] <= 20).ToArray();
                ch = ultra[_random.Next(ultra.Length)];
            }
            else
            {
                // 终极字（20+画）
                var ultimate = allChars.Where(c => StrokeCount[c] > 20).ToArray();
                ch = ultimate[_random.Next(ultimate.Length)];
            }

            int correct = StrokeCount[ch];

            // 生成干扰项（相似笔画数）
            var options = new List<string> { correct.ToString() };
            int count = 4 + Math.Min(difficulty / 15, 4);

            while (options.Count < count)
            {
                int fake = correct + _random.Next(-3, 4);
                if (fake < 1) fake = correct + _random.Next(2, 5);
                if (fake > 35) fake = correct - _random.Next(2, 5);
                if (fake < 1) fake = 3 + _random.Next(1, 5);

                string str = fake.ToString();
                if (!options.Contains(str) && fake != correct)
                {
                    options.Add(str);
                }
            }

            int timeLimit = Math.Max(4, 14 - difficulty / 10);

            return new
            {
                type = "stroke",
                level = level,
                question = $"📝 「{ch}」字有几画？",
                correctAnswer = correct.ToString(),
                options = options.OrderBy(_ => _random.Next()).ToList(),
                timeLimit = timeLimit,
                funMessage = GetFunMessage("stroke")
            };
        }

        // ============================================================
        // 4. 颜色识别（渐难：相似色+更多选项）
        // ============================================================
        private object GenerateColorRecognition(int level, int difficulty)
        {
            var colors = new[]
            {
                new { name = "红色", hex = "#FF0000" },
                new { name = "深红", hex = "#8B0000" },
                new { name = "粉色", hex = "#FF69B4" },
                new { name = "浅粉", hex = "#FFB6C1" },
                new { name = "蓝色", hex = "#0000FF" },
                new { name = "深蓝", hex = "#00008B" },
                new { name = "天蓝", hex = "#87CEEB" },
                new { name = "青色", hex = "#00CED1" },
                new { name = "绿色", hex = "#00AA00" },
                new { name = "深绿", hex = "#006400" },
                new { name = "草绿", hex = "#7CFC00" },
                new { name = "黄色", hex = "#FFD700" },
                new { name = "金色", hex = "#DAA520" },
                new { name = "橙色", hex = "#FF6600" },
                new { name = "深橙", hex = "#CC5500" },
                new { name = "紫色", hex = "#8800CC" },
                new { name = "深紫", hex = "#4B0082" },
                new { name = "棕色", hex = "#8B4513" },
                new { name = "灰色", hex = "#808080" },
                new { name = "黑色", hex = "#000000" },
                new { name = "白色", hex = "#FFFFFF" },
                new { name = "银灰", hex = "#C0C0C0" },
                new { name = "紫罗兰", hex = "#EE82EE" },
                new { name = "靛蓝", hex = "#4B0082" },
                new { name = "玫瑰", hex = "#FF007F" },
                new { name = "柠檬", hex = "#FFF700" },
                new { name = "薄荷", hex = "#98FF98" },
                new { name = "珊瑚", hex = "#FF7F50" },
                new { name = "象牙", hex = "#FFFFF0" },
                new { name = "巧克力", hex = "#D2691E" },
                new { name = "琥珀", hex = "#FFBF00" },
            };

            var pool = difficulty < 15 ? colors.Take(8).ToArray() :
                       difficulty < 30 ? colors.Take(15).ToArray() :
                       difficulty < 50 ? colors.Take(22).ToArray() :
                       colors;

            var selected = pool[_random.Next(pool.Length)];

            var options = new List<string> { selected.name };
            var poolList = pool.ToList();
            poolList.Remove(selected);

            int count = 4 + Math.Min(difficulty / 12, 4);
            var shuffledPool = poolList.OrderBy(_ => _random.Next()).ToList();

            for (int i = 0; i < count - 1 && i < shuffledPool.Count; i++)
            {
                options.Add(shuffledPool[i].name);
            }

            int timeLimit = Math.Max(3, 10 - difficulty / 15);

            return new
            {
                type = "color",
                level = level,
                question = "🎨 下面文字是什么颜色？",
                displayText = $"<span style='color:{selected.hex};font-size:2.5rem;font-weight:bold;text-shadow:0 0 30px {selected.hex}30;'>{GetRandomColorText()}</span>",
                correctAnswer = selected.name,
                options = options.OrderBy(_ => _random.Next()).ToList(),
                timeLimit = timeLimit,
                funMessage = GetFunMessage("color")
            };
        }

        private string GetRandomColorText()
        {
            var texts = new[] { "颜色", "色彩", "鲜艳", "绚丽", "斑斓", "彩色", "炫彩", "斑斓" };
            return texts[_random.Next(texts.Length)];
        }

        // ============================================================
        // 5. 找不同（渐难：更多字符+更相似的替换）
        // ============================================================
        private object GenerateFindDifferent(int level, int difficulty)
        {
            int length = 4 + difficulty / 5;
            if (length > 15) length = 15;

            string baseStr = "";
            for (int i = 0; i < length; i++)
            {
                baseStr += GetRandomChar(difficulty);
            }

            int pos = _random.Next(length);
            char original = baseStr[pos];
            char replacement = GetSimilarChar(original);

            // 确保真的不同
            while (replacement == original)
            {
                replacement = GetSimilarChar(original);
            }

            char correct = original;

            string display = "";
            for (int i = 0; i < length; i++)
            {
                if (i == pos)
                {
                    display += $"<span style='color:#EC4899;font-weight:bold;text-decoration:underline;'>{replacement}</span>";
                }
                else
                {
                    display += baseStr[i];
                }
            }

            var options = new List<string> { correct.ToString() };
            int count = 4 + Math.Min(difficulty / 15, 4);

            while (options.Count < count)
            {
                char fake = GetRandomChar(difficulty);
                if (fake != correct && !options.Contains(fake.ToString()))
                {
                    options.Add(fake.ToString());
                }
            }

            int timeLimit = Math.Max(4, 14 - difficulty / 8);

            return new
            {
                type = "findDifferent",
                level = level,
                question = $"🔍 以下字符中，哪个被替换了？（{length}个字符）",
                displayText = display,
                correctAnswer = correct.ToString(),
                options = options.OrderBy(_ => _random.Next()).ToList(),
                timeLimit = timeLimit,
                funMessage = GetFunMessage("findDifferent")
            };
        }

        // ============================================================
        // 6. 倒序识别
        // ============================================================
        private object GenerateReverseText(int level, int difficulty)
        {
            int length = Math.Min(3 + difficulty / 8, 8);
            string text = "";
            for (int i = 0; i < length; i++)
            {
                text += GetRandomChar(difficulty);
            }

            string reversed = new string(text.Reverse().ToArray());

            int lineCount = 10 + difficulty * 2;
            int distortion = 5 + difficulty / 3;
            var svg = GenerateSvg(text, distortion, lineCount);

            var options = GenerateTextOptions(reversed, difficulty);

            int timeLimit = Math.Max(5, 16 - difficulty / 6);

            return new
            {
                type = "reverse",
                level = level,
                question = "🔄 图片中的文字是什么？（倒过来了）",
                imageSvg = svg,
                correctAnswer = reversed,
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("reverse")
            };
        }

        // ============================================================
        // 7-19. 其他类型（简化版，同样渐进难度）
        // ============================================================
        private object GenerateMissingLetter(int level, int difficulty)
        {
            int len = Math.Min(3 + difficulty / 8, 8);
            string word = "";
            for (int i = 0; i < len; i++) word += GetRandomChar(difficulty);

            int idx = _random.Next(len);
            char correct = word[idx];
            char[] display = word.ToCharArray();
            display[idx] = '_';

            var options = GenerateTextOptions(correct.ToString(), difficulty);
            int timeLimit = Math.Max(4, 12 - difficulty / 8);

            return new
            {
                type = "missingLetter",
                level = level,
                question = $"🔤 补全：{new string(display)}",
                correctAnswer = correct.ToString(),
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("missingLetter")
            };
        }

        private object GenerateQuickTap(int level, int difficulty)
        {
            char target = GetRandomChar(difficulty);
            var options = GenerateTextOptions(target.ToString(), difficulty);

            int timeLimit = Math.Max(3, 10 - difficulty / 10);

            return new
            {
                type = "quickTap",
                level = level,
                question = "⚡ 快速找到目标字符！",
                correctAnswer = target.ToString(),
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("quickTap")
            };
        }

        private object GenerateIdiomFill(int level, int difficulty)
        {
            var idioms = new[] { "一马当先", "龙飞凤舞", "画蛇添足", "守株待兔", "狐假虎威",
                "马到成功", "鸟语花香", "鱼目混珠", "鹤立鸡群", "龙腾虎跃",
                "画龙点睛", "亡羊补牢", "杯弓蛇影", "指鹿为马", "马不停蹄",
                "一帆风顺", "万事如意", "心想事成", "步步高升", "蒸蒸日上" };

            var idiom = idioms[_random.Next(idioms.Length)];
            int idx = _random.Next(idiom.Length);
            char correct = idiom[idx];
            char[] display = idiom.ToCharArray();
            display[idx] = '□';

            var options = GenerateTextOptions(correct.ToString(), difficulty);
            int timeLimit = Math.Max(4, 12 - difficulty / 8);

            return new
            {
                type = "idiom",
                level = level,
                question = $"📖 补全成语：{new string(display)}",
                correctAnswer = correct.ToString(),
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("idiom")
            };
        }

        private object GenerateChineseToNumber(int level, int difficulty)
        {
            var cn = new[] { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
            int num = _random.Next(0, 11);
            var options = GenerateNumberOptions(num, 4 + difficulty / 10, difficulty);
            int timeLimit = Math.Max(3, 10 - difficulty / 10);

            return new
            {
                type = "chineseNumber",
                level = level,
                question = $"🔢 「{cn[num]}」对应的数字是？",
                correctAnswer = num.ToString(),
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("chineseNumber")
            };
        }

        private object GenerateCaseConversion(int level, int difficulty)
        {
            var letters = "ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();
            char c = letters[_random.Next(letters.Length)];
            bool isUpper = _random.Next(2) == 0;

            var options = GenerateTextOptions(isUpper ? c.ToString().ToLower() : c.ToString().ToUpper(), difficulty);
            int timeLimit = Math.Max(3, 10 - difficulty / 10);

            return new
            {
                type = "caseConversion",
                level = level,
                question = isUpper ? $"🔤 字母「{c}」的小写是？" : $"🔤 字母「{c.ToString().ToLower()}」的大写是？",
                correctAnswer = isUpper ? c.ToString().ToLower() : c.ToString().ToUpper(),
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("caseConversion")
            };
        }

        private object GeneratePinyinMatch(int level, int difficulty)
        {
            var letters = "ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();
            char c = letters[_random.Next(letters.Length)];

            var options = GenerateTextOptions(c.ToString(), difficulty);
            int timeLimit = Math.Max(3, 10 - difficulty / 10);

            return new
            {
                type = "pinyin",
                level = level,
                question = $"🔊 字母「{c}」的读音是？",
                correctAnswer = c.ToString(),
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("pinyin")
            };
        }

        private object GenerateInverseColor(int level, int difficulty)
        {
            var colors = new[] { "黑色", "白色" };
            int idx = _random.Next(2);
            string color = colors[idx];
            string bg = idx == 0 ? "#FFFFFF" : "#000000";

            int timeLimit = Math.Max(3, 8 - difficulty / 15);

            return new
            {
                type = "inverseColor",
                level = level,
                question = "🎨 下面文字是什么颜色？（注意背景）",
                displayText = $"<span style='color:{color == "黑色" ? "#000000" : "#FFFFFF"};background:{bg};padding:0.3rem 1.5rem;border-radius:8px;font-size:2rem;font-weight:bold;'>{GetRandomColorText()}</span>",
                correctAnswer = color,
                options = new List<string> { "黑色", "白色" },
                timeLimit = timeLimit,
                funMessage = GetFunMessage("inverseColor")
            };
        }

        private object GenerateMirrorLetter(int level, int difficulty)
        {
            var mirrorMap = new Dictionary<char, char> {
                {'A','A'},{'H','H'},{'I','I'},{'M','M'},{'O','O'},{'T','T'},{'U','U'},{'V','V'},{'W','W'},{'X','X'},{'Y','Y'},
                {'B','B'},{'C','C'},{'D','D'},{'E','E'},{'K','K'},{'P','P'},{'S','S'},{'Z','Z'}
            };
            var keys = mirrorMap.Keys.ToArray();
            char c = keys[_random.Next(keys.Length)];
            char correct = mirrorMap[c];

            var options = GenerateTextOptions(correct.ToString(), difficulty);
            int timeLimit = Math.Max(3, 10 - difficulty / 10);

            return new
            {
                type = "mirror",
                level = level,
                question = $"🪞 字母「{c}」的镜像字母是？",
                correctAnswer = correct.ToString(),
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("mirror")
            };
        }

        private object GenerateKeyboardNeighbor(int level, int difficulty)
        {
            var keys = "QWERTYUIOPASDFGHJKLZXCVBNM".ToCharArray();
            char c = keys[_random.Next(keys.Length)];

            var options = GenerateTextOptions(c.ToString(), difficulty);
            int timeLimit = Math.Max(3, 10 - difficulty / 10);

            return new
            {
                type = "keyboard",
                level = level,
                question = $"⌨️ 键盘上「{c}」的右边键是？",
                correctAnswer = c.ToString(),
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("keyboard")
            };
        }

        private object GenerateCharacterCount(int level, int difficulty)
        {
            int len = Math.Min(6 + difficulty / 6, 15);
            string text = "";
            for (int i = 0; i < len; i++) text += GetRandomChar(difficulty);

            char target = GetRandomChar(difficulty);
            int count = text.Count(c => c == target);

            var options = GenerateNumberOptions(count, 4 + difficulty / 10, difficulty);
            int timeLimit = Math.Max(4, 12 - difficulty / 8);

            return new
            {
                type = "countChar",
                level = level,
                question = $"🔢 字符「{target}」在「{text}」中出现几次？",
                correctAnswer = count.ToString(),
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("countChar")
            };
        }

        private object GenerateMemoryChallenge(int level, int difficulty)
        {
            int len = Math.Min(3 + difficulty / 8, 8);
            string text = "";
            for (int i = 0; i < len; i++) text += _random.Next(0, 10);

            var options = GenerateTextOptions(text, difficulty);
            int timeLimit = Math.Max(5, 14 - difficulty / 8);

            return new
            {
                type = "memory",
                level = level,
                question = $"🧠 记住这个数字：{text} （然后选择它）",
                correctAnswer = text,
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("memory")
            };
        }

        private object GenerateDirection(int level, int difficulty)
        {
            var dirs = new[] { "上", "下", "左", "右" };
            string dir = dirs[_random.Next(dirs.Length)];
            var opposite = new Dictionary<string, string> { {"上","下"},{"下","上"},{"左","右"},{"右","左"} };

            var options = new List<string> { opposite[dir] };
            var pool = dirs.Where(d => d != dir && d != opposite[dir]).ToList();
            while (options.Count < 4 && pool.Count > 0)
            {
                int idx = _random.Next(pool.Count);
                options.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            int timeLimit = Math.Max(3, 8 - difficulty / 15);

            return new
            {
                type = "direction",
                level = level,
                question = $"🧭 请选择「{dir}」的相反方向",
                correctAnswer = opposite[dir],
                options = options.OrderBy(_ => _random.Next()).ToList(),
                timeLimit = timeLimit,
                funMessage = GetFunMessage("direction")
            };
        }

        private object GenerateMathLogic(int level, int difficulty)
        {
            int a = _random.Next(2, 5 + difficulty / 5);
            int b = _random.Next(2, 5 + difficulty / 5);
            int c = a * b;
            int missing = _random.Next(3);

            string question;
            string correct;
            int timeLimit = Math.Max(4, 12 - difficulty / 8);

            if (missing == 0)
            {
                question = $"💡 {a} × ? = {c}";
                correct = b.ToString();
            }
            else if (missing == 1)
            {
                question = $"💡 ? × {b} = {c}";
                correct = a.ToString();
            }
            else
            {
                question = $"💡 {a} × {b} = ?";
                correct = c.ToString();
            }

            var options = GenerateNumberOptions(int.Parse(correct), 4 + difficulty / 10, difficulty);

            return new
            {
                type = "logic",
                level = level,
                question = question,
                correctAnswer = correct,
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("logic")
            };
        }

        private object GenerateUltimate(int level, int difficulty)
        {
            // 终极混合：随机选其他类型，但时间和选项更苛刻
            var types = new[] { "text", "arithmetic", "stroke", "color", "findDifferent",
                "reverse", "missingLetter", "quickTap", "idiom", "chineseNumber",
                "caseConversion", "pinyin", "inverseColor", "mirror", "keyboard",
                "countChar", "memory", "direction", "logic" };

            string type = types[_random.Next(types.Length)];
            object challenge = null;

            switch (type)
            {
                case "text": challenge = GenerateTextRecognition(level, difficulty + 20); break;
                case "arithmetic": challenge = GenerateArithmetic(level, difficulty + 20); break;
                case "stroke": challenge = GenerateStrokeCount(level, difficulty + 20); break;
                case "color": challenge = GenerateColorRecognition(level, difficulty + 20); break;
                case "findDifferent": challenge = GenerateFindDifferent(level, difficulty + 20); break;
                case "reverse": challenge = GenerateReverseText(level, difficulty + 20); break;
                case "missingLetter": challenge = GenerateMissingLetter(level, difficulty + 20); break;
                case "quickTap": challenge = GenerateQuickTap(level, difficulty + 20); break;
                case "idiom": challenge = GenerateIdiomFill(level, difficulty + 20); break;
                case "chineseNumber": challenge = GenerateChineseToNumber(level, difficulty + 20); break;
                case "caseConversion": challenge = GenerateCaseConversion(level, difficulty + 20); break;
                case "pinyin": challenge = GeneratePinyinMatch(level, difficulty + 20); break;
                case "inverseColor": challenge = GenerateInverseColor(level, difficulty + 20); break;
                case "mirror": challenge = GenerateMirrorLetter(level, difficulty + 20); break;
                case "keyboard": challenge = GenerateKeyboardNeighbor(level, difficulty + 20); break;
                case "countChar": challenge = GenerateCharacterCount(level, difficulty + 20); break;
                case "memory": challenge = GenerateMemoryChallenge(level, difficulty + 20); break;
                case "direction": challenge = GenerateDirection(level, difficulty + 20); break;
                default: challenge = GenerateMathLogic(level, difficulty + 20); break;
            }

            var result = challenge as dynamic;
            if (result != null)
            {
                // 覆盖为终极挑战
                return new
                {
                    type = "ultimate",
                    level = level,
                    question = $"💀 终极挑战！{result.question}",
                    imageSvg = result.imageSvg,
                    displayText = result.displayText,
                    correctAnswer = result.correctAnswer,
                    options = result.options,
                    timeLimit = Math.Max(2, (result.timeLimit as int? ?? 10) - 3),
                    funMessage = GetFunMessage("ultimate")
                };
            }

            return GenerateTextRecognition(level, difficulty + 20);
        }

        // ============================================================
        // 工具方法
        // ============================================================
        private string GetFunMessage(string type)
        {
            if (FunMessages.ContainsKey(type))
            {
                var msgs = FunMessages[type];
                return msgs[_random.Next(msgs.Length)];
            }
            return "🎉 太棒了！";
        }
    }
}
