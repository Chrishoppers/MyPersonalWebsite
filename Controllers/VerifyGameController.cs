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
        // 汉字笔画数据库
        // ============================================================
        private static readonly Dictionary<char, int> StrokeCount = new()
        {
            {'一',1},{'二',2},{'三',3},{'十',2},{'人',2},{'大',3},{'上',3},{'下',3},{'口',3},{'山',3},
            {'中',4},{'日',4},{'月',4},{'水',4},{'火',4},{'木',4},{'天',4},{'不',4},{'开',4},{'心',4},
            {'五',4},{'六',4},{'文',4},{'方',4},{'王',4},{'车',4},{'龙',5},{'东',5},{'生',5},{'白',5},
            {'田',5},{'石',5},{'目',5},{'且',5},{'由',5},{'甲',5},{'申',5},{'电',5},{'回',6},{'地',6},
            {'羊',6},{'老',6},{'西',6},{'有',6},{'年',6},{'成',6},{'百',6},{'竹',6},{'米',6},{'红',6},
            {'全',6},{'合',6},{'安',6},{'字',6},{'平',5},{'左',5},{'右',5},{'兄',5},{'母',5},{'民',5},
            {'行',6},{'列',6},{'此',6},{'自',6},{'至',6},{'舟',6},{'羽',6},{'耳',6},{'虫',6},{'血',6},
            {'国',8},{'和',8},{'的',8},{'你',7},{'我',7},{'他',5},{'她',6},{'们',5},{'是',9},{'在',6},
            {'这',7},{'那',7},{'来',7},{'去',5},{'学',8},{'生',5},{'校',10},{'园',7},{'书',4},{'写',5},
            {'画',8},{'家',10},{'美',9},{'丽',7},{'春',9},{'夏',10},{'秋',9},{'冬',5},{'南',9},{'北',5},
            {'城',9},{'市',5},{'风',4},{'雨',8},{'雪',11},{'星',9},{'花',7},{'草',9},{'树',9},{'林',8},
            {'森',12},{'梅',11},{'兰',5},{'菊',11},{'荷',10},{'黄',11},{'蓝',13},{'绿',11},{'黑',12},
            {'紫',12},{'诚',8},{'信',9},{'敬',12},{'爱',10},{'善',12},{'良',7},{'猫',11},{'熊',14},
            {'象',12},{'窗',12},{'楼',13},{'桥',10},{'船',11},{'路',13},{'港',12},{'海',10},{'湖',12},
            {'流',10},{'峰',10},{'岭',8},{'谷',7},{'岩',8},{'洞',9},{'泉',9},{'溪',13},{'池',6},
            {'梦',11},{'望',11},{'数',13},{'感',13},{'解',13},{'决',6},{'游',12},{'戏',6},{'运',7},
            {'动',11},{'智',12},{'慧',15},{'暴',19},{'攀',19},{'变',19},{'魔',20},{'警',19},{'耀',20},
            {'露',21},{'霜',17},{'霞',17},{'霸',21},{'爆',19},{'蛮',23},{'湾',25},{'镶',25},{'囊',22},
            {'嚷',23},{'壤',20},{'懿',22},{'罐',23},{'噩',16},{'嚣',21},{'龘',33},{'灏',25}
        };

        // ============================================================
        // 500+ 成语库
        // ============================================================
        private static readonly string[] Idioms = new string[]
        {
            "一马当先", "龙飞凤舞", "画蛇添足", "守株待兔", "狐假虎威",
            "马到成功", "鸟语花香", "鱼目混珠", "鹤立鸡群", "龙腾虎跃",
            "画龙点睛", "亡羊补牢", "杯弓蛇影", "指鹿为马", "马不停蹄",
            "一帆风顺", "万事如意", "心想事成", "步步高升", "蒸蒸日上",
            "百花齐放", "百家争鸣", "百折不挠", "百尺竿头", "百战百胜",
            "千变万化", "千军万马", "千言万语", "千辛万苦", "千锤百炼",
            "万水千山", "万紫千红", "万马奔腾", "万家灯火", "万无一失",
            "三心二意", "三头六臂", "三顾茅庐", "三足鼎立", "三生有幸",
            "四面楚歌", "四通八达", "四海为家", "四平八稳", "四书五经",
            "五光十色", "五湖四海", "五花八门", "五颜六色", "五体投地",
            "六神无主", "六亲不认", "六六大顺", "六根清净", "六朝金粉",
            "七上八下", "七嘴八舌", "七手八脚", "七零八落", "七拼八凑",
            "八面玲珑", "八仙过海", "八斗之才", "八面威风", "八拜之交",
            "九牛一毛", "九死一生", "九霄云外", "九鼎大吕", "九九归一",
            "十全十美", "十拿九稳", "十面埋伏", "十恶不赦", "十万火急",
            "大刀阔斧", "大公无私", "大器晚成", "大智若愚", "大义灭亲",
            "高瞻远瞩", "高风亮节", "高谈阔论", "高不可攀", "高枕无忧",
            "深不可测", "深思熟虑", "深谋远虑", "深情厚谊", "深恶痛绝",
            "明察秋毫", "明辨是非", "明哲保身", "明心见性", "明知故犯",
            "春暖花开", "春意盎然", "春风化雨", "春花秋月", "春华秋实",
            "秋高气爽", "秋色宜人", "秋月春风", "秋毫无犯", "秋收冬藏",
            "冬暖夏凉", "冬日夏云", "冬裘夏葛", "冬温夏清", "冬去春来",
            "喜怒哀乐", "喜出望外", "喜从天降", "喜笑颜开", "喜上眉梢",
            "怒发冲冠", "怒不可遏", "怒火中烧", "怒气冲天", "怒形于色",
            "哀兵必胜", "哀鸿遍野", "哀痛欲绝", "哀毁骨立", "哀而不伤",
            "乐极生悲", "乐此不疲", "乐在其中", "乐而忘返", "乐善好施",
            "一鸣惊人", "一箭双雕", "一石二鸟", "一诺千金", "一视同仁",
            "一心一意", "一言九鼎", "一往无前", "一飞冲天", "一触即发",
            "安居乐业", "安步当车", "安分守己", "安贫乐道", "安身立命",
            "博览群书", "博古通今", "博学多才", "博闻强识", "博大精深",
            "才华横溢", "才高八斗", "才思敏捷", "才貌双全", "才子佳人",
            "出类拔萃", "出人头地", "出奇制胜", "出神入化", "出生入死",
            "当机立断", "当仁不让", "当务之急", "当之无愧", "当头棒喝",
            "精益求精", "精卫填海", "精忠报国", "精明强干", "精打细算",
            "气象万千", "气宇轩昂", "气势磅礴", "气贯长虹", "气吞山河",
            "神采飞扬", "神机妙算", "神出鬼没", "神通广大", "神清气爽",
            "雄心壮志", "雄才大略", "雄关漫道", "雄风犹在", "雄心勃勃",
            "壮志凌云", "壮心不已", "壮志未酬", "壮士断腕", "壮丽山河",
            "豪情壮志", "豪言壮语", "豪放不羁", "豪迈慷慨", "豪气干云",
            "乘风破浪", "乘胜追击", "乘兴而来", "乘龙快婿", "乘火打劫",
            "破釜沉舟", "破镜重圆", "破涕为笑", "破茧成蝶", "破旧立新",
            "锐不可当", "锐意进取", "锐气不减", "锐意创新", "锐敏过人",
            "志同道合", "志在四方", "志得意满", "志大才疏", "志士仁人",
            "同心协力", "同甘共苦", "同舟共济", "同病相怜", "同仇敌忾",
            "众志成城", "众望所归", "众说纷纭", "众星捧月", "众口铄金",
            "自强不息", "自告奋勇", "自食其力", "自相矛盾", "自立更生",
            "奋勇当先", "奋不顾身", "奋发图强", "奋起直追", "奋勇直前",
            "勇往直前", "勇冠三军", "勇猛精进", "勇者不惧", "勇挑重担",
            "不屈不挠", "不卑不亢", "不折不扣", "不骄不躁", "不慌不忙",
            "持之以恒", "持家有道", "持重待人", "持久之计", "持平之论",
            "坚持不懈", "坚定不移", "坚持不渝", "坚韧不拔", "坚贞不屈",
            "百炼成钢", "千磨万击", "千回百转", "万象更新", "春风得意",
            "花好月圆", "花团锦簇", "花枝招展", "花言巧语", "花红柳绿",
            "雪中送炭", "雪上加霜", "雪泥鸿爪", "雪月风花", "雪白如银",
            "风调雨顺", "风和日丽", "风起云涌", "风驰电掣", "风花雪月",
            "雨过天晴", "雨后春笋", "雨打风吹", "雨露之恩", "雨丝风片",
            "云开日出", "云蒸霞蔚", "云消雾散", "云淡风轻", "云泥之别",
            "山清水秀", "山高水长", "山明水秀", "山穷水尽", "山重水复",
            "水到渠成", "水滴石穿", "水涨船高", "水落石出", "水木清华",
            "海阔天空", "海枯石烂", "海誓山盟", "海纳百川", "海市蜃楼",
            "天翻地覆", "天南地北", "天高地厚", "天长地久", "天罗地网",
            "地大物博", "地广人稀", "地灵人杰", "地覆天翻", "地久天长",
            "人才济济", "人山人海", "人杰地灵", "人云亦云", "人定胜天",
            "心花怒放", "心旷神怡", "心平气和", "心想事成", "心满意足",
            "智勇双全", "智圆行方", "智周万物", "智珠在握", "智尽能索",
            "魑魅魍魉", "饕餮盛宴", "龙骧虎步", "凤翥鸾翔", "鸾翔凤集",
            "蜚短流长", "龙蟠虎踞", "龙肝凤髓", "凤毛麟角", "鹤唳华亭",
            "兔起鹘落", "鹰击长空", "鱼跃龙门", "虎视眈眈", "狼奔豕突",
            "獐头鼠目", "鬼斧神工", "鬼哭狼嚎", "鬼使神差", "鬼迷心窍",
            "螳臂当车", "蚍蜉撼树", "鹏程万里", "鹰扬虎视", "龙章凤姿",
            "虎背熊腰", "豹头环眼", "豺狼当道", "狼狈为奸", "狐朋狗友",
            "鸡鸣狗盗", "鼠目寸光", "牛鬼蛇神", "虎头蛇尾", "龙争虎斗",
            "鹤发童颜", "龟毛兔角", "鹤归华表", "鼠牙雀角", "牛衣对泣",
            "虎口余生", "兔死狐悲", "龙蛇混杂", "马革裹尸", "羊肠小道",
            "猴年马月", "鸡犬不宁", "狗尾续貂", "豕突狼奔", "鱼贯而入",
            "鸟尽弓藏", "兽聚鸟散", "鹿死谁手", "鹏搏九天", "鹰隼试翼",
            "龙章凤彩", "虎啸龙吟", "豹隐南山", "鸱目虎吻", "凤仪兽舞",
            "鸾鸣凤奏", "鹤唳九皋", "鹤鸣之士", "龙虎风云", "龙骧虎视",
            "龙腾豹变", "龙潜凤采", "龙吟虎啸"
        };

        // ============================================================
        // 8方向 + 组合
        // ============================================================
        private static readonly string[] Directions = new string[]
        {
            "上", "下", "左", "右", "左上", "右上", "左下", "右下",
            "正上", "正下", "正左", "正右", "斜上", "斜下", "中上", "中下"
        };

        private static readonly Dictionary<string, string> DirectionOpposites = new()
        {
            {"上", "下"}, {"下", "上"}, {"左", "右"}, {"右", "左"},
            {"左上", "右下"}, {"右上", "左下"}, {"左下", "右上"}, {"右下", "左上"},
            {"正上", "正下"}, {"正下", "正上"}, {"正左", "正右"}, {"正右", "正左"},
            {"斜上", "斜下"}, {"斜下", "斜上"}, {"中上", "中下"}, {"中下", "中上"}
        };

        // ============================================================
        // 中文数字
        // ============================================================
        private static readonly string[] ChineseNumbers = new string[]
        {
            "零","一","二","三","四","五","六","七","八","九","十",
            "十一","十二","十三","十四","十五","十六","十七","十八","十九","二十",
            "二十一","二十二","二十三","二十四","二十五","二十六","二十七","二十八","二十九","三十",
            "三十一","三十二","三十三","三十四","三十五","三十六","三十七","三十八","三十九","四十",
            "四十一","四十二","四十三","四十四","四十五","四十六","四十七","四十八","四十九","五十",
            "五十一","五十二","五十三","五十四","五十五","五十六","五十七","五十八","五十九","六十",
            "六十一","六十二","六十三","六十四","六十五","六十六","六十七","六十八","六十九","七十",
            "七十一","七十二","七十三","七十四","七十五","七十六","七十七","七十八","七十九","八十",
            "八十一","八十二","八十三","八十四","八十五","八十六","八十七","八十八","八十九","九十",
            "九十一","九十二","九十三","九十四","九十五","九十六","九十七","九十八","九十九",
            "一百","二百","三百","四百","五百","六百","七百","八百","九百","一千",
            "一千零一","一千零二","一千零三","一千零四","一千零五","一千零六","一千零七","一千零八","一千零九","一千一十",
            "一千一百","一千二百","一千三百","一千四百","一千五百","一千六百","一千七百","一千八百","一千九百","一千九百九十九",
            "两千","三千","四千","五千","六千","七千","八千","九千","九千九百九十九"
        };

        // ============================================================
        // 中文大写数字
        // ============================================================
        private static readonly Dictionary<string, string> ChineseCapital = new()
        {
            {"零","零"},{"一","壹"},{"二","贰"},{"三","叁"},{"四","肆"},{"五","伍"},
            {"六","陆"},{"七","柒"},{"八","捌"},{"九","玖"},{"十","拾"},
            {"百","佰"},{"千","仟"},{"万","万"}
        };

        // ============================================================
        // 完整字母表
        // ============================================================
        private static readonly char[] Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly char[] LowerAlphabet = "abcdefghijklmnopqrstuvwxyz".ToCharArray();

        // ============================================================
        // 趣味反馈语
        // ============================================================
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
            {"ultimate", new[]{"💀 太强了！", "🔥 终极王者！", "👑 人类之光！", "✨ 完美通关！"}}
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
                return Json(new { success = false, data = new List<object>() });
            }
        }

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
        // 挑战生成器
        // ============================================================
        private object GenerateChallenge(int level)
        {
            int typeIndex = (level - 1) % 19;
            int difficulty = level;

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
                case 9: return GenerateChineseNumber(level, difficulty);
                case 10: return GenerateCaseConversion(level, difficulty);
                case 11: return GeneratePinyinMatch(level, difficulty);
                case 12: return GenerateInverseColor(level, difficulty);
                case 13: return GenerateMirrorLetter(level, difficulty);
                case 14: return GenerateKeyboardNeighbor(level, difficulty);
                case 15: return GenerateCharacterCount(level, difficulty);
                case 16: return GenerateMemoryChallenge(level, difficulty);
                case 17: return GenerateDirection(level, difficulty);
                default: return GenerateUltimate(level, difficulty);
            }
        }

        // ============================================================
        // 1. 文字识别
        // ============================================================
        private object GenerateTextRecognition(int level, int difficulty)
        {
            int length = Math.Min(3 + difficulty / 8, 12);
            string text = "";
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*";
            for (int i = 0; i < length; i++)
            {
                text += chars[_random.Next(chars.Length)];
            }

            var svg = GenerateHardSvg(text, difficulty);
            var options = GenerateHardTextOptions(text, difficulty);
            int timeLimit = Math.Max(4, 16 - difficulty / 5);

            return new
            {
                type = "text",
                level = level,
                question = $"👁️ 识别下方文字（{length}位）",
                imageSvg = svg,
                correctAnswer = text,
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("text")
            };
        }

        // ============================================================
        // SVG 生成器
        // ============================================================
        private string GenerateHardSvg(string text, int difficulty)
        {
            int width = 360;
            int height = 120;
            int charCount = text.Length;

            var svg = new System.Text.StringBuilder();
            svg.AppendLine($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>");

            int bgR1 = _random.Next(180, 250);
            int bgG1 = _random.Next(180, 250);
            int bgB1 = _random.Next(180, 250);

            svg.AppendLine($"<defs>");
            svg.AppendLine($"<linearGradient id='bgGrad' x1='0%' y1='0%' x2='100%' y2='100%'>");
            svg.AppendLine($"<stop offset='0%' style='stop-color:rgb({bgR1},{bgG1},{bgB1});stop-opacity:1' />");
            svg.AppendLine($"<stop offset='100%' style='stop-color:rgb({bgR1 - 30},{bgG1 - 30},{bgB1 - 30});stop-opacity:1' />");
            svg.AppendLine($"</linearGradient>");

            svg.AppendLine($"<filter id='distort' x='-20%' y='-20%' width='140%' height='140%'>");
            svg.AppendLine($"<feTurbulence type='fractalNoise' baseFrequency='{0.02 + difficulty / 200f:F2}' numOctaves='3' result='noise'/>");
            svg.AppendLine($"<feDisplacementMap in='SourceGraphic' in2='noise' scale='{5 + difficulty / 3}' xChannelSelector='R' yChannelSelector='G'/>");
            svg.AppendLine($"</filter>");

            if (difficulty > 30)
            {
                float blurAmount = 0.3f + difficulty / 40f;
                svg.AppendLine($"<filter id='blur'>");
                svg.AppendLine($"<feGaussianBlur stdDeviation='{blurAmount:F1}'/>");
                svg.AppendLine($"</filter>");
            }
            svg.AppendLine($"</defs>");

            svg.AppendLine($"<rect width='{width}' height='{height}' rx='12' fill='url(#bgGrad)'/>");

            int lineCount = 80 + difficulty * 4;
            for (int i = 0; i < lineCount; i++)
            {
                int r = _random.Next(100, 230);
                int g = _random.Next(100, 230);
                int b = _random.Next(100, 230);
                int x1 = _random.Next(-50, width + 50);
                int y1 = _random.Next(-50, height + 50);
                int x2 = _random.Next(-50, width + 50);
                int y2 = _random.Next(-50, height + 50);
                int w = _random.Next(1, 5);
                double alpha = 0.15 + _random.NextDouble() * 0.5;
                svg.AppendLine($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='rgb({r},{g},{b})' stroke-width='{w}' opacity='{alpha:F2}'/>");
            }

            int curveCount = 20 + difficulty * 2;
            for (int i = 0; i < curveCount; i++)
            {
                int r = _random.Next(100, 220);
                int g = _random.Next(100, 220);
                int b = _random.Next(100, 220);
                int cx1 = _random.Next(0, width);
                int cy1 = _random.Next(0, height);
                int cx2 = _random.Next(0, width);
                int cy2 = _random.Next(0, height);
                int cx3 = _random.Next(0, width);
                int cy3 = _random.Next(0, height);
                double alpha = 0.15 + _random.NextDouble() * 0.3;
                svg.AppendLine($"<path d='M{cx1} {cy1} Q{cx2} {cy2} {cx3} {cy3}' stroke='rgb({r},{g},{b})' stroke-width='{_random.Next(1,3)}' fill='none' opacity='{alpha:F2}'/>");
            }

            int fakeCharCount = 15 + difficulty * 2;
            string allChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()_+-=<>?/";
            for (int i = 0; i < fakeCharCount; i++)
            {
                char fc = allChars[_random.Next(allChars.Length)];
                int fx = _random.Next(0, width);
                int fy = _random.Next(0, height);
                int fsize = _random.Next(8, 20);
                int fr = _random.Next(150, 220);
                int fg = _random.Next(150, 220);
                int fb = _random.Next(150, 220);
                double falpha = 0.05 + _random.NextDouble() * 0.15;
                svg.AppendLine($"<text x='{fx}' y='{fy}' font-family='Arial' font-size='{fsize}' fill='rgb({fr},{fg},{fb})' opacity='{falpha:F2}' text-anchor='middle' dominant-baseline='central'>{fc}</text>");
            }

            int dotCount = 300 + difficulty * 10;
            for (int i = 0; i < dotCount; i++)
            {
                int dx = _random.Next(0, width);
                int dy = _random.Next(0, height);
                int dr = _random.Next(100, 220);
                int dg = _random.Next(100, 220);
                int db = _random.Next(100, 220);
                int dsize = _random.Next(1, 4);
                double dalpha = 0.2 + _random.NextDouble() * 0.3;
                svg.AppendLine($"<circle cx='{dx}' cy='{dy}' r='{dsize}' fill='rgb({dr},{dg},{db})' opacity='{dalpha:F2}'/>");
            }

            int spacing = (width - 60) / charCount;
            int startX = 30;
            int colorOffset = 20 + difficulty / 3;

            for (int i = 0; i < charCount; i++)
            {
                char ch = text[i];
                int angle = _random.Next(-45, 45);
                int fontSize = _random.Next(34, 50);
                int x = startX + i * spacing + _random.Next(-10, 10);
                int y = height / 2 + 18 + _random.Next(-18, 18);

                int r = Math.Min(255, Math.Max(80, bgR1 - _random.Next(-colorOffset, colorOffset)));
                int g = Math.Min(255, Math.Max(80, bgG1 - _random.Next(-colorOffset, colorOffset)));
                int b = Math.Min(255, Math.Max(80, bgB1 - _random.Next(-colorOffset, colorOffset)));

                float scaleX = 0.7f + (float)_random.NextDouble() * 0.8f;
                float scaleY = 0.7f + (float)_random.NextDouble() * 0.8f;
                int skewX = _random.Next(-20, 20);
                int skewY = _random.Next(-10, 10);

                string[] fonts = { "Arial", "Times New Roman", "Courier New", "Georgia", "Verdana", "Comic Sans MS", "Impact" };
                string font = fonts[_random.Next(fonts.Length)];

                string stroke = "";
                if (difficulty > 20)
                {
                    int sr = Math.Min(255, Math.Max(80, r + _random.Next(-30, 30)));
                    int sg = Math.Min(255, Math.Max(80, g + _random.Next(-30, 30)));
                    int sb = Math.Min(255, Math.Max(80, b + _random.Next(-30, 30)));
                    stroke = $"stroke='rgb({sr},{sg},{sb})' stroke-width='{_random.Next(1,3)}'";
                }

                string filter = difficulty > 30 ? "filter='url(#blur)'" : "";

                svg.AppendLine($"<text x='{x}' y='{y}' font-family='{font}' font-size='{fontSize}' font-weight='{_random.Next(400, 900)}' fill='rgb({r},{g},{b})' {stroke} transform='rotate({angle} {x} {y}) scale({scaleX:F2},{scaleY:F2}) skewX({skewX}) skewY({skewY})' text-anchor='middle' dominant-baseline='central' {filter} opacity='{0.7 + _random.NextDouble() * 0.3:F2}'>{ch}</text>");
            }

            svg.AppendLine("</svg>");
            return svg.ToString();
        }

        // ============================================================
        // 干扰选项生成
        // ============================================================
        private List<string> GenerateHardTextOptions(string correct, int difficulty)
        {
            var options = new List<string> { correct };
            int count = 4 + Math.Min(difficulty / 8, 5);
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()_+-=<>?/";

            while (options.Count < count)
            {
                string fake = "";
                for (int i = 0; i < correct.Length; i++)
                {
                    if (_random.Next(100) < 30 + difficulty / 5)
                    {
                        fake += GetSimilarChar(correct[i]);
                    }
                    else
                    {
                        fake += chars[_random.Next(chars.Length)];
                    }
                }
                if (!options.Contains(fake) && fake != correct)
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
                {'0', new[]{'O','Q','D','C','8'}},
                {'O', new[]{'0','Q','D','C','8'}},
                {'Q', new[]{'0','O','D','C','8'}},
                {'D', new[]{'0','O','Q','C','8'}},
                {'C', new[]{'G','O','Q','0','8'}},
                {'G', new[]{'C','O','Q','6','8'}},
                {'1', new[]{'I','L','!','|','7'}},
                {'I', new[]{'1','L','!','|','7'}},
                {'L', new[]{'1','I','J','|','7'}},
                {'J', new[]{'L','I','T','Y','7'}},
                {'T', new[]{'Y','7','L','J','I'}},
                {'Y', new[]{'V','T','7','J','U'}},
                {'V', new[]{'Y','U','W','M','N'}},
                {'U', new[]{'V','J','L','I','W'}},
                {'W', new[]{'M','V','N','U','Y'}},
                {'M', new[]{'W','N','V','U','H'}},
                {'N', new[]{'M','H','K','W','Z'}},
                {'H', new[]{'N','K','A','M','R'}},
                {'K', new[]{'H','N','M','X','R'}},
                {'X', new[]{'K','*','+','x','H'}},
                {'A', new[]{'4','H','R','K','8'}},
                {'R', new[]{'P','A','K','B','8'}},
                {'P', new[]{'R','B','9','D','8'}},
                {'B', new[]{'8','3','P','R','6'}},
                {'3', new[]{'8','B','E','S','6'}},
                {'8', new[]{'3','B','6','S','0'}},
                {'6', new[]{'8','9','G','S','5'}},
                {'9', new[]{'6','8','P','G','5'}},
                {'S', new[]{'5','$','8','3','6'}},
                {'5', new[]{'S','$','8','3','6'}},
                {'2', new[]{'Z','7','L','I','T'}},
                {'Z', new[]{'2','7','N','M','H'}},
                {'7', new[]{'2','Z','T','Y','I'}},
                {'4', new[]{'A','H','K','R','8'}},
                {'E', new[]{'3','B','F','P','8'}},
                {'F', new[]{'E','P','T','Y','R'}},
                {'$', new[]{'S','5','8','3','6'}},
                {'!', new[]{'1','I','L','|','7'}},
                {'@', new[]{'0','O','Q','D','8'}},
                {'#', new[]{'H','K','M','N','R'}},
                {'%', new[]{'S','5','8','3','6'}},
                {'^', new[]{'V','Y','T','W','U'}},
                {'&', new[]{'8','3','B','6','S'}},
                {'*', new[]{'X','+','K','H','x'}},
                {'(', new[]{'C','G','O','Q','8'}},
                {')', new[]{'O','C','G','Q','8'}},
                {'-', new[]{'_','=','+','T','I'}},
                {'_', new[]{'-','=','+','T','I'}},
                {'=', new[]{'-','_','+','T','I'}},
                {'+', new[]{'*','X','K','H','T'}},
                {'<', new[]{'C','G','O','Q','8'}},
                {'>', new[]{'C','G','O','Q','8'}},
                {'?', new[]{'7','Z','2','T','Y'}},
                {'/', new[]{'7','Z','2','T','Y'}}
            };

            if (map.ContainsKey(c))
            {
                var similar = map[c];
                return similar[_random.Next(similar.Length)];
            }
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*";
            return chars[_random.Next(chars.Length)];
        }

        // ============================================================
        // 2. 算术
        // ============================================================
        private object GenerateArithmetic(int level, int difficulty)
        {
            int maxNum = 10 + difficulty * 5;
            int a = _random.Next(5, maxNum);
            int b = _random.Next(1, Math.Max(2, maxNum / 2));

            string[] ops = difficulty > 60 ? new[] { '+', '-', '×', '÷' } :
                           difficulty > 30 ? new[] { '+', '-', '×', '÷' } :
                           difficulty > 15 ? new[] { '+', '-', '×' } : new[] { '+', '-' };

            string op = ops[_random.Next(ops.Length)];
            int result = op == '+' ? a + b : op == '-' ? a - b : op == '×' ? a * b : a / b;
            if (result < 0) result = Math.Abs(result);
            if (result == 0) result = a + b;

            var options = GenerateNumberOptions(result, 4 + difficulty / 10);
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

        private List<string> GenerateNumberOptions(int correct, int count)
        {
            var options = new List<string> { correct.ToString() };
            int range = Math.Max(2, 5 + _random.Next(5));

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
        // 3. 汉字笔画
        // ============================================================
        private object GenerateStrokeCount(int level, int difficulty)
        {
            var allChars = StrokeCount.Keys.ToArray();
            char ch;
            if (difficulty < 20)
            {
                var simple = allChars.Where(c => StrokeCount[c] <= 5).ToArray();
                ch = simple[_random.Next(simple.Length)];
            }
            else if (difficulty < 40)
            {
                var medium = allChars.Where(c => StrokeCount[c] >= 6 && StrokeCount[c] <= 10).ToArray();
                ch = medium[_random.Next(medium.Length)];
            }
            else if (difficulty < 60)
            {
                var complex = allChars.Where(c => StrokeCount[c] >= 11 && StrokeCount[c] <= 15).ToArray();
                ch = complex[_random.Next(complex.Length)];
            }
            else if (difficulty < 80)
            {
                var ultra = allChars.Where(c => StrokeCount[c] >= 16 && StrokeCount[c] <= 20).ToArray();
                ch = ultra[_random.Next(ultra.Length)];
            }
            else
            {
                var ultimate = allChars.Where(c => StrokeCount[c] > 20).ToArray();
                ch = ultimate[_random.Next(ultimate.Length)];
            }

            int correct = StrokeCount[ch];
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
        // 4. 颜色识别
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
                new { name = "银灰", hex = "#C0C0C0" }
            };

            var pool = difficulty < 15 ? colors.Take(8).ToArray() :
                       difficulty < 30 ? colors.Take(14).ToArray() :
                       colors;

            var selected = pool[_random.Next(pool.Length)];
            var options = new List<string> { selected.name };
            var poolList = pool.ToList();
            poolList.Remove(selected);

            int count = 4 + Math.Min(difficulty / 12, 3);
            var shuffledPool = poolList.OrderBy(_ => _random.Next()).ToList();

            for (int i = 0; i < count - 1 && i < shuffledPool.Count; i++)
            {
                options.Add(shuffledPool[i].name);
            }

            int timeLimit = Math.Max(3, 10 - difficulty / 15);
            string[] texts = { "颜色", "色彩", "鲜艳", "绚丽", "斑斓" };
            string displayText = texts[_random.Next(texts.Length)];

            return new
            {
                type = "color",
                level = level,
                question = "🎨 下面文字是什么颜色？",
                displayText = $"<span style='color:{selected.hex};font-size:2.5rem;font-weight:bold;'>{displayText}</span>",
                correctAnswer = selected.name,
                options = options.OrderBy(_ => _random.Next()).ToList(),
                timeLimit = timeLimit,
                funMessage = GetFunMessage("color")
            };
        }

        // ============================================================
        // 5. 找不同
        // ============================================================
        private object GenerateFindDifferent(int level, int difficulty)
        {
            int length = 4 + difficulty / 5;
            if (length > 15) length = 15;

            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            string baseStr = "";
            for (int i = 0; i < length; i++)
            {
                baseStr += chars[_random.Next(chars.Length)];
            }

            int pos = _random.Next(length);
            char original = baseStr[pos];
            char replacement = chars[_random.Next(chars.Length)];
            while (replacement == original)
            {
                replacement = chars[_random.Next(chars.Length)];
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
                char fake = chars[_random.Next(chars.Length)];
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
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            string text = "";
            for (int i = 0; i < length; i++)
            {
                text += chars[_random.Next(chars.Length)];
            }

            string reversed = new string(text.Reverse().ToArray());
            var svg = GenerateHardSvg(text, difficulty);
            var options = GenerateHardTextOptions(reversed, difficulty);
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
        // 7. 缺失字母
        // ============================================================
        private object GenerateMissingLetter(int level, int difficulty)
        {
            int len = Math.Min(3 + difficulty / 8, 8);
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            string word = "";
            for (int i = 0; i < len; i++) word += chars[_random.Next(chars.Length)];

            int idx = _random.Next(len);
            char correct = word[idx];
            char[] display = word.ToCharArray();
            display[idx] = '_';

            var options = GenerateHardTextOptions(correct.ToString(), difficulty);
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

        // ============================================================
        // 8. 快速点击
        // ============================================================
        private object GenerateQuickTap(int level, int difficulty)
        {
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            char target = chars[_random.Next(chars.Length)];
            var options = GenerateHardTextOptions(target.ToString(), difficulty);
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

        // ============================================================
        // 9. 成语填空
        // ============================================================
        private object GenerateIdiomFill(int level, int difficulty)
        {
            string[] pool;
            if (difficulty > 70)
            {
                pool = Idioms.Where(i => i.Length == 4 && IsRareIdiom(i)).ToArray();
            }
            else if (difficulty > 40)
            {
                pool = Idioms.Where(i => i.Length == 4 && !IsCommonIdiom(i)).ToArray();
            }
            else
            {
                pool = Idioms.Where(i => i.Length == 4).ToArray();
            }

            if (pool.Length == 0) pool = Idioms.Where(i => i.Length == 4).ToArray();

            string idiom = pool[_random.Next(pool.Length)];

            int missingCount = difficulty > 50 ? 2 : 1;
            var positions = new List<int>();
            for (int i = 0; i < idiom.Length; i++)
            {
                positions.Add(i);
            }
            positions = positions.OrderBy(_ => _random.Next()).ToList();
            var missingPositions = positions.Take(missingCount).OrderBy(p => p).ToList();

            char[] display = idiom.ToCharArray();
            char[] correctChars = new char[missingCount];
            for (int i = 0; i < missingCount; i++)
            {
                int pos = missingPositions[i];
                correctChars[i] = idiom[pos];
                display[pos] = '□';
            }

            string correctAnswer = new string(correctChars);
            string displayStr = new string(display);

            var options = new List<string> { correctAnswer };
            int optionCount = 4 + Math.Min(difficulty / 10, 3);

            while (options.Count < optionCount)
            {
                string fake = "";
                for (int i = 0; i < correctAnswer.Length; i++)
                {
                    char similar = GetSimilarChar(correctAnswer[i]);
                    fake += similar;
                }
                if (!options.Contains(fake) && fake != correctAnswer)
                {
                    options.Add(fake);
                }
            }

            int timeLimit = Math.Max(4, 12 - difficulty / 10);

            return new
            {
                type = "idiom",
                level = level,
                question = $"📖 补全成语（{missingCount}个字）：{displayStr}",
                correctAnswer = correctAnswer,
                options = options.OrderBy(_ => _random.Next()).ToList(),
                timeLimit = timeLimit,
                funMessage = GetFunMessage("idiom")
            };
        }

        private bool IsCommonIdiom(string idiom)
        {
            var common = new HashSet<string> { "一马当先", "龙飞凤舞", "画蛇添足", "守株待兔", "狐假虎威",
                "马到成功", "鸟语花香", "鱼目混珠", "鹤立鸡群", "龙腾虎跃" };
            return common.Contains(idiom);
        }

        private bool IsRareIdiom(string idiom)
        {
            var rare = new HashSet<string> { "魑魅魍魉", "饕餮盛宴", "龙骧虎步", "凤翥鸾翔", "鸾翔凤集",
                "蜚短流长", "龙蟠虎踞", "龙肝凤髓", "凤毛麟角", "鹤唳华亭",
                "兔起鹘落", "鹰击长空", "鱼跃龙门", "虎视眈眈", "狼奔豕突" };
            return rare.Contains(idiom);
        }

        // ============================================================
        // 10. 中文数字
        // ============================================================
        private object GenerateChineseNumber(int level, int difficulty)
        {
            int num;
            string chinese;

            if (difficulty > 80)
            {
                num = _random.Next(1000, 9999);
                chinese = NumberToChinese(num);
            }
            else if (difficulty > 50)
            {
                num = _random.Next(100, 999);
                chinese = NumberToChinese(num);
            }
            else if (difficulty > 25)
            {
                num = _random.Next(21, 99);
                chinese = ChineseNumbers[num];
            }
            else
            {
                num = _random.Next(0, 20);
                chinese = ChineseNumbers[num];
            }

            bool useCapital = difficulty > 30 && _random.Next(100) < 40;
            string displayChinese = useCapital ? ToChineseCapital(chinese) : chinese;

            var options = new List<string> { num.ToString() };
            int optionCount = 4 + Math.Min(difficulty / 10, 3);
            int range = Math.Max(5, 20 + difficulty / 2);

            while (options.Count < optionCount)
            {
                int fake = num + _random.Next(-range, range + 1);
                if (fake < 0) fake = _random.Next(1, 20);
                if (fake > 9999) fake = num - _random.Next(1, 50);
                if (fake < 0) fake = 1;
                string str = fake.ToString();
                if (!options.Contains(str) && fake != num)
                {
                    options.Add(str);
                }
            }

            int timeLimit = Math.Max(4, 14 - difficulty / 10);

            return new
            {
                type = "chineseNumber",
                level = level,
                question = useCapital ? $"🔢 「{displayChinese}」（大写）对应的数字是？" : $"🔢 「{displayChinese}」对应的数字是？",
                correctAnswer = num.ToString(),
                options = options.OrderBy(_ => _random.Next()).ToList(),
                timeLimit = timeLimit,
                funMessage = GetFunMessage("chineseNumber")
            };
        }

        private string NumberToChinese(int num)
        {
            if (num == 0) return "零";
            if (num < 100) return ChineseNumbers[num];

            string result = "";
            int thousands = num / 1000;
            int hundreds = (num % 1000) / 100;
            int tens = (num % 100) / 10;
            int ones = num % 10;

            if (thousands > 0)
            {
                result += (thousands > 1 ? ChineseNumbers[thousands] : "") + "千";
            }
            if (hundreds > 0)
            {
                result += (hundreds > 1 ? ChineseNumbers[hundreds] : "") + "百";
                if (tens == 0 && ones > 0) result += "零";
            }
            else if (thousands > 0 && (tens > 0 || ones > 0))
            {
                result += "零";
            }
            if (tens > 0)
            {
                result += (tens > 1 ? ChineseNumbers[tens] : "") + "十";
            }
            if (ones > 0)
            {
                if (tens > 0 && ones > 0) result += ChineseNumbers[ones];
                else if (tens == 0 && hundreds > 0) result += ChineseNumbers[ones];
                else if (tens == 0 && hundreds == 0 && thousands == 0) result += ChineseNumbers[ones];
                else if (thousands > 0 || hundreds > 0) result += ChineseNumbers[ones];
                else result += ChineseNumbers[ones];
            }

            return result;
        }

        private string ToChineseCapital(string chinese)
        {
            string result = chinese;
            foreach (var kv in ChineseCapital)
            {
                result = result.Replace(kv.Key, kv.Value);
            }
            return result;
        }

        // ============================================================
        // 11. 大小写转换
        // ============================================================
        private object GenerateCaseConversion(int level, int difficulty)
        {
            int len;
            string source, correct;

            if (difficulty > 70)
            {
                len = _random.Next(5, 9);
                source = "";
                for (int i = 0; i < len; i++)
                {
                    if (_random.Next(2) == 0)
                    {
                        source += Alphabet[_random.Next(26)];
                    }
                    else
                    {
                        source += LowerAlphabet[_random.Next(26)];
                    }
                }
                int rule = _random.Next(4);
                if (rule == 0)
                {
                    correct = source.ToUpper();
                    source = source.ToLower();
                }
                else if (rule == 1)
                {
                    correct = source.ToLower();
                    source = source.ToUpper();
                }
                else if (rule == 2)
                {
                    correct = char.ToUpper(source[0]) + source.Substring(1).ToLower();
                    source = source.ToLower();
                }
                else
                {
                    correct = source.ToUpper();
                    source = source.ToLower();
                }
            }
            else if (difficulty > 40)
            {
                len = _random.Next(3, 6);
                source = "";
                for (int i = 0; i < len; i++)
                {
                    source += Alphabet[_random.Next(26)];
                }
                if (_random.Next(2) == 0)
                {
                    correct = source.ToLower();
                }
                else
                {
                    correct = source.ToUpper();
                    source = source.ToLower();
                }
            }
            else
            {
                char c = Alphabet[_random.Next(26)];
                if (_random.Next(2) == 0)
                {
                    source = c.ToString();
                    correct = c.ToString().ToLower();
                }
                else
                {
                    source = c.ToString().ToLower();
                    correct = c.ToString();
                }
            }

            var options = new List<string> { correct };
            int optionCount = 4 + Math.Min(difficulty / 10, 3);

            while (options.Count < optionCount)
            {
                string fake = "";
                for (int i = 0; i < correct.Length; i++)
                {
                    if (_random.Next(2) == 0)
                    {
                        fake += Alphabet[_random.Next(26)];
                    }
                    else
                    {
                        fake += LowerAlphabet[_random.Next(26)];
                    }
                }
                if (!options.Contains(fake) && fake != correct && fake.Length == correct.Length)
                {
                    options.Add(fake);
                }
            }

            int timeLimit = Math.Max(4, 14 - difficulty / 10);

            return new
            {
                type = "caseConversion",
                level = level,
                question = $"🔤 转换大小写：{source}",
                correctAnswer = correct,
                options = options.OrderBy(_ => _random.Next()).ToList(),
                timeLimit = timeLimit,
                funMessage = GetFunMessage("caseConversion")
            };
        }

        // ============================================================
        // 12. 读音识别
        // ============================================================
        private object GeneratePinyinMatch(int level, int difficulty)
        {
            char c = Alphabet[_random.Next(26)];
            var options = GenerateHardTextOptions(c.ToString(), difficulty);
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

        // ============================================================
        // 13. 反色识别
        // ============================================================
        private object GenerateInverseColor(int level, int difficulty)
        {
            var colors = new[] { "黑色", "白色" };
            int idx = _random.Next(2);
            string color = colors[idx];
            string bg = idx == 0 ? "#FFFFFF" : "#000000";
            string textColor = color == "黑色" ? "#000000" : "#FFFFFF";

            int timeLimit = Math.Max(3, 8 - difficulty / 15);
            string[] texts = { "颜色", "色彩", "文字" };
            string displayText = texts[_random.Next(texts.Length)];

            return new
            {
                type = "inverseColor",
                level = level,
                question = "🎨 下面文字是什么颜色？（注意背景）",
                displayText = $"<span style='color:{textColor};background:{bg};padding:0.3rem 1.5rem;border-radius:8px;font-size:2rem;font-weight:bold;'>{displayText}</span>",
                correctAnswer = color,
                options = new List<string> { "黑色", "白色" },
                timeLimit = timeLimit,
                funMessage = GetFunMessage("inverseColor")
            };
        }

        // ============================================================
        // 14. 镜像字母
        // ============================================================
      private object GenerateMirrorLetter(int level, int difficulty)
{
    char[] mirrorChars = new char[] { 
        'A','H','I','M','O','T','U','V','W','X','Y',
        'C','D','E','K','P','S','Z'
    };
    
    char c = mirrorChars[_random.Next(mirrorChars.Length)];
    var options = GenerateHardTextOptions(c.ToString(), difficulty);
    int timeLimit = Math.Max(3, 10 - difficulty / 10);

    return new
    {
        type = "mirror",
        level = level,
        question = $"🪞 字母「{c}」的镜像字母是？",
        correctAnswer = c.ToString(),
        options = options,
        timeLimit = timeLimit,
        funMessage = GetFunMessage("mirror")
    };
}

        // ============================================================
        // 15. 键盘相邻
        // ============================================================
        private object GenerateKeyboardNeighbor(int level, int difficulty)
        {
            var keys = "QWERTYUIOPASDFGHJKLZXCVBNM".ToCharArray();
            char c = keys[_random.Next(keys.Length)];

            var options = GenerateHardTextOptions(c.ToString(), difficulty);
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

        // ============================================================
        // 16. 字符计数
        // ============================================================
        private object GenerateCharacterCount(int level, int difficulty)
        {
            int len = Math.Min(6 + difficulty / 6, 15);
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            string text = "";
            for (int i = 0; i < len; i++) text += chars[_random.Next(chars.Length)];

            char target = chars[_random.Next(chars.Length)];
            int count = text.Count(c => c == target);

            var options = GenerateNumberOptions(count, 4 + difficulty / 10);
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

        // ============================================================
        // 17. 数字记忆
        // ============================================================
        private object GenerateMemoryChallenge(int level, int difficulty)
        {
            int len = Math.Min(3 + difficulty / 8, 8);
            string text = "";
            for (int i = 0; i < len; i++) text += _random.Next(0, 10);

            var options = GenerateHardTextOptions(text, difficulty);
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

        // ============================================================
        // 18. 方向判断
        // ============================================================
        private object GenerateDirection(int level, int difficulty)
        {
            string dir;
            if (difficulty > 70)
            {
                dir = Directions[_random.Next(Directions.Length)];
            }
            else if (difficulty > 40)
            {
                dir = Directions[_random.Next(8)];
            }
            else
            {
                var fourDir = new[] { "上", "下", "左", "右" };
                dir = fourDir[_random.Next(4)];
            }

            string correct = DirectionOpposites[dir];
            var allOptions = Directions.Where(d => d != dir && d != correct).ToList();
            var options = new List<string> { correct };
            int optionCount = 4 + Math.Min(difficulty / 10, 2);

            var shuffled = allOptions.OrderBy(_ => _random.Next()).ToList();
            for (int i = 0; i < Math.Min(optionCount - 1, shuffled.Count); i++)
            {
                options.Add(shuffled[i]);
            }

            int timeLimit = Math.Max(3, 8 - difficulty / 15);

            return new
            {
                type = "direction",
                level = level,
                question = $"🧭 「{dir}」的相反方向是？",
                correctAnswer = correct,
                options = options.OrderBy(_ => _random.Next()).ToList(),
                timeLimit = timeLimit,
                funMessage = GetFunMessage("direction")
            };
        }

        // ============================================================
        // 19. 终极挑战
        // ============================================================
        private object GenerateUltimate(int level, int difficulty)
        {
            var types = new[] { "text", "arithmetic", "stroke", "color", "findDifferent",
                "reverse", "missingLetter", "quickTap", "idiom", "chineseNumber",
                "caseConversion", "pinyin", "inverseColor", "mirror", "keyboard",
                "countChar", "memory", "direction" };

            string type = types[_random.Next(types.Length)];
            object challenge = null;

            switch (type)
            {
                case "text": challenge = GenerateTextRecognition(level, difficulty + 30); break;
                case "arithmetic": challenge = GenerateArithmetic(level, difficulty + 30); break;
                case "stroke": challenge = GenerateStrokeCount(level, difficulty + 30); break;
                case "color": challenge = GenerateColorRecognition(level, difficulty + 30); break;
                case "findDifferent": challenge = GenerateFindDifferent(level, difficulty + 30); break;
                case "reverse": challenge = GenerateReverseText(level, difficulty + 30); break;
                case "missingLetter": challenge = GenerateMissingLetter(level, difficulty + 30); break;
                case "quickTap": challenge = GenerateQuickTap(level, difficulty + 30); break;
                case "idiom": challenge = GenerateIdiomFill(level, difficulty + 30); break;
                case "chineseNumber": challenge = GenerateChineseNumber(level, difficulty + 30); break;
                case "caseConversion": challenge = GenerateCaseConversion(level, difficulty + 30); break;
                case "pinyin": challenge = GeneratePinyinMatch(level, difficulty + 30); break;
                case "inverseColor": challenge = GenerateInverseColor(level, difficulty + 30); break;
                case "mirror": challenge = GenerateMirrorLetter(level, difficulty + 30); break;
                case "keyboard": challenge = GenerateKeyboardNeighbor(level, difficulty + 30); break;
                case "countChar": challenge = GenerateCharacterCount(level, difficulty + 30); break;
                case "memory": challenge = GenerateMemoryChallenge(level, difficulty + 30); break;
                default: challenge = GenerateDirection(level, difficulty + 30); break;
            }

            var result = challenge as dynamic;
            if (result != null)
            {
                int timeLimit = Math.Max(2, (result.timeLimit as int? ?? 10) - 4);
                return new
                {
                    type = "ultimate",
                    level = level,
                    question = $"💀 终极挑战！{result.question}",
                    imageSvg = result.imageSvg,
                    displayText = result.displayText,
                    correctAnswer = result.correctAnswer,
                    options = result.options,
                    timeLimit = timeLimit,
                    funMessage = GetFunMessage("ultimate")
                };
            }

            return GenerateTextRecognition(level, difficulty + 30);
        }

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
