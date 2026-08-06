using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace MyPersonalWebsite.Controllers
{
    public class VerifyGameController : Controller
    {
        private readonly DataSyncService _dataSync;
        private readonly Random _random = new();
        private readonly string[] _fontFamilies;

        // ⭐ 35种颜色
        private readonly Dictionary<string, string> _colorHex = new()
        {
            {"红色", "#FF0000"}, {"深红", "#8B0000"}, {"粉色", "#FF69B4"}, {"浅粉", "#FFB6C1"},
            {"蓝色", "#0000FF"}, {"深蓝", "#00008B"}, {"天蓝", "#87CEEB"}, {"青色", "#00CED1"},
            {"绿色", "#00AA00"}, {"深绿", "#006400"}, {"草绿", "#7CFC00"}, {"黄色", "#FFD700"},
            {"金色", "#DAA520"}, {"橙色", "#FF6600"}, {"深橙", "#CC5500"}, {"紫色", "#8800CC"},
            {"深紫", "#4B0082"}, {"棕色", "#8B4513"}, {"灰色", "#808080"}, {"黑色", "#000000"},
            {"白色", "#FFFFFF"}, {"银灰", "#C0C0C0"}, {"紫罗兰", "#EE82EE"}, {"靛蓝", "#4B0082"},
            {"玫瑰红", "#FF007F"}, {"柠檬黄", "#FFF700"}, {"薄荷绿", "#98FF98"}, {"珊瑚橙", "#FF7F50"},
            {"象牙白", "#FFFFF0"}, {"巧克力棕", "#D2691E"}, {"琥珀金", "#FFBF00"}, {"翡翠绿", "#50C878"},
            {"宝石蓝", "#0F52BA"}, {"玛瑙红", "#C04000"}, {"珍珠白", "#F5F5F5"}
        };

        private readonly string[] _colorNames;
        private readonly string[] _colorWords = { "红", "蓝", "绿", "黄", "紫", "橙", "粉", "青", "棕", "灰", "黑", "白", "金", "银", "玫", "柠", "薄", "珊", "巧", "琥", "翡", "宝", "玛" };

        private readonly Dictionary<char, int> _strokeCount;
        private readonly string[] _idioms;
        private readonly string[] _directions;
        private readonly Dictionary<string, string> _directionOpposites;
        private readonly string[] _chineseNumbers;
        private readonly Dictionary<string, string> _chineseCapital;
        private readonly char[] _alphabet;
        private readonly char[] _lowerAlphabet;
        private readonly Dictionary<string, string[]> _funMessages;

        public VerifyGameController(DataSyncService dataSync)
        {
            _dataSync = dataSync;
            _fontFamilies = new[] { "Arial", "Times New Roman", "Georgia", "Verdana", "Impact", "Comic Sans MS", "Courier New", "Trebuchet MS" };
            _colorNames = _colorHex.Keys.ToArray();

            // 汉字笔画（无重复）
            _strokeCount = new Dictionary<char, int>
            {
                {'一',1},{'二',2},{'三',3},{'十',2},{'人',2},{'大',3},{'上',3},{'下',3},{'口',3},{'山',3},
                {'中',4},{'日',4},{'月',4},{'水',4},{'火',4},{'木',4},{'天',4},{'不',4},{'开',4},{'心',4},
                {'五',4},{'六',4},{'文',4},{'方',4},{'王',4},{'车',4},{'龙',5},{'东',5},{'生',5},{'白',5},
                {'田',5},{'石',5},{'目',5},{'且',5},{'由',5},{'甲',5},{'申',5},{'电',5},{'回',6},{'地',6},
                {'羊',6},{'老',6},{'西',6},{'有',6},{'年',6},{'成',6},{'百',6},{'竹',6},{'米',6},{'红',6},
                {'全',6},{'合',6},{'安',6},{'字',6},{'平',5},{'左',5},{'右',5},{'兄',5},{'母',5},{'民',5},
                {'行',6},{'列',6},{'此',6},{'自',6},{'至',6},{'舟',6},{'羽',6},{'耳',6},{'虫',6},{'血',6},
                {'国',8},{'和',8},{'的',8},{'你',7},{'我',7},{'他',5},{'她',6},{'们',5},{'是',9},{'在',6},
                {'这',7},{'那',7},{'来',7},{'去',5},{'学',8},{'校',10},{'园',7},{'书',4},{'写',5},
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

            // 500+成语
            _idioms = new string[]
            {
                "一马当先","龙飞凤舞","画蛇添足","守株待兔","狐假虎威","马到成功","鸟语花香","鱼目混珠",
                "鹤立鸡群","龙腾虎跃","画龙点睛","亡羊补牢","杯弓蛇影","指鹿为马","马不停蹄","一帆风顺",
                "万事如意","心想事成","步步高升","蒸蒸日上","百花齐放","百家争鸣","百折不挠","百尺竿头",
                "百战百胜","千变万化","千军万马","千言万语","千辛万苦","千锤百炼","万水千山","万紫千红",
                "万马奔腾","万家灯火","万无一失","三心二意","三头六臂","三顾茅庐","三足鼎立","三生有幸",
                "四面楚歌","四通八达","四海为家","四平八稳","四书五经","五光十色","五湖四海","五花八门",
                "五颜六色","五体投地","六神无主","六亲不认","六六大顺","六根清净","六朝金粉","七上八下",
                "七嘴八舌","七手八脚","七零八落","七拼八凑","八面玲珑","八仙过海","八斗之才","八面威风",
                "八拜之交","九牛一毛","九死一生","九霄云外","九鼎大吕","九九归一","十全十美","十拿九稳",
                "十面埋伏","十恶不赦","十万火急","大刀阔斧","大公无私","大器晚成","大智若愚","大义灭亲",
                "高瞻远瞩","高风亮节","高谈阔论","高不可攀","高枕无忧","深不可测","深思熟虑","深谋远虑",
                "深情厚谊","深恶痛绝","明察秋毫","明辨是非","明哲保身","明心见性","明知故犯","春暖花开",
                "春意盎然","春风化雨","春花秋月","春华秋实","秋高气爽","秋色宜人","秋月春风","秋毫无犯",
                "秋收冬藏","冬暖夏凉","冬日夏云","冬裘夏葛","冬温夏清","冬去春来","喜怒哀乐","喜出望外",
                "喜从天降","喜笑颜开","喜上眉梢","怒发冲冠","怒不可遏","怒火中烧","怒气冲天","怒形于色",
                "哀兵必胜","哀鸿遍野","哀痛欲绝","哀毁骨立","哀而不伤","乐极生悲","乐此不疲","乐在其中",
                "乐而忘返","乐善好施","一鸣惊人","一箭双雕","一石二鸟","一诺千金","一视同仁","一心一意",
                "一言九鼎","一往无前","一飞冲天","一触即发","安居乐业","安步当车","安分守己","安贫乐道",
                "安身立命","博览群书","博古通今","博学多才","博闻强识","博大精深","才华横溢","才高八斗",
                "才思敏捷","才貌双全","才子佳人","出类拔萃","出人头地","出奇制胜","出神入化","出生入死",
                "当机立断","当仁不让","当务之急","当之无愧","当头棒喝","精益求精","精卫填海","精忠报国",
                "精明强干","精打细算","气象万千","气宇轩昂","气势磅礴","气贯长虹","气吞山河","神采飞扬",
                "神机妙算","神出鬼没","神通广大","神清气爽","雄心壮志","雄才大略","雄关漫道","雄风犹在",
                "雄心勃勃","壮志凌云","壮心不已","壮志未酬","壮士断腕","壮丽山河","豪情壮志","豪言壮语",
                "豪放不羁","豪迈慷慨","豪气干云","乘风破浪","乘胜追击","乘兴而来","乘龙快婿","乘火打劫",
                "破釜沉舟","破镜重圆","破涕为笑","破茧成蝶","破旧立新","锐不可当","锐意进取","锐气不减",
                "锐意创新","锐敏过人","志同道合","志在四方","志得意满","志大才疏","志士仁人","同心协力",
                "同甘共苦","同舟共济","同病相怜","同仇敌忾","众志成城","众望所归","众说纷纭","众星捧月",
                "众口铄金","自强不息","自告奋勇","自食其力","自相矛盾","自立更生","奋勇当先","奋不顾身",
                "奋发图强","奋起直追","奋勇直前","勇往直前","勇冠三军","勇猛精进","勇者不惧","勇挑重担",
                "不屈不挠","不卑不亢","不折不扣","不骄不躁","不慌不忙","持之以恒","持家有道","持重待人",
                "持久之计","持平之论","坚持不懈","坚定不移","坚持不渝","坚韧不拔","坚贞不屈","百炼成钢",
                "千磨万击","千回百转","万象更新","春风得意","花好月圆","花团锦簇","花枝招展","花言巧语",
                "花红柳绿","雪中送炭","雪上加霜","雪泥鸿爪","雪月风花","雪白如银","风调雨顺","风和日丽",
                "风起云涌","风驰电掣","风花雪月","雨过天晴","雨后春笋","雨打风吹","雨露之恩","雨丝风片",
                "云开日出","云蒸霞蔚","云消雾散","云淡风轻","云泥之别","山清水秀","山高水长","山明水秀",
                "山穷水尽","山重水复","水到渠成","水滴石穿","水涨船高","水落石出","水木清华","海阔天空",
                "海枯石烂","海誓山盟","海纳百川","海市蜃楼","天翻地覆","天南地北","天高地厚","天长地久",
                "天罗地网","地大物博","地广人稀","地灵人杰","地覆天翻","地久天长","人才济济","人山人海",
                "人杰地灵","人云亦云","人定胜天","心花怒放","心旷神怡","心平气和","心想事成","心满意足",
                "智勇双全","智圆行方","智周万物","智珠在握","智尽能索","魑魅魍魉","饕餮盛宴","龙骧虎步",
                "凤翥鸾翔","鸾翔凤集","蜚短流长","龙蟠虎踞","龙肝凤髓","凤毛麟角","鹤唳华亭","兔起鹘落",
                "鹰击长空","鱼跃龙门","虎视眈眈","狼奔豕突","獐头鼠目","鬼斧神工","鬼哭狼嚎","鬼使神差",
                "鬼迷心窍","螳臂当车","蚍蜉撼树","鹏程万里","鹰扬虎视","龙章凤姿","虎背熊腰","豹头环眼",
                "豺狼当道","狼狈为奸","狐朋狗友","鸡鸣狗盗","鼠目寸光","牛鬼蛇神","虎头蛇尾","龙争虎斗",
                "鹤发童颜","龟毛兔角","鹤归华表","鼠牙雀角","牛衣对泣","虎口余生","兔死狐悲","龙蛇混杂",
                "马革裹尸","羊肠小道","猴年马月","鸡犬不宁","狗尾续貂","豕突狼奔","鱼贯而入","鸟尽弓藏",
                "兽聚鸟散","鹿死谁手","鹏搏九天","鹰隼试翼","龙章凤彩","虎啸龙吟","豹隐南山","鸱目虎吻",
                "凤仪兽舞","鸾鸣凤奏","鹤唳九皋","鹤鸣之士","龙虎风云","龙骧虎视","龙腾豹变","龙潜凤采"
            };

            // 24方向
            _directions = new string[]
            {
                "上","下","左","右","左上","右上","左下","右下",
                "正上","正下","正左","正右","斜上","斜下","中上","中下",
                "东北","东南","西北","西南","北","南","东","西"
            };

            _directionOpposites = new Dictionary<string, string>
            {
                {"上","下"},{"下","上"},{"左","右"},{"右","左"},
                {"左上","右下"},{"右上","左下"},{"左下","右上"},{"右下","左上"},
                {"正上","正下"},{"正下","正上"},{"正左","正右"},{"正右","正左"},
                {"斜上","斜下"},{"斜下","斜上"},{"中上","中下"},{"中下","中上"},
                {"东北","西南"},{"东南","西北"},{"西北","东南"},{"西南","东北"},
                {"北","南"},{"南","北"},{"东","西"},{"西","东"}
            };

            _chineseNumbers = new string[]
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

            _chineseCapital = new Dictionary<string, string>
            {
                {"零","零"},{"一","壹"},{"二","贰"},{"三","叁"},{"四","肆"},{"五","伍"},
                {"六","陆"},{"七","柒"},{"八","捌"},{"九","玖"},{"十","拾"},
                {"百","佰"},{"千","仟"},{"万","万"}
            };

            _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            _lowerAlphabet = "abcdefghijklmnopqrstuvwxyz".ToCharArray();

            _funMessages = new Dictionary<string, string[]>
            {
                {"text", new[]{"👁️ 神级视力！", "🔍 显微镜级！", "🎯 精准狙击！"}},
                {"arithmetic", new[]{"🧮 人形计算器！", "💡 爱因斯坦！", "🤓 数学之神！"}},
                {"stroke", new[]{"📝 文字学家！", "✍️ 汉字活字典！", "🏯 甲骨文专家！"}},
                {"color", new[]{"🎨 色彩之神！", "🌈 火眼金睛！", "✨ 审美大师！"}},
                {"findDifferent", new[]{"🔍 人形扫描仪！", "🎯 鹰眼！", "👀 像素级观察！"}},
                {"reverse", new[]{"🔄 人形反转器！", "🧠 超脑！", "💪 空间掌控者！"}},
                {"missingLetter", new[]{"🔤 人形词典！", "📚 词汇之王！", "✍️ 拼写之神！"}},
                {"quickTap", new[]{"⚡ 闪电侠！", "💨 光速反应！", "🔥 人形电竞！"}},
                {"idiom", new[]{"📖 成语活字典！", "🏯 国学大师！", "✍️ 文学宗师！"}},
                {"chineseNumber", new[]{"🔢 人形计算器！", "🧮 数学之神！", "💡 数字天才！"}},
                {"caseConversion", new[]{"🔤 字母之神！", "📚 语言学家！", "✍️ 拼写大师！"}},
                {"pinyin", new[]{"🔊 语音之神！", "🎙️ 播音级！", "📢 朗读大师！"}},
                {"inverseColor", new[]{"🎨 视觉之神！", "🌈 火眼金睛！", "✨ 色彩大师！"}},
                {"mirror", new[]{"🪞 空间之神！", "🧠 超脑！", "💪 逻辑之王！"}},
                {"keyboard", new[]{"⌨️ 键盘之神！", "💨 光速打字！", "🔥 电竞级！"}},
                {"countChar", new[]{"🔢 人形计数器！", "🧮 数学之神！", "💡 逻辑天才！"}},
                {"memory", new[]{"🧠 照相机记忆！", "💪 超脑！", "✨ 过目不忘！"}},
                {"direction", new[]{"🧭 人形指南针！", "🗺️ 活地图！", "✨ 方向感之神！"}},
                {"logic", new[]{"💡 逻辑之神！", "🧠 超脑！", "✨ 推理之王！"}},
                {"tripleColor", new[]{"🎯 三重干扰通关！", "🌈 视觉之神！", "✨ 不是人类！"}},
                {"ultimate", new[]{"💀 超越人类！", "🔥 终极王者！", "👑 神级存在！"}}
            };
        }

        [HttpGet]
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            return View();
        }

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
        // ⭐ 20种类型生成器
        // ============================================================
        private object GenerateChallenge(int level)
        {
            int typeIndex = (level - 1) % 20;
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
                case 18: return GenerateMathLogic(level, difficulty);
                default: return GenerateTripleColorInterference(level, difficulty);
            }
        }

        // ============================================================
        // 1. 文字识别（地狱难度）
        // ============================================================
        private object GenerateTextRecognition(int level, int difficulty)
        {
            int length = Math.Min(4 + difficulty / 5, 14);
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()_+-=<>?/";
            string text = "";
            for (int i = 0; i < length; i++) text += chars[_random.Next(chars.Length)];

            int lineCount = 60 + difficulty * 4;
            int distortion = 10 + difficulty / 2;
            var svg = GenerateSuperHardSvg(text, distortion, lineCount, difficulty);

            var options = GenerateSuperHardOptions(text, difficulty);
            int timeLimit = Math.Max(4, 16 - difficulty / 4);

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

        private string GenerateSuperHardSvg(string text, int distortion, int lineCount, int difficulty)
        {
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
            svg.AppendLine($"<feTurbulence type='fractalNoise' baseFrequency='{0.03 + difficulty / 500f:F2}' numOctaves='3' result='noise'/>");
            svg.AppendLine($"<feDisplacementMap in='SourceGraphic' in2='noise' scale='{8 + difficulty / 3}' xChannelSelector='R' yChannelSelector='G'/>");
            svg.AppendLine($"</filter>");

            if (difficulty > 30)
            {
                float blur = 0.3f + difficulty / 40f;
                svg.AppendLine($"<filter id='blur'><feGaussianBlur stdDeviation='{blur:F1}'/></filter>");
            }
            svg.AppendLine($"</defs>");

            svg.AppendLine($"<rect width='{width}' height='{height}' rx='10' fill='rgb({bgR},{bgG},{bgB})'/>");

            // 干扰线
            for (int i = 0; i < lineCount; i++)
            {
                int r = _random.Next(80, 230);
                int g = _random.Next(80, 230);
                int b = _random.Next(80, 230);
                svg.AppendLine($"<line x1='{_random.Next(-50,width+50)}' y1='{_random.Next(-50,height+50)}' x2='{_random.Next(-50,width+50)}' y2='{_random.Next(-50,height+50)}' stroke='rgb({r},{g},{b})' stroke-width='{_random.Next(1,4)}' opacity='{0.1 + _random.NextDouble() * 0.5:F2}'/>");
            }

            // 曲线干扰
            for (int i = 0; i < lineCount / 2; i++)
            {
                int r = _random.Next(80, 220);
                int g = _random.Next(80, 220);
                int b = _random.Next(80, 220);
                svg.AppendLine($"<path d='M{_random.Next(0,width)} {_random.Next(0,height)} Q{_random.Next(0,width)} {_random.Next(0,height)} {_random.Next(0,width)} {_random.Next(0,height)}' stroke='rgb({r},{g},{b})' stroke-width='{_random.Next(1,3)}' fill='none' opacity='{0.1 + _random.NextDouble() * 0.3:F2}'/>");
            }

            // 干扰字符
            string allChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()_+-=<>?/";
            for (int i = 0; i < 40 + difficulty * 2; i++)
            {
                char fc = allChars[_random.Next(allChars.Length)];
                svg.AppendLine($"<text x='{_random.Next(0,width)}' y='{_random.Next(0,height)}' font-family='Arial' font-size='{_random.Next(10,25)}' fill='rgb({_random.Next(150,220)},{_random.Next(150,220)},{_random.Next(150,220)})' opacity='{0.03 + _random.NextDouble() * 0.1:F2}' text-anchor='middle' dominant-baseline='central'>{fc}</text>");
            }

            // 噪点
            for (int i = 0; i < 400 + difficulty * 15; i++)
            {
                svg.AppendLine($"<circle cx='{_random.Next(0,width)}' cy='{_random.Next(0,height)}' r='{_random.Next(1,4)}' fill='rgb({_random.Next(100,220)},{_random.Next(100,220)},{_random.Next(100,220)})' opacity='{0.1 + _random.NextDouble() * 0.3:F2}'/>");
            }

            // 主字符
            int spacing = (width - 60) / charCount;
            int startX = 30;

            for (int i = 0; i < charCount; i++)
            {
                char ch = text[i];
                int angle = _random.Next(-55, 55);
                int fontSize = _random.Next(32, 52);
                int x = startX + i * spacing + _random.Next(-12, 12);
                int y = height / 2 + 18 + _random.Next(-20, 20);

                int colorOffset = 20 + difficulty / 2;
                int r = Math.Min(255, Math.Max(50, bgR - _random.Next(-colorOffset, colorOffset)));
                int g = Math.Min(255, Math.Max(50, bgG - _random.Next(-colorOffset, colorOffset)));
                int b = Math.Min(255, Math.Max(50, bgB - _random.Next(-colorOffset, colorOffset)));

                float scaleX = 0.6f + (float)_random.NextDouble() * 1.0f;
                float scaleY = 0.6f + (float)_random.NextDouble() * 1.0f;
                int skewX = _random.Next(-25, 25);

                string font = _fontFamilies[_random.Next(_fontFamilies.Length)];
                string filter = difficulty > 30 ? "filter='url(#blur)'" : "";

                svg.AppendLine($"<text x='{x}' y='{y}' font-family='{font}' font-size='{fontSize}' font-weight='{_random.Next(400,900)}' fill='rgb({r},{g},{b})' transform='rotate({angle} {x} {y}) scale({scaleX:F2},{scaleY:F2}) skewX({skewX})' text-anchor='middle' dominant-baseline='central' {filter} opacity='{0.7 + _random.NextDouble() * 0.3:F2}'>{ch}</text>");
            }

            svg.AppendLine("</svg>");
            return svg.ToString();
        }

        private List<string> GenerateSuperHardOptions(string correct, int difficulty)
        {
            var options = new List<string> { correct };
            int count = 4 + Math.Min(difficulty / 8, 4);
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

        // ============================================================
        // 2. 算术（地狱难度 - 大数+混合运算）
        // ============================================================
        private object GenerateArithmetic(int level, int difficulty)
        {
            int maxNum = 20 + difficulty * 8;
            int a = _random.Next(10, maxNum);
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

            var options = GenerateSuperNumberOptions(result, 4 + difficulty / 10);
            int timeLimit = Math.Max(3, 12 - difficulty / 6);

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

        private List<string> GenerateSuperNumberOptions(int correct, int count)
        {
            var options = new List<string> { correct.ToString() };
            int range = Math.Max(10, 25);

            while (options.Count < count)
            {
                int fake = correct + _random.Next(-range, range + 1);
                if (fake < 0) fake = _random.Next(1, 50);
                string str = fake.ToString();
                if (!options.Contains(str) && fake != correct)
                {
                    options.Add(str);
                }
            }
            return options.OrderBy(_ => _random.Next()).ToList();
        }

        // ============================================================
        // 3. 汉字笔画（地狱难度 - 生僻字）
        // ============================================================
        private object GenerateStrokeCount(int level, int difficulty)
        {
            var allChars = _strokeCount.Keys.ToArray();
            char ch;

            if (difficulty < 20)
            {
                var simple = allChars.Where(c => _strokeCount[c] <= 5).ToArray();
                ch = simple[_random.Next(simple.Length)];
            }
            else if (difficulty < 40)
            {
                var medium = allChars.Where(c => _strokeCount[c] >= 6 && _strokeCount[c] <= 10).ToArray();
                ch = medium[_random.Next(medium.Length)];
            }
            else if (difficulty < 60)
            {
                var complex = allChars.Where(c => _strokeCount[c] >= 11 && _strokeCount[c] <= 15).ToArray();
                ch = complex[_random.Next(complex.Length)];
            }
            else if (difficulty < 80)
            {
                var ultra = allChars.Where(c => _strokeCount[c] >= 16 && _strokeCount[c] <= 20).ToArray();
                ch = ultra[_random.Next(ultra.Length)];
            }
            else
            {
                var ultimate = allChars.Where(c => _strokeCount[c] > 20).ToArray();
                ch = ultimate[_random.Next(ultimate.Length)];
            }

            int correct = _strokeCount[ch];
            var options = new List<string> { correct.ToString() };
            int count = 4 + Math.Min(difficulty / 12, 4);

            while (options.Count < count)
            {
                int fake = correct + _random.Next(-5, 6);
                if (fake < 1) fake = correct + _random.Next(2, 7);
                if (fake > 35) fake = correct - _random.Next(2, 7);
                if (fake < 1) fake = 3 + _random.Next(1, 7);
                string str = fake.ToString();
                if (!options.Contains(str) && fake != correct)
                {
                    options.Add(str);
                }
            }

            int timeLimit = Math.Max(3, 12 - difficulty / 8);

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
        // 4. ⭐ 颜色识别（修复：使用 displayText）
        // ============================================================
       // 4. ⭐ 颜色识别（修复：用单字颜色词）
private object GenerateColorRecognition(int level, int difficulty)
{
    var pool = _colorHex.ToArray();
    var selected = pool[_random.Next(pool.Length)];

    // 找相似颜色
    var similarColors = pool.Where(c => IsSimilarColor(c.Value, selected.Value)).ToList();
    if (similarColors.Count < 3) similarColors = pool.ToList();

    var options = new List<string> { selected.Key };
    int count = 4 + Math.Min(difficulty / 10, 4);

    var poolList = similarColors.Where(c => c.Key != selected.Key).ToList();
    for (int i = 0; i < count - 1 && i < poolList.Count; i++)
    {
        options.Add(poolList[i].Key);
    }

    int timeLimit = Math.Max(2, 7 - difficulty / 15);
    
    // ⭐ 只用单字颜色词
    string[] singleColorWords = { "红", "蓝", "绿", "黄", "紫", "橙", "粉", "青", "棕", "灰", "黑", "白", "金", "银" };
    string displayWord = singleColorWords[_random.Next(singleColorWords.Length)];

    return new
    {
        type = "color",
        level = level,
        question = $"🎨 下面文字是什么颜色？",
        displayText = $"<span style='color:{selected.Value};font-size:3rem;font-weight:bold;'>{displayWord}</span>",
        correctAnswer = selected.Key,
        options = options.OrderBy(_ => _random.Next()).ToList(),
        timeLimit = timeLimit,
        funMessage = GetFunMessage("color")
    };
}

      private object GenerateFindDifferent(int level, int difficulty)
{
    int length = 5 + difficulty / 4;
    if (length > 12) length = 12;

    string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    
    string original = "";
    for (int i = 0; i < length; i++) 
        original += chars[_random.Next(chars.Length)];

    int pos = _random.Next(length);
    char originalChar = original[pos];
    
    // ⭐ 修复：用 GetSimilarChar
    char replacementChar = GetSimilarChar(originalChar);
    while (replacementChar == originalChar) 
        replacementChar = GetSimilarChar(originalChar);

    char[] replacedArr = original.ToCharArray();
    replacedArr[pos] = replacementChar;
    string replaced = new string(replacedArr);

    char[] shuffledArr = replaced.ToCharArray();
    for (int i = shuffledArr.Length - 1; i > 0; i--)
    {
        int j = _random.Next(i + 1);
        char temp = shuffledArr[i];
        shuffledArr[i] = shuffledArr[j];
        shuffledArr[j] = temp;
    }
    string shuffled = new string(shuffledArr);

    int displayTime = Math.Max(2, 8 - difficulty / 8);

    var options = new List<string> { originalChar.ToString() };
    int count = 4 + Math.Min(difficulty / 12, 3);

    while (options.Count < count)
    {
        char fake = chars[_random.Next(chars.Length)];
        if (fake != originalChar && !options.Contains(fake.ToString()))
        {
            options.Add(fake.ToString());
        }
    }

    int timeLimit = Math.Max(4, 14 - difficulty / 6);
    string diffLabel = difficulty > 70 ? "💀 地狱" : difficulty > 40 ? "🔥 噩梦" : "⚡ 困难";

    return new
    {
        type = "findDifferent",
        level = level,
        question = $"🔍 记住下面的字符，然后找出被更改的那个！（{diffLabel}）",
        originalDisplay = original,
        displayTime = displayTime,
        shuffledDisplay = shuffled,
        correctAnswer = originalChar.ToString(),
        options = options.OrderBy(_ => _random.Next()).ToList(),
        timeLimit = timeLimit,
        funMessage = GetFunMessage("findDifferent")
    };
}
        // ============================================================
        // 6. 倒序识别（地狱难度 - 10位+干扰）
        // ============================================================
        private object GenerateReverseText(int level, int difficulty)
        {
            int length = Math.Min(5 + difficulty / 4, 12);
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()_+-=<>?/";
            string text = "";
            for (int i = 0; i < length; i++) text += chars[_random.Next(chars.Length)];

            string reversed = new string(text.Reverse().ToArray());

            int lineCount = 60 + difficulty * 4;
            int distortion = 10 + difficulty / 2;
            var svg = GenerateSuperHardSvg(text, distortion, lineCount, difficulty);

            var options = GenerateSuperHardOptions(reversed, difficulty);
            int timeLimit = Math.Max(3, 12 - difficulty / 4);

            return new
            {
                type = "reverse",
                level = level,
                question = $"🔄 图片中的文字是什么？（倒过来了）",
                imageSvg = svg,
                correctAnswer = reversed,
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("reverse")
            };
        }

        // ============================================================
        // 7. 缺失字母（地狱难度 - 缺2-3个）
        // ============================================================
        private object GenerateMissingLetter(int level, int difficulty)
        {
            int len = Math.Min(5 + difficulty / 4, 12);
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()_+-=<>?/";
            string word = "";
            for (int i = 0; i < len; i++) word += chars[_random.Next(chars.Length)];

            int missingCount = difficulty > 70 ? 3 : difficulty > 40 ? 2 : 1;
            var positions = new List<int>();
            for (int i = 0; i < word.Length; i++) positions.Add(i);
            positions = positions.OrderBy(_ => _random.Next()).ToList();
            var missingPos = positions.Take(missingCount).OrderBy(p => p).ToList();

            char[] display = word.ToCharArray();
            char[] correctChars = new char[missingCount];

            for (int i = 0; i < missingCount; i++)
            {
                int pos = missingPos[i];
                correctChars[i] = word[pos];
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
                    fake += chars[_random.Next(chars.Length)];
                }
                if (!options.Contains(fake) && fake != correctAnswer)
                {
                    options.Add(fake);
                }
            }

            int timeLimit = Math.Max(3, 10 - difficulty / 6);

            return new
            {
                type = "missingLetter",
                level = level,
                question = $"🔤 补全（缺{missingCount}个）：{displayStr}",
                correctAnswer = correctAnswer,
                options = options.OrderBy(_ => _random.Next()).ToList(),
                timeLimit = timeLimit,
                funMessage = GetFunMessage("missingLetter")
            };
        }

        // ============================================================
        // 8. 快速点击（地狱难度 - 时间极短）
        // ============================================================
        private object GenerateQuickTap(int level, int difficulty)
        {
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()_+-=<>?/";
            char target = chars[_random.Next(chars.Length)];
            var options = GenerateSuperHardOptions(target.ToString(), difficulty);
            int timeLimit = Math.Max(2, 8 - difficulty / 10);

            return new
            {
                type = "quickTap",
                level = level,
                question = $"⚡ 快速找到目标字符！",
                correctAnswer = target.ToString(),
                options = options,
                timeLimit = timeLimit,
                funMessage = GetFunMessage("quickTap")
            };
        }

        // ============================================================
        // 9. 成语填空（地狱难度 - 缺3个字+生僻成语）
        // ============================================================
        private object GenerateIdiomFill(int level, int difficulty)
        {
            string[] pool;
            if (difficulty > 70)
            {
                pool = _idioms.Where(i => i.Length == 4 && IsRareIdiom(i)).ToArray();
            }
            else if (difficulty > 40)
            {
                pool = _idioms.Where(i => i.Length == 4 && !IsCommonIdiom(i)).ToArray();
            }
            else
            {
                pool = _idioms.Where(i => i.Length == 4).ToArray();
            }

            if (pool.Length == 0) pool = _idioms.Where(i => i.Length == 4).ToArray();

            string idiom = pool[_random.Next(pool.Length)];

            int missingCount = difficulty > 70 ? 3 : difficulty > 40 ? 2 : 1;
            var positions = new List<int>();
            for (int i = 0; i < idiom.Length; i++) positions.Add(i);
            positions = positions.OrderBy(_ => _random.Next()).ToList();
            var missingPos = positions.Take(missingCount).OrderBy(p => p).ToList();

            char[] display = idiom.ToCharArray();
            char[] correctChars = new char[missingCount];

            for (int i = 0; i < missingCount; i++)
            {
                int pos = missingPos[i];
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
                    fake += GetSuperSimilarChar(correctAnswer[i]);
                }
                if (!options.Contains(fake) && fake != correctAnswer)
                {
                    options.Add(fake);
                }
            }

            int timeLimit = Math.Max(3, 10 - difficulty / 8);

            return new
            {
                type = "idiom",
                level = level,
                question = $"📖 补全成语（缺{missingCount}个字）：{displayStr}",
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
        // 10. 中文数字（地狱难度 - 大写+大数）
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
                chinese = _chineseNumbers[num];
            }
            else
            {
                num = _random.Next(0, 20);
                chinese = _chineseNumbers[num];
            }

            bool useCapital = difficulty > 30 && _random.Next(100) < 50;
            string displayChinese = useCapital ? ToChineseCapital(chinese) : chinese;

            var options = new List<string> { num.ToString() };
            int optionCount = 4 + Math.Min(difficulty / 10, 3);
            int range = Math.Max(10, 50 + difficulty);

            while (options.Count < optionCount)
            {
                int fake = num + _random.Next(-range, range + 1);
                if (fake < 0) fake = _random.Next(1, 50);
                if (fake > 9999) fake = num - _random.Next(1, 50);
                if (fake < 0) fake = 1;
                string str = fake.ToString();
                if (!options.Contains(str) && fake != num)
                {
                    options.Add(str);
                }
            }

            int timeLimit = Math.Max(3, 12 - difficulty / 8);

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
            if (num < 100) return _chineseNumbers[num];

            string result = "";
            int thousands = num / 1000;
            int hundreds = (num % 1000) / 100;
            int tens = (num % 100) / 10;
            int ones = num % 10;

            if (thousands > 0)
            {
                result += (thousands > 1 ? _chineseNumbers[thousands] : "") + "千";
            }
            if (hundreds > 0)
            {
                result += (hundreds > 1 ? _chineseNumbers[hundreds] : "") + "百";
                if (tens == 0 && ones > 0) result += "零";
            }
            else if (thousands > 0 && (tens > 0 || ones > 0))
            {
                result += "零";
            }
            if (tens > 0)
            {
                result += (tens > 1 ? _chineseNumbers[tens] : "") + "十";
            }
            if (ones > 0)
            {
                if (tens > 0 && ones > 0) result += _chineseNumbers[ones];
                else if (tens == 0 && hundreds > 0) result += _chineseNumbers[ones];
                else if (tens == 0 && hundreds == 0 && thousands == 0) result += _chineseNumbers[ones];
                else if (thousands > 0 || hundreds > 0) result += _chineseNumbers[ones];
                else result += _chineseNumbers[ones];
            }

            return result;
        }

        private string ToChineseCapital(string chinese)
        {
            string result = chinese;
            foreach (var kv in _chineseCapital)
            {
                result = result.Replace(kv.Key, kv.Value);
            }
            return result;
        }

        // ============================================================
        // 11. 大小写转换（地狱难度 - 混合大小写+规则随机）
        // ============================================================
        private object GenerateCaseConversion(int level, int difficulty)
        {
            int len;
            string source, correct;

            if (difficulty > 70)
            {
                len = _random.Next(6, 12);
                source = "";
                for (int i = 0; i < len; i++)
                {
                    source += _random.Next(2) == 0 ? _alphabet[_random.Next(26)] : _lowerAlphabet[_random.Next(26)];
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
                    var words = source.Split(' ');
                    var result = "";
                    foreach (var w in words)
                    {
                        if (w.Length > 0)
                            result += char.ToUpper(w[0]) + w.Substring(1).ToLower() + " ";
                    }
                    correct = result.Trim();
                    source = source.ToUpper();
                }
            }
            else if (difficulty > 40)
            {
                len = _random.Next(4, 8);
                source = "";
                for (int i = 0; i < len; i++) source += _alphabet[_random.Next(26)];
                correct = _random.Next(2) == 0 ? source.ToLower() : source.ToUpper();
                source = _random.Next(2) == 0 ? source.ToLower() : source.ToUpper();
            }
            else
            {
                char c = _alphabet[_random.Next(26)];
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
                    fake += _random.Next(2) == 0 ? _alphabet[_random.Next(26)] : _lowerAlphabet[_random.Next(26)];
                }
                if (!options.Contains(fake) && fake != correct && fake.Length == correct.Length)
                {
                    options.Add(fake);
                }
            }

            int timeLimit = Math.Max(3, 12 - difficulty / 8);

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
        // 12. 读音识别（地狱难度）
        // ============================================================
        private object GeneratePinyinMatch(int level, int difficulty)
        {
            char c = _alphabet[_random.Next(26)];
            var options = GenerateSuperHardOptions(c.ToString(), difficulty);
            int timeLimit = Math.Max(2, 8 - difficulty / 10);

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
        // 13. 反色识别（地狱难度 - 3色）
        // ============================================================
        private object GenerateInverseColor(int level, int difficulty)
        {
            var colors = new[] { "黑色", "白色", "灰色" };
            int idx = _random.Next(3);
            string color = colors[idx];

            string bg = color == "黑色" ? "#FFFFFF" : color == "白色" ? "#000000" : "#808080";
            string textColor = color == "黑色" ? "#000000" : color == "白色" ? "#FFFFFF" : "#404040";

            int timeLimit = Math.Max(2, 6 - difficulty / 15);
            string[] texts = { "颜色", "色彩", "文字" };
            string displayText = texts[_random.Next(texts.Length)];

            return new
            {
                type = "inverseColor",
                level = level,
                question = $"🎨 下面文字是什么颜色？（注意背景）",
                displayText = $"<span style='color:{textColor};background:{bg};padding:0.3rem 1.5rem;border-radius:8px;font-size:2rem;font-weight:bold;'>{displayText}</span>",
                correctAnswer = color,
                options = new List<string> { "黑色", "白色", "灰色" },
                timeLimit = timeLimit,
                funMessage = GetFunMessage("inverseColor")
            };
        }

        // ============================================================
        // 14. 镜像字母（地狱难度）
        // ============================================================
        private object GenerateMirrorLetter(int level, int difficulty)
        {
            char[] mirrorKeys = new char[] {
                'A','H','I','M','O','T','U','V','W','X','Y',
                'C','D','E','K','P','S','Z'
            };
            char c = mirrorKeys[_random.Next(mirrorKeys.Length)];
            var options = GenerateSuperHardOptions(c.ToString(), difficulty);
            int timeLimit = Math.Max(2, 8 - difficulty / 10);

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
        // 15. 键盘相邻（地狱难度）
        // ============================================================
        private object GenerateKeyboardNeighbor(int level, int difficulty)
        {
            var keys = "QWERTYUIOPASDFGHJKLZXCVBNM".ToCharArray();
            char c = keys[_random.Next(keys.Length)];

            var options = GenerateSuperHardOptions(c.ToString(), difficulty);
            int timeLimit = Math.Max(2, 8 - difficulty / 10);

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
        // 16. 字符计数（地狱难度 - 长字符串）
        // ============================================================
        private object GenerateCharacterCount(int level, int difficulty)
        {
            int len = Math.Min(8 + difficulty / 4, 25);
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()_+-=<>?/";
            string text = "";
            for (int i = 0; i < len; i++) text += chars[_random.Next(chars.Length)];

            char target = chars[_random.Next(chars.Length)];
            int count = text.Count(c => c == target);

            var options = GenerateSuperNumberOptions(count, 4 + difficulty / 10);
            int timeLimit = Math.Max(3, 10 - difficulty / 6);

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
        // 17. 数字记忆（地狱难度 - 14位）
        // ============================================================
        private object GenerateMemoryChallenge(int level, int difficulty)
        {
            int len = Math.Min(5 + difficulty / 4, 16);
            string text = "";
            for (int i = 0; i < len; i++) text += _random.Next(0, 10);

            var options = new List<string> { text };
            int optionCount = 4 + Math.Min(difficulty / 10, 3);

            while (options.Count < optionCount)
            {
                string fake = "";
                for (int i = 0; i < len; i++) fake += _random.Next(0, 10);
                if (!options.Contains(fake) && fake != text)
                {
                    options.Add(fake);
                }
            }

            int timeLimit = Math.Max(4, 16 - difficulty / 5);

            return new
            {
                type = "memory",
                level = level,
                question = $"🧠 记住这个数字：{text}",
                displayNumber = text,
                correctAnswer = text,
                options = options.OrderBy(_ => _random.Next()).ToList(),
                timeLimit = timeLimit,
                funMessage = GetFunMessage("memory")
            };
        }

        // ============================================================
        // 18. 方向判断（地狱难度 - 24方向）
        // ============================================================
        private object GenerateDirection(int level, int difficulty)
        {
            string dir;
            if (difficulty > 70)
            {
                dir = _directions[_random.Next(_directions.Length)];
            }
            else if (difficulty > 40)
            {
                var sixteenDir = new[] { "上", "下", "左", "右", "左上", "右上", "左下", "右下",
                                        "正上", "正下", "正左", "正右", "斜上", "斜下", "中上", "中下" };
                dir = sixteenDir[_random.Next(sixteenDir.Length)];
            }
            else
            {
                var eightDir = new[] { "上", "下", "左", "右", "左上", "右上", "左下", "右下" };
                dir = eightDir[_random.Next(eightDir.Length)];
            }

            string correct = _directionOpposites[dir];

            var options = new List<string> { correct };
            int optionCount = 4 + Math.Min(difficulty / 10, 3);

            var pool = _directions.Where(d => d != dir && d != correct).ToList();
            var shuffled = pool.OrderBy(_ => _random.Next()).ToList();

            for (int i = 0; i < Math.Min(optionCount - 1, shuffled.Count); i++)
            {
                options.Add(shuffled[i]);
            }

            int timeLimit = Math.Max(2, 7 - difficulty / 15);

            return new
            {
                type = "direction",
                level = level,
                question = $"🧭 请选择「{dir}」的相反方向",
                correctAnswer = correct,
                options = options.OrderBy(_ => _random.Next()).ToList(),
                timeLimit = timeLimit,
                funMessage = GetFunMessage("direction")
            };
        }

        // ============================================================
        // 19. 逻辑推理（地狱难度 - 三步运算）
        // ============================================================
        private object GenerateMathLogic(int level, int difficulty)
        {
            int a, b, c, missing;
            string question, correct;
            int timeLimit = Math.Max(3, 10 - difficulty / 6);

            if (difficulty > 70)
            {
                a = _random.Next(2, 12);
                b = _random.Next(2, 12);
                c = _random.Next(1, 25);
                int result = a * b + c;
                missing = _random.Next(3);
                if (missing == 0)
                {
                    question = $"💡 {a} × {b} + ? = {result}";
                    correct = c.ToString();
                }
                else if (missing == 1)
                {
                    question = $"💡 {a} × ? + {c} = {result}";
                    correct = b.ToString();
                }
                else
                {
                    question = $"💡 ? × {b} + {c} = {result}";
                    correct = a.ToString();
                }
            }
            else if (difficulty > 40)
            {
                a = _random.Next(2, 18);
                b = _random.Next(2, 18);
                int result = a * b;
                missing = _random.Next(3);
                if (missing == 0)
                {
                    question = $"💡 {a} × ? = {result}";
                    correct = b.ToString();
                }
                else if (missing == 1)
                {
                    question = $"💡 ? × {b} = {result}";
                    correct = a.ToString();
                }
                else
                {
                    question = $"💡 {a} × {b} = ?";
                    correct = result.ToString();
                }
            }
            else
            {
                a = _random.Next(10, 80);
                b = _random.Next(10, 80);
                int result = a + b;
                missing = _random.Next(3);
                if (missing == 0)
                {
                    question = $"💡 {a} + ? = {result}";
                    correct = b.ToString();
                }
                else if (missing == 1)
                {
                    question = $"💡 ? + {b} = {result}";
                    correct = a.ToString();
                }
                else
                {
                    question = $"💡 {a} + {b} = ?";
                    correct = result.ToString();
                }
            }

            var options = GenerateSuperNumberOptions(int.Parse(correct), 4 + difficulty / 10);

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

       // 20. ⭐ 颜色三重干扰（修复：只用单字颜色词）
private object GenerateTripleColorInterference(int level, int difficulty)
{
    var colorPool = _colorHex.ToArray();
    var selectedColors = new List<KeyValuePair<string, string>>();

    while (selectedColors.Count < 3)
    {
        var c = colorPool[_random.Next(colorPool.Length)];
        if (!selectedColors.Any(x => x.Key == c.Key))
        {
            selectedColors.Add(c);
        }
    }

    // ⭐ 只用单字颜色词
    string[] singleColorWords = { "红", "蓝", "绿", "黄", "紫", "橙", "粉", "青" };
    string displayWord = singleColorWords[_random.Next(singleColorWords.Length)];

    // 分配颜色
    var shuffledColors = selectedColors.OrderBy(_ => _random.Next()).ToList();
    var wordColor = shuffledColors[0];
    var bgColor = shuffledColors[1];
    var meaningColor = shuffledColors[2];

    // 确保三者不同
    while (bgColor.Key == wordColor.Key)
    {
        shuffledColors = selectedColors.OrderBy(_ => _random.Next()).ToList();
        wordColor = shuffledColors[0];
        bgColor = shuffledColors[1];
        meaningColor = shuffledColors[2];
    }
    while (meaningColor.Key == wordColor.Key || meaningColor.Key == bgColor.Key)
    {
        shuffledColors = selectedColors.OrderBy(_ => _random.Next()).ToList();
        wordColor = shuffledColors[0];
        bgColor = shuffledColors[1];
        meaningColor = shuffledColors[2];
    }

    // 三种提问
    string[] questions = new[]
    {
        $"字的颜色是什么？",
        $"背景是什么颜色？",
        $"「{displayWord}」这个字本身是什么颜色？"
    };

    int qIndex = _random.Next(3);
    string questionText = questions[qIndex];
    string correctAnswer = qIndex == 0 ? wordColor.Key : qIndex == 1 ? bgColor.Key : meaningColor.Key;

    // 选项
    var options = new List<string> { correctAnswer };
    int optionCount = 4 + Math.Min(difficulty / 10, 3);
    var pool = _colorNames.Where(c => c != correctAnswer).ToList();
    var shuffled = pool.OrderBy(_ => _random.Next()).ToList();

    for (int i = 0; i < Math.Min(optionCount - 1, shuffled.Count); i++)
    {
        options.Add(shuffled[i]);
    }

    // ⭐ 生成显示HTML
    string displayText = $"<div style='background:{bgColor.Value};padding:2rem 3.5rem;border-radius:20px;border:3px solid rgba(255,255,255,0.05);display:inline-block;box-shadow:0 0 60px {bgColor.Value}30;'>";
    displayText += $"<span style='color:{wordColor.Value};font-size:4rem;font-weight:900;text-shadow:0 0 50px {wordColor.Value}50;letter-spacing:10px;'>{displayWord}</span>";
    displayText += "</div>";

    int timeLimit = Math.Max(3, 8 - difficulty / 12);
    string diffLabel = difficulty > 70 ? "💀 地狱" : difficulty > 40 ? "🔥 噩梦" : "⚡ 困难";

    return new
    {
        type = "tripleColor",
        level = level,
        question = $"🎯 颜色三重干扰！（{diffLabel}）<br><span style='font-size:0.9rem;color:rgba(255,255,255,0.3);'>{questionText}</span>",
        displayText = displayText,
        correctAnswer = correctAnswer,
        options = options.OrderBy(_ => _random.Next()).ToList(),
        timeLimit = timeLimit,
        funMessage = GetFunMessage("tripleColor")
    };
}


        private string GetFunMessage(string type)
        {
            if (_funMessages.ContainsKey(type))
            {
                var msgs = _funMessages[type];
                return msgs[_random.Next(msgs.Length)];
            }
            return "🎉 太棒了！";
        }
        // ⭐ 相似字符映射（用于找不同）
private char GetSimilarChar(char c)
{
    var map = new Dictionary<char, char[]>
    {
        {'0', new[]{'O','D','Q'}},
        {'O', new[]{'0','D','Q'}},
        {'1', new[]{'I','L','7'}},
        {'I', new[]{'1','L','7'}},
        {'L', new[]{'1','I','J'}},
        {'5', new[]{'S','8'}},
        {'S', new[]{'5','8'}},
        {'8', new[]{'B','3','6'}},
        {'B', new[]{'8','3','6'}},
        {'3', new[]{'8','B','E'}},
        {'E', new[]{'3','B','F'}},
        {'6', new[]{'8','G','9'}},
        {'9', new[]{'6','P','G'}},
        {'G', new[]{'6','9','C'}},
        {'C', new[]{'G','O','0'}},
        {'D', new[]{'O','0','Q'}},
        {'Q', new[]{'O','0','D'}},
        {'2', new[]{'Z','7'}},
        {'Z', new[]{'2','7'}},
        {'7', new[]{'2','Z'}},
        {'T', new[]{'Y','7'}},
        {'Y', new[]{'T','V'}},
        {'V', new[]{'Y','U'}},
        {'W', new[]{'M','V'}},
        {'M', new[]{'W','N'}},
        {'N', new[]{'M','H'}},
        {'H', new[]{'N','A'}},
        {'A', new[]{'4','H'}},
        {'R', new[]{'P','A'}},
        {'P', new[]{'R','B'}},
        {'K', new[]{'H','N'}},
        {'X', new[]{'K','H'}},
        {'J', new[]{'L','I'}},
        {'U', new[]{'V','W'}},
        {'4', new[]{'A','H'}},
        {'F', new[]{'E','P','T'}},
        {'Z', new[]{'2','7'}}
    };

    if (map.ContainsKey(c))
    {
        var similar = map[c];
        return similar[_random.Next(similar.Length)];
    }
    
    string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    return chars[_random.Next(chars.Length)];
}
// ⭐ 判断两种颜色是否相似
private bool IsSimilarColor(string hex1, string hex2)
{
    if (hex1 == hex2) return false;
    try
    {
        var c1 = System.Drawing.ColorTranslator.FromHtml(hex1);
        var c2 = System.Drawing.ColorTranslator.FromHtml(hex2);
        var diff = Math.Abs(c1.R - c2.R) + Math.Abs(c1.G - c2.G) + Math.Abs(c1.B - c2.B);
        return diff < 150;
    }
    catch
    {
        return false;
    }
}

// ⭐ 超级相似字符（别名，直接调用 GetSimilarChar）
private char GetSuperSimilarChar(char c)
{
    return GetSimilarChar(c);
}
    }
}
