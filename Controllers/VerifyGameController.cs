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
        // 颜色库 - 80+ 种颜色
        // ============================================================
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
            {"宝石蓝", "#0F52BA"}, {"玛瑙红", "#C04000"}, {"珍珠白", "#F5F5F5"}, {"青蓝", "#00BFFF"},
            {"紫红", "#C71585"}, {"橙红", "#FF4500"}, {"黄绿", "#9ACD32"}, {"蓝紫", "#8A2BE2"},
            {"粉红", "#FFB6C1"}, {"米色", "#F5F5DC"}, {"卡其", "#F0E68C"}, {"珊瑚", "#FF7F50"},
            {"青绿", "#008B8B"}, {"靛青", "#4B0082"}, {"藏青", "#000080"}, {"酒红", "#800020"},
            {"橄榄绿", "#556B2F"}, {"石板灰", "#708090"}, {"杏色", "#FFDAB9"}, {"薰衣草", "#E6E6FA"},
            {"紫藤", "#C9A0DC"}, {"樱粉", "#FFB7C5"}, {"薄荷", "#98FF98"}, {"奶油", "#FFFDD0"},
            {"浅绿", "#90EE90"}, {"浅蓝", "#ADD8E6"}, {"浅紫", "#D8BFD8"}, {"浅黄", "#FFFFE0"},
            {"浅橙", "#FFDAB9"}, {"浅灰", "#D3D3D3"}, {"深灰", "#A9A9A9"}, {"墨绿", "#006400"},
            {"海军蓝", "#000080"}, {"克莱因蓝", "#002FA7"}, {"蒂芙尼蓝", "#81D8D0"},
            {"马卡龙粉", "#FFB5C5"}, {"马卡龙蓝", "#A7D8DE"}, {"马卡龙黄", "#FDE8B6"},
            {"马卡龙紫", "#C9B1E0"}, {"马卡龙绿", "#B5D4C5"}, {"莫兰迪粉", "#DDB5B5"},
            {"莫兰迪蓝", "#A8BCCD"}, {"莫兰迪绿", "#B5C4B5"}, {"莫兰迪紫", "#C4B5D4"},
            {"荧光粉", "#FF1493"}, {"荧光绿", "#00FF00"}, {"荧光黄", "#CCFF00"}, {"荧光橙", "#FF6B00"},
            {"暗红", "#8B1A1A"}, {"暗蓝", "#1A2A6B"}, {"暗绿", "#1A4A2A"}, {"暗紫", "#4A1A6B"},
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
            {"白色", "白色"}, {"银灰", "银灰"}, {"紫罗兰", "紫罗兰"}, {"靛蓝", "靛蓝"},
            {"玫瑰红", "玫瑰红"}, {"柠檬黄", "柠檬黄"}, {"薄荷绿", "薄荷绿"}, {"珊瑚橙", "珊瑚橙"},
            {"象牙白", "象牙白"}, {"巧克力棕", "巧克力棕"}, {"琥珀金", "琥珀金"}, {"翡翠绿", "翡翠绿"},
            {"宝石蓝", "宝石蓝"}, {"玛瑙红", "玛瑙红"}, {"珍珠白", "珍珠白"}, {"青蓝", "青蓝"},
            {"紫红", "紫红"}, {"橙红", "橙红"}, {"黄绿", "黄绿"}, {"蓝紫", "蓝紫"},
            {"粉红", "粉红"}, {"米色", "米色"}, {"卡其", "卡其"}, {"珊瑚", "珊瑚"},
            {"青绿", "青绿"}, {"靛青", "靛青"}, {"藏青", "藏青"}, {"酒红", "酒红"},
            {"橄榄绿", "橄榄绿"}, {"石板灰", "石板灰"}, {"杏色", "杏色"},
            {"薰衣草", "薰衣草"}, {"紫藤", "紫藤"}, {"樱粉", "樱粉"},
            {"薄荷", "薄荷"}, {"奶油", "奶油"}, {"浅绿", "浅绿"}, {"浅蓝", "浅蓝"},
            {"浅紫", "浅紫"}, {"浅黄", "浅黄"}, {"浅橙", "浅橙"}, {"浅灰", "浅灰"},
            {"深灰", "深灰"}, {"墨绿", "墨绿"}, {"海军蓝", "海军蓝"}, {"克莱因蓝", "克莱因蓝"},
            {"蒂芙尼蓝", "蒂芙尼蓝"}, {"马卡龙粉", "马卡龙粉"}, {"马卡龙蓝", "马卡龙蓝"},
            {"马卡龙黄", "马卡龙黄"}, {"马卡龙紫", "马卡龙紫"}, {"马卡龙绿", "马卡龙绿"},
            {"莫兰迪粉", "莫兰迪粉"}, {"莫兰迪蓝", "莫兰迪蓝"}, {"莫兰迪绿", "莫兰迪绿"},
            {"莫兰迪紫", "莫兰迪紫"}, {"荧光粉", "荧光粉"}, {"荧光绿", "荧光绿"},
            {"荧光黄", "荧光黄"}, {"荧光橙", "荧光橙"}, {"暗红", "暗红"},
            {"暗蓝", "暗蓝"}, {"暗绿", "暗绿"}, {"暗紫", "暗紫"},
        };

        // ============================================================
        // 汉字笔画数据
        // ============================================================
        private readonly Dictionary<char, int> _strokeCount = new()
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

        // ============================================================
        // 成语库（500+）
        // ============================================================
        private readonly string[] _idioms = new string[]
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

        // ============================================================
        // 方向数据
        // ============================================================
        private readonly string[] _directions = new string[]
        {
            "上","下","左","右","左上","右上","左下","右下",
            "正上","正下","正左","正右","斜上","斜下","中上","中下",
            "东北","东南","西北","西南","北","南","东","西"
        };

        private readonly Dictionary<string, string> _directionOpposites = new()
        {
            {"上","下"},{"下","上"},{"左","右"},{"右","左"},
            {"左上","右下"},{"右上","左下"},{"左下","右上"},{"右下","左上"},
            {"正上","正下"},{"正下","正上"},{"正左","正右"},{"正右","正左"},
            {"斜上","斜下"},{"斜下","斜上"},{"中上","中下"},{"中下","中上"},
            {"东北","西南"},{"东南","西北"},{"西北","东南"},{"西南","东北"},
            {"北","南"},{"南","北"},{"东","西"},{"西","东"}
        };

        // ============================================================
        // 字母表
        // ============================================================
        private readonly char[] _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private readonly char[] _lowerAlphabet = "abcdefghijklmnopqrstuvwxyz".ToCharArray();

        // ============================================================
        // 英文单词库
        // ============================================================
        private readonly string[] _englishWords = new string[]
        {
            "APPLE", "BANANA", "CHERRY", "DRAGON", "EAGLE", "FALCON", "GOLDEN", "HEAVEN", "ICICLE", "JUNGLE",
            "KNIGHT", "LION", "MAGIC", "NIGHT", "OCEAN", "PEACE", "QUEEN", "RIVER", "STORM", "TIGER",
            "UNITY", "VICTORY", "WISDOM", "XENON", "YOUTH", "ZEBRA", "BLAZE", "CRYSTAL", "DREAM", "ELEPHANT",
            "FLOWER", "GARDEN", "HAPPY", "JACKET", "KITTEN", "LUNCH", "MUSCLE", "NEPHEW", "ORANGE", "PENCIL",
            "QUILT", "ROCKET", "SILVER", "TURTLE", "UMPIRE", "VALLEY", "WINDOW", "YELLOW", "BOTTLE", "CANDLE",
            "DANCER", "EFFECT", "FOSSIL", "GENTLE", "HONEST", "IGNORE", "KINDER", "LITTLE", "MOTHER",
            "NOTICE", "OFFICE", "PURPLE", "RABBIT", "SAVAGE", "TALENT", "UNIQUE", "VISUAL", "WICKED", "YONDER"
        };

        // ============================================================
        // 找规律题库 - 5个难度等级
        // ============================================================
        private readonly (string pattern, int answer, int minLevel, int maxLevel)[] _patternQuestions = new (string, int, int, int)[]
        {
            // 📝 入门 (1-20)
            ("2, 4, 6, ?, 10", 8, 1, 20),
            ("1, 3, 5, ?, 9", 7, 1, 20),
            ("10, 20, 30, ?, 50", 40, 1, 20),
            ("5, 10, 15, ?, 25", 20, 1, 20),
            ("1, 2, 4, ?, 11", 7, 1, 20),
            ("3, 6, 9, ?, 15", 12, 1, 20),
            ("2, 4, 8, ?, 32", 16, 1, 20),
            ("1, 4, 9, ?, 25", 16, 1, 20),
            ("2, 6, 12, ?, 30", 20, 1, 20),
            ("3, 7, 11, ?, 19", 15, 1, 20),
            ("1, 2, 3, 5, ?, 13", 8, 1, 20),
            ("2, 3, 5, 8, ?, 21", 13, 1, 20),
            ("1, 3, 7, 15, ?, 63", 31, 1, 20),
            ("1, 2, 6, 24, ?, 720", 120, 1, 20),

            // ⚡ 困难 (21-40)
            ("2, 5, 10, 17, ?, 37", 26, 21, 40),
            ("1, 3, 6, 10, ?, 21", 15, 21, 40),
            ("2, 6, 12, 20, ?, 42", 30, 21, 40),
            ("1, 2, 5, 10, ?, 26", 17, 21, 40),
            ("3, 8, 15, 24, ?, 48", 35, 21, 40),
            ("2, 3, 5, 9, ?, 33", 17, 21, 40),
            ("1, 4, 13, 40, ?, 364", 121, 21, 40),
            ("2, 5, 11, 23, ?, 95", 47, 21, 40),
            ("1, 3, 8, 19, ?, 81", 42, 21, 40),
            ("4, 9, 16, 25, ?, 49", 36, 21, 40),
            ("1, 8, 27, 64, ?, 216", 125, 21, 40),
            ("2, 8, 18, 32, ?, 72", 50, 21, 40),

            // 🔥 噩梦 (41-60)
            ("1, 2, 6, 15, 31, ?, 92", 56, 41, 60),
            ("2, 3, 7, 18, 47, ?, 322", 123, 41, 60),
            ("1, 4, 10, 22, 46, ?, 190", 94, 41, 60),
            ("3, 7, 15, 31, 63, ?, 255", 127, 41, 60),
            ("1, 3, 9, 31, 113, ?, 1913", 481, 41, 60),
            ("2, 5, 14, 41, 122, ?, 1094", 365, 41, 60),
            ("1, 2, 5, 14, 41, ?, 365", 122, 41, 60),
            ("3, 8, 23, 68, 203, ?, 1823", 608, 41, 60),
            ("1, 4, 15, 56, 209, ?, 3125", 780, 41, 60),

            // 💀 地狱 (61-80)
            ("1, 3, 7, 13, 21, ?, 43", 31, 61, 80),
            ("2, 6, 14, 30, 62, ?, 254", 126, 61, 80),
            ("1, 5, 13, 29, 61, ?, 253", 125, 61, 80),
            ("3, 10, 29, 66, 127, ?, 365", 218, 61, 80),
            ("1, 4, 18, 96, 600, ?, 45360", 4320, 61, 80),
            ("2, 6, 24, 120, 720, ?, 40320", 5040, 61, 80),
            ("1, 2, 8, 48, 384, ?, 46080", 3840, 61, 80),

            // 👑 传说 (81-100)
            ("1, 4, 27, 256, ?, 46656", 3125, 81, 100),
            ("2, 12, 36, 80, 150, ?, 392", 252, 81, 100),
            ("1, 3, 11, 51, 251, ?, 8255", 1251, 81, 100),
            ("3, 16, 45, 96, 175, ?, 441", 288, 81, 100),
            ("1, 2, 12, 72, 480, ?, 34560", 3600, 81, 100),
        };

        // ============================================================
        // 数独题库（4×4）- 包含完整解决方案
        // ============================================================
        private readonly (int[][] puzzle, int[][] solution)[] _sudokuData = new (int[][], int[][])[]
        {
            // 简单
            (
                new int[][] { new int[] {0, 0, 3, 0}, new int[] {0, 4, 0, 0}, new int[] {0, 0, 2, 0}, new int[] {0, 3, 0, 0} },
                new int[][] { new int[] {2, 1, 3, 4}, new int[] {3, 4, 1, 2}, new int[] {4, 2, 2, 3}, new int[] {1, 3, 4, 1} }
            ),
            (
                new int[][] { new int[] {1, 0, 0, 0}, new int[] {0, 0, 4, 0}, new int[] {0, 3, 0, 0}, new int[] {0, 0, 0, 2} },
                new int[][] { new int[] {1, 4, 2, 3}, new int[] {2, 3, 4, 1}, new int[] {3, 2, 1, 4}, new int[] {4, 1, 3, 2} }
            ),
            // 中等
            (
                new int[][] { new int[] {0, 0, 1, 0}, new int[] {0, 2, 0, 0}, new int[] {0, 0, 3, 0}, new int[] {0, 4, 0, 0} },
                new int[][] { new int[] {3, 4, 1, 2}, new int[] {1, 2, 4, 3}, new int[] {2, 1, 3, 4}, new int[] {4, 3, 2, 1} }
            ),
            (
                new int[][] { new int[] {0, 1, 0, 0}, new int[] {0, 0, 2, 0}, new int[] {0, 3, 0, 0}, new int[] {0, 0, 4, 0} },
                new int[][] { new int[] {2, 1, 3, 4}, new int[] {4, 3, 2, 1}, new int[] {1, 2, 4, 3}, new int[] {3, 4, 1, 2} }
            ),
            // 较难
            (
                new int[][] { new int[] {0, 0, 0, 0}, new int[] {0, 0, 0, 0}, new int[] {0, 0, 0, 0}, new int[] {0, 0, 0, 0} },
                new int[][] { new int[] {1, 2, 3, 4}, new int[] {3, 4, 1, 2}, new int[] {2, 1, 4, 3}, new int[] {4, 3, 2, 1} }
            ),
            (
                new int[][] { new int[] {0, 0, 0, 0}, new int[] {0, 0, 0, 0}, new int[] {0, 0, 0, 0}, new int[] {0, 0, 0, 0} },
                new int[][] { new int[] {4, 1, 2, 3}, new int[] {2, 3, 4, 1}, new int[] {1, 4, 3, 2}, new int[] {3, 2, 1, 4} }
            ),
        };

        // ============================================================
        // 真假判断题库 - 5个难度等级
        // ============================================================
        private readonly (string statement, bool isTrue, int minLevel, int maxLevel)[] _trueFalseQuestions = new (string, bool, int, int)[]
        {
            // 📝 入门 (1-20)
            ("地球是圆的", true, 1, 20),
            ("太阳从东边升起", true, 1, 20),
            ("水在0度时结冰", true, 1, 20),
            ("人类有206块骨头", true, 1, 20),
            ("企鹅生活在南极", true, 1, 20),
            ("熊猫是中国的国宝", true, 1, 20),
            ("北京是中国的首都", true, 1, 20),
            ("东京是日本的首都", true, 1, 20),
            ("金字塔在埃及", true, 1, 20),
            ("长城在中国", true, 1, 20),
            ("太阳从西边升起", false, 1, 20),
            ("月亮是恒星", false, 1, 20),
            ("鱼能在空中飞", false, 1, 20),
            ("蜘蛛是昆虫", false, 1, 20),
            ("巴黎是英国的首都", false, 1, 20),

            // ⚡ 困难 (21-40)
            ("鲸鱼是哺乳动物", true, 21, 40),
            ("成年人有32颗牙齿", true, 21, 40),
            ("蜜蜂会产蜜", true, 21, 40),
            ("地球绕太阳转", true, 21, 40),
            ("光速是宇宙中最快的", true, 21, 40),
            ("所有质数都是奇数", false, 21, 40),
            ("光年是时间单位", false, 21, 40),
            ("人类有8种血型", false, 21, 40),
            ("地球有5大洲", false, 21, 40),
            ("万里长城在太空可见", false, 21, 40),

            // 🔥 噩梦 (41-60)
            ("如果今天是周三，那么后天是周五", true, 41, 60),
            ("如果a>b且b>c，则a>c", true, 41, 60),
            ("所有正方形都是矩形", true, 41, 60),
            ("所有矩形都是正方形", false, 41, 60),
            ("如果a能被b整除，则b一定能被a整除", false, 41, 60),
            ("三角形内角和为180度", true, 41, 60),
            ("四边形内角和为360度", true, 41, 60),
            ("圆的周长是直径的π倍", true, 41, 60),

            // 💀 地狱 (61-80)
            ("如果A能被B整除，B能被C整除，则A一定能被C整除", true, 61, 80),
            ("如果a+b是偶数，则a和b都是偶数", false, 61, 80),
            ("如果a×b是偶数，则a和b至少有一个是偶数", true, 61, 80),
            ("如果a×b是奇数，则a和b都是奇数", true, 61, 80),
            ("两个质数的和一定是偶数", false, 61, 80),
            ("两个奇数的和是偶数", true, 61, 80),

            // 👑 传说 (81-100)
            ("如果a和b都是正整数，且a+b是奇数，则a和b一奇一偶", true, 81, 100),
            ("如果a和b都是正整数，且a×b是奇数，则a和b都是奇数", true, 81, 100),
            ("如果a是质数，则a+1一定是偶数", false, 81, 100),
            ("所有能被9整除的数都能被3整除", true, 81, 100),
            ("所有能被3整除的数都能被9整除", false, 81, 100),
            ("1既不是质数也不是合数", true, 81, 100),
            ("0是偶数", true, 81, 100),
            ("负数的平方是正数", true, 81, 100),
        };

        // ============================================================
        // 趣味消息
        // ============================================================
        private readonly Dictionary<string, string[]> _funMessages = new()
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
            {"memory", new[]{"🧠 照相机记忆！", "💪 超脑！", "✨ 过目不忘！"}},
            {"pattern", new[]{"📐 规律大师！", "🧠 模式识别！", "🎯 数学之眼！"}},
            {"colorMix", new[]{"🎨 色彩炼金术！", "🌈 颜色魔法师！", "✨ 视觉艺术！"}},
            {"trueFalse", new[]{"⚖️ 真相之神！", "🧐 明察秋毫！", "🎯 一语中的！"}},
            {"puzzle", new[]{"🧩 拼图大师！", "🎯 空间掌控者！", "✨ 华容道之王！"}},
            {"sudoku", new[]{"🧮 数独之神！", "📐 逻辑大师！", "🎯 填数天才！"}},
            {"countChar", new[]{"🔢 人形计数器！", "🧮 数学之神！", "💡 逻辑天才！"}},
            {"inverseColor", new[]{"🎨 视觉之神！", "🌈 火眼金睛！", "✨ 色彩大师！"}},
            {"mirror", new[]{"🪞 空间之神！", "🧠 超脑！", "💪 逻辑之王！"}},
            {"direction", new[]{"🧭 人形指南针！", "🗺️ 活地图！", "✨ 方向感之神！"}},
            {"tripleColor", new[]{"🎯 三重干扰通关！", "🌈 视觉之神！", "✨ 不是人类！"}},
            {"ultimate", new[]{"💀 超越人类！", "🔥 终极王者！", "👑 神级存在！"}},
        };

        // ============================================================
        // 构造函数
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

        // ============================================================
        // 获取挑战 - ⭐ 核心方法
        // ============================================================
        [HttpGet]
        public IActionResult GetChallenge(int level)
        {
            try
            {
                var result = GenerateChallenge(level);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // ⭐ 随机类型生成器（20种类型随机）
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

            // 中文类型名映射
            string[] typeNames = new string[]
            {
                "文字识别", "算术计算", "汉字笔画", "颜色识别", "找不同",
                "倒序识别", "空缺字母", "快速点击", "成语填空", "数字记忆",
                "找规律", "颜色混合", "真假判断", "数字华容道", "数独逻辑",
                "字符计数", "反色识别", "镜像字母", "方向判断", "颜色三重干扰"
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
                case 7: result = GenerateQuickTap(level, difficulty, typesCompleted); break;
                case 8: result = GenerateIdiomFill(level, difficulty, typesCompleted); break;
                case 9: result = GenerateMemoryChallenge(level, difficulty, typesCompleted); break;
                case 10: result = GeneratePatternRecognition(level, difficulty, typesCompleted); break;
                case 11: result = GenerateColorMix(level, difficulty, typesCompleted); break;
                case 12: result = GenerateTrueFalse(level, difficulty, typesCompleted); break;
                case 13: result = GeneratePuzzle(level, difficulty, typesCompleted); break;
                case 14: result = GenerateSudokuLogic(level, difficulty, typesCompleted); break;
                case 15: result = GenerateCharacterCount(level, difficulty, typesCompleted); break;
                case 16: result = GenerateInverseColor(level, difficulty, typesCompleted); break;
                case 17: result = GenerateMirrorLetter(level, difficulty, typesCompleted); break;
                case 18: result = GenerateDirection(level, difficulty, typesCompleted); break;
                default: result = GenerateTripleColorInterference(level, difficulty, typesCompleted); break;
            }

            // 使用反射或动态添加 typeName
            var dict = result as Dictionary<string, object>;
            if (dict == null)
            {
                // 如果是匿名对象，转换为字典
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
            if (difficulty >= 81) return "👑 传说";
            if (difficulty >= 61) return "💀 地狱";
            if (difficulty >= 41) return "🔥 噩梦";
            if (difficulty >= 21) return "⚡ 困难";
            return "📝 入门";
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

        private string GenerateSvgForText(string text, int distortion, int lineCount, int difficulty)
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
                {'F', new[]{'E','P','T'}}
            };

            if (map.ContainsKey(c))
            {
                var similar = map[c];
                return similar[_random.Next(similar.Length)];
            }

            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            return chars[_random.Next(chars.Length)];
        }

        private char GetSimilarChineseChar(char c)
        {
            var map = new Dictionary<char, char[]>
            {
                {'一', new[]{'二','三','十'}},
                {'二', new[]{'一','三','十'}},
                {'三', new[]{'一','二','五'}},
                {'十', new[]{'一','二','七'}},
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
                {'校', new[]{'较','铰','效'}},
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
                {'灏', new[]{'景','影','浩'}}
            };

            if (map.ContainsKey(c))
            {
                var similar = map[c];
                return similar[_random.Next(similar.Length)];
            }

            string chars = "一二三十人大天日月木王土田白自我你他的是不在学了生海湖路爱善国家春秋风雨雪星花草树林龙虎象猫熊窗楼桥船港峰岩泉溪梦幻智慧暴攀变魔警耀镶囊懿罐噩嚣龘灏";
            return chars[_random.Next(chars.Length)];
        }

        // ⭐⭐ 核心：RGB 颜色混合算法（真实混合）
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

        // ⭐⭐ 查找最接近的颜色（RGB 欧几里得距离）
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

        // ⭐ 生成颜色方形色块 HTML
        private string GenerateColorBlock(string colorName, int size = 60)
        {
            if (!_colorHex.ContainsKey(colorName))
                return $"<div style='width:{size}px;height:{size}px;border-radius:8px;background:#808080;border:1px solid rgba(255,255,255,0.04);'></div>";

            var hex = _colorHex[colorName];
            return $"<div style='width:{size}px;height:{size}px;border-radius:8px;background:{hex};border:1px solid rgba(255,255,255,0.06);box-shadow:0 4px 12px rgba(0,0,0,0.1);'></div>";
        }

        // ⭐ 生成颜色混合显示
        private string GenerateColorMixDisplay(string[] colorNames, string resultName, int size = 50)
        {
            var sb = new StringBuilder();
            sb.Append("<div style='display:flex;align-items:center;gap:12px;justify-content:center;padding:12px 0;flex-wrap:wrap;'>");

            foreach (var color in colorNames)
            {
                sb.Append(GenerateColorBlock(color, size));
                if (color != colorNames.Last())
                {
                    sb.Append("<span style='color:rgba(255,255,255,0.1);font-size:1.2rem;'>+</span>");
                }
            }

            sb.Append("<span style='color:rgba(255,255,255,0.08);font-size:1.2rem;'>→</span>");
            sb.Append("<div style='display:flex;flex-direction:column;align-items:center;gap:4px;'>");
            sb.Append(GenerateColorBlock(resultName, size + 10));
            sb.Append($"<span style='color:rgba(255,255,255,0.15);font-size:0.6rem;'>混合结果</span>");
            sb.Append("</div>");

            sb.Append("</div>");
            return sb.ToString();
        }

        // ⭐ 生成颜色选项列表（方形色块）
        private string GenerateColorOptionsHtml(List<string> colorNames, int size = 48)
        {
            var sb = new StringBuilder();
            sb.Append("<div style='display:grid;grid-template-columns:repeat(3,1fr);gap:8px;max-width:320px;margin:0 auto;'>");

            foreach (var color in colorNames)
            {
                var displayName = _colorDisplayName.ContainsKey(color) ? _colorDisplayName[color] : color;
                sb.Append($"<div style='display:flex;flex-direction:column;align-items:center;gap:3px;padding:6px;border-radius:10px;border:1px solid rgba(255,255,255,0.03);transition:all 0.3s ease;' class='color-option' data-color='{color}'>");
                sb.Append(GenerateColorBlock(color, size));
                sb.Append($"<span style='color:rgba(255,255,255,0.12);font-size:0.5rem;'>{displayName}</span>");
                sb.Append("</div>");
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        // ============================================================
        // ⭐ 题型 0：文字识别
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
        // ⭐ 题型 1：算术计算
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
        // ⭐ 题型 2：汉字笔画
        // ============================================================
        private object GenerateStrokeCount(int level, int difficulty, int typesCompleted)
        {
            Dictionary<char, int> strokePool;

            if (difficulty <= 25)
            {
                strokePool = _strokeCount.Where(kv => kv.Value <= 5).ToDictionary(kv => kv.Key, kv => kv.Value);
            }
            else if (difficulty <= 50)
            {
                strokePool = _strokeCount.Where(kv => kv.Value >= 6 && kv.Value <= 10).ToDictionary(kv => kv.Key, kv => kv.Value);
            }
            else if (difficulty <= 75)
            {
                strokePool = _strokeCount.Where(kv => kv.Value >= 11 && kv.Value <= 15).ToDictionary(kv => kv.Key, kv => kv.Value);
            }
            else
            {
                strokePool = _strokeCount.Where(kv => kv.Value >= 16).ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            if (strokePool.Count == 0) strokePool = _strokeCount;

            var allChars = strokePool.Keys.ToArray();
            char ch = allChars[_random.Next(allChars.Length)];
            int correct = strokePool[ch];

            var options = GenerateNumberOptions(correct, 4 + Math.Min(difficulty / 15, 3), 4);
            int timeLimit = Math.Max(3, 14 - difficulty / 8);

            return new Dictionary<string, object>
            {
                ["type"] = "stroke",
                ["level"] = level,
                ["question"] = $"📝 「{ch}」字有几画？",
                ["correctAnswer"] = correct.ToString(),
                ["options"] = options.OrderBy(_ => _random.Next()).ToList(),
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("stroke")
            };
        }

        // ============================================================
        // ⭐ 题型 3：颜色识别
        // ============================================================
        private object GenerateColorRecognition(int level, int difficulty, int typesCompleted)
        {
            var pool = _colorHex.ToArray();
            var selected = pool[_random.Next(pool.Length)];

            var similarColors = pool.Where(c => IsSimilarColor(c.Value, selected.Value)).ToList();
            if (similarColors.Count < 3) similarColors = pool.ToList();

            var options = new List<string> { selected.Key };
            int count = 4 + Math.Min(difficulty / 10, 3);

            var poolList = similarColors.Where(c => c.Key != selected.Key).ToList();
            for (int i = 0; i < count - 1 && i < poolList.Count; i++)
            {
                options.Add(poolList[i].Key);
            }

            int timeLimit = Math.Max(2, 8 - difficulty / 12);
            string displayWord = _singleColorWords[_random.Next(_singleColorWords.Length)];

            return new Dictionary<string, object>
            {
                ["type"] = "color",
                ["level"] = level,
                ["question"] = $"🎨 下面文字是什么颜色？",
                ["displayHtml"] = $"<span style='color:{selected.Value};font-size:3rem;font-weight:bold;'>{displayWord}</span>",
                ["correctAnswer"] = selected.Key,
                ["options"] = options.OrderBy(_ => _random.Next()).ToList(),
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("color")
            };
        }

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
            catch { return false; }
        }

        // ============================================================
        // ⭐ 题型 4：找不同
        // ============================================================
        private object GenerateFindDifferent(int level, int difficulty, int typesCompleted)
        {
            int length = Math.Min(4 + difficulty / 6, 14);
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            string original = "";
            for (int i = 0; i < length; i++)
                original += chars[_random.Next(chars.Length)];

            int pos = _random.Next(length);
            char originalChar = original[pos];
            char replacementChar = GetSimilarChar(originalChar);
            while (replacementChar == originalChar) replacementChar = GetSimilarChar(originalChar);

            char[] replacedArr = original.ToCharArray();
            replacedArr[pos] = replacementChar;
            string replaced = new string(replacedArr);

            char[] shuffledArr = replaced.ToCharArray();
            bool allSamePosition = true;
            int maxAttempts = 50;
            int attempts = 0;

            while (allSamePosition && attempts < maxAttempts)
            {
                for (int i = shuffledArr.Length - 1; i > 0; i--)
                {
                    int j = _random.Next(i + 1);
                    char temp = shuffledArr[i];
                    shuffledArr[i] = shuffledArr[j];
                    shuffledArr[j] = temp;
                }

                allSamePosition = true;
                for (int i = 0; i < shuffledArr.Length; i++)
                {
                    if (shuffledArr[i] != replacedArr[i])
                    {
                        allSamePosition = false;
                        break;
                    }
                }
                attempts++;
            }

            string shuffled = new string(shuffledArr);

            int shuffledPos = -1;
            for (int i = 0; i < shuffledArr.Length; i++)
            {
                if (shuffledArr[i] == replacementChar)
                {
                    shuffledPos = i;
                    break;
                }
            }

            if (shuffledPos == -1)
            {
                return GenerateFindDifferent(level, difficulty, typesCompleted);
            }

            int displayTime = Math.Max(2, 8 - difficulty / 8);
            int timeLimit = Math.Max(4, 14 - difficulty / 6);

            return new Dictionary<string, object>
            {
                ["type"] = "findDifferent",
                ["level"] = level,
                ["question"] = $"🔍 记住下面的字符，然后找出被更改的那个！",
                ["originalDisplay"] = original,
                ["displayTime"] = displayTime,
                ["shuffledDisplay"] = shuffled,
                ["shuffledPos"] = shuffledPos,
                ["displayText"] = original,
                ["correctAnswer"] = originalChar.ToString(),
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("findDifferent")
            };
        }
                // ============================================================
        // ⭐ 题型 5：倒序识别
        // ============================================================
        private object GenerateReverseText(int level, int difficulty, int typesCompleted)
        {
            int length = Math.Min(4 + difficulty / 6, 12);
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()_+-=<>?/";
            string text = "";
            for (int i = 0; i < length; i++) text += chars[_random.Next(chars.Length)];

            string reversed = new string(text.Reverse().ToArray());

            int lineCount = 30 + difficulty * 3;
            int distortion = 5 + difficulty / 2;
            var svg = GenerateSvgForText(text, distortion, lineCount, difficulty);

            var options = GenerateOptions(reversed, 4 + Math.Min(difficulty / 10, 4));
            int timeLimit = Math.Max(3, 14 - difficulty / 5);

            return new Dictionary<string, object>
            {
                ["type"] = "reverse",
                ["level"] = level,
                ["question"] = $"🔄 图片中的文字是什么？（倒过来了）",
                ["imageSvg"] = svg,
                ["correctAnswer"] = reversed,
                ["options"] = options,
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("reverse")
            };
        }

        // ============================================================
        // ⭐ 题型 6：空缺字母
        // ============================================================
        private object GenerateMissingLetter(int level, int difficulty, int typesCompleted)
        {
            string word;
            if (difficulty > 70)
            {
                var longWords = _englishWords.Where(w => w.Length >= 7).ToArray();
                word = longWords[_random.Next(longWords.Length)];
            }
            else if (difficulty > 40)
            {
                var mediumWords = _englishWords.Where(w => w.Length >= 5 && w.Length <= 7).ToArray();
                word = mediumWords[_random.Next(mediumWords.Length)];
            }
            else
            {
                var shortWords = _englishWords.Where(w => w.Length >= 4 && w.Length <= 5).ToArray();
                word = shortWords[_random.Next(shortWords.Length)];
            }

            int missingCount = difficulty > 70 ? 2 : 1;
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
                display[pos] = '_';
            }

            string correctAnswer = new string(correctChars);
            string displayStr = new string(display);

            string hintText = missingCount == 1 ? "1个字母被隐藏了" : $"{missingCount}个字母被隐藏了";

            var options = new List<string> { correctAnswer };
            int optionCount = 4 + Math.Min(difficulty / 10, 3);

            while (options.Count < optionCount)
            {
                string fake = "";
                for (int i = 0; i < correctAnswer.Length; i++)
                {
                    fake += _alphabet[_random.Next(26)];
                }
                if (!options.Contains(fake) && fake != correctAnswer)
                {
                    options.Add(fake);
                }
            }

            int timeLimit = Math.Max(3, 11 - difficulty / 7);

            return new Dictionary<string, object>
            {
                ["type"] = "missingLetter",
                ["level"] = level,
                ["question"] = $"🔤 补全下面的英文单词（{hintText}）：<br><span style='font-size:1.8rem;font-weight:bold;letter-spacing:6px;font-family:monospace;color:#8B5CF6;'>{displayStr}</span>",
                ["correctAnswer"] = correctAnswer,
                ["options"] = options.OrderBy(_ => _random.Next()).ToList(),
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("missingLetter")
            };
        }

        // ============================================================
        // ⭐ 题型 7：快速点击
        // ============================================================
        private object GenerateQuickTap(int level, int difficulty, int typesCompleted)
        {
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*";
            char target = chars[_random.Next(chars.Length)];
            var options = GenerateOptions(target.ToString(), 4 + Math.Min(difficulty / 10, 4));
            int timeLimit = Math.Max(2, 9 - difficulty / 8);

            return new Dictionary<string, object>
            {
                ["type"] = "quickTap",
                ["level"] = level,
                ["question"] = $"⚡ 从下方选项中，找出字符 <span style='color:#8B5CF6;font-weight:bold;font-size:1.5rem;'>{target}</span>",
                ["correctAnswer"] = target.ToString(),
                ["options"] = options,
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("quickTap")
            };
        }

        // ============================================================
        // ⭐ 题型 8：成语填空
        // ============================================================
        private object GenerateIdiomFill(int level, int difficulty, int typesCompleted)
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
                    char similar = GetSimilarChineseChar(correctAnswer[i]);
                    fake += similar;
                }
                if (!options.Contains(fake) && fake != correctAnswer)
                {
                    options.Add(fake);
                }
            }

            int timeLimit = Math.Max(3, 11 - difficulty / 8);

            return new Dictionary<string, object>
            {
                ["type"] = "idiom",
                ["level"] = level,
                ["question"] = $"📖 补全成语（缺{missingCount}个字）：{displayStr}",
                ["correctAnswer"] = correctAnswer,
                ["options"] = options.OrderBy(_ => _random.Next()).ToList(),
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("idiom")
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
        // ⭐ 题型 9：数字记忆（九宫格输入版）
        // ============================================================
        private object GenerateMemoryChallenge(int level, int difficulty, int typesCompleted)
        {
            int len = Math.Min(4 + difficulty / 6, 16);
            string text = "";
            for (int i = 0; i < len; i++) text += _random.Next(0, 10);

            int memoryTime = Math.Max(3, 8 - difficulty / 20);
            int timeLimit = Math.Max(10, 20 + difficulty / 5);

            // 生成打乱的数字键盘（0-9）
            var digits = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var shuffledDigits = digits.OrderBy(_ => _random.Next()).ToList();

            var keyboardRows = new List<List<int>>();
            for (int i = 0; i < 3; i++)
            {
                var row = new List<int>();
                for (int j = 0; j < 4; j++)
                {
                    int idx = i * 4 + j;
                    if (idx < shuffledDigits.Count)
                        row.Add(shuffledDigits[idx]);
                }
                keyboardRows.Add(row);
            }

            return new Dictionary<string, object>
            {
                ["type"] = "memory",
                ["level"] = level,
                ["question"] = $"🧠 记住这个数字：<span style='font-size:2.5rem;font-weight:bold;color:#8B5CF6;letter-spacing:8px;'>{text}</span>",
                ["displayNumber"] = text,
                ["memoryTime"] = memoryTime,
                ["correctAnswer"] = text,
                ["keyboardRows"] = keyboardRows,
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("memory")
            };
        }

        // ============================================================
        // ⭐ 题型 10：找规律
        // ============================================================
        private object GeneratePatternRecognition(int level, int difficulty, int typesCompleted)
        {
            var pool = _patternQuestions.Where(q =>
                difficulty >= q.minLevel && difficulty <= q.maxLevel).ToArray();

            if (pool.Length == 0) pool = _patternQuestions;

            var selected = pool[_random.Next(pool.Length)];

            var options = GenerateNumberOptions(selected.answer, 4 + Math.Min(difficulty / 10, 3), 15 + difficulty);
            int timeLimit = Math.Max(8, 20 - difficulty / 8);

            return new Dictionary<string, object>
            {
                ["type"] = "pattern",
                ["level"] = level,
                ["question"] = $"📐 找规律填空：<br><span style='font-size:1.8rem;font-weight:bold;color:#fff;'>{selected.pattern}</span>",
                ["correctAnswer"] = selected.answer.ToString(),
                ["options"] = options,
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("pattern")
            };
        }

        // ============================================================
        // ⭐ 题型 11：颜色混合（RGB 真实混合 + 方形色块）
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

            // 使用 RGB 真实混合
            string resultColor = selectedColors[0];
            for (int i = 1; i < selectedColors.Count; i++)
            {
                resultColor = MixColorsRGB(resultColor, selectedColors[i]);
            }

            // 生成干扰选项（颜色相似度）
            var allColors = _colorNames.ToList();
            var options = new List<string> { resultColor };

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

            var displayHtml = GenerateColorMixDisplay(selectedColors.ToArray(), resultColor);
            var optionsHtml = GenerateColorOptionsHtml(options);

            int timeLimit = Math.Max(8, 20 - difficulty / 10);

            return new Dictionary<string, object>
            {
                ["type"] = "colorMix",
                ["level"] = level,
                ["question"] = $"🎨 以下颜色混合后是什么颜色？<br><span style='color:rgba(255,255,255,0.15);font-size:0.8rem;'>点击色块选择答案</span>",
                ["displayHtml"] = displayHtml,
                ["optionsHtml"] = optionsHtml,
                ["options"] = options,
                ["correctAnswer"] = resultColor,
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("colorMix")
            };
        }

        // ============================================================
        // ⭐ 题型 12：真假判断（5个难度等级）
        // ============================================================
        private object GenerateTrueFalse(int level, int difficulty, int typesCompleted)
        {
            var pool = _trueFalseQuestions.Where(q =>
                difficulty >= q.minLevel && difficulty <= q.maxLevel).ToArray();

            if (pool.Length == 0) pool = _trueFalseQuestions;

            var selected = pool[_random.Next(pool.Length)];

            var options = new List<string> { "✅ 真的", "❌ 假的" };
            string correctAnswer = selected.isTrue ? "✅ 真的" : "❌ 假的";

            int timeLimit = 60;

            return new Dictionary<string, object>
            {
                ["type"] = "trueFalse",
                ["level"] = level,
                ["question"] = $"⚖️ 判断以下陈述是否正确：<br><span style='font-size:1.3rem;color:#fff;font-weight:500;'>{selected.statement}</span>",
                ["correctAnswer"] = correctAnswer,
                ["options"] = options,
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("trueFalse")
            };
        }

        // ============================================================
        // ⭐ 题型 13：数字华容道（3×3）
        // ============================================================
        private object GeneratePuzzle(int level, int difficulty, int typesCompleted)
        {
            var puzzle = Generate3x3Puzzle(difficulty);
            int timeLimit = Math.Min(120, 30 + difficulty / 2);

            return new Dictionary<string, object>
            {
                ["type"] = "puzzle",
                ["level"] = level,
                ["question"] = $"🧩 将数字按顺序排列（1-8），空格为0<br><span style='color:rgba(255,255,255,0.15);font-size:0.8rem;'>点击数字移动，限时{timeLimit}秒</span>",
                ["puzzle"] = puzzle,
                ["correctAnswer"] = "solved",
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("puzzle")
            };
        }

        private int[][] Generate3x3Puzzle(int difficulty)
        {
            int[][] target = new int[][]
            {
                new int[] { 1, 2, 3 },
                new int[] { 4, 5, 6 },
                new int[] { 7, 8, 0 }
            };

            int steps = 20 + difficulty * 2;
            steps = Math.Min(steps, 200);

            var puzzle = target.Select(row => row.ToArray()).ToArray();

            int emptyRow = 2;
            int emptyCol = 2;

            int lastMove = -1;
            for (int s = 0; s < steps; s++)
            {
                var moves = GetValidMoves(emptyRow, emptyCol, lastMove);
                if (moves.Count == 0) break;

                var move = moves[_random.Next(moves.Count)];
                lastMove = move.direction;

                int newRow = emptyRow + move.dRow;
                int newCol = emptyCol + move.dCol;

                puzzle[emptyRow][emptyCol] = puzzle[newRow][newCol];
                puzzle[newRow][newCol] = 0;

                emptyRow = newRow;
                emptyCol = newCol;
            }

            return puzzle;
        }

        private List<(int dRow, int dCol, int direction)> GetValidMoves(int row, int col, int lastMove)
        {
            var moves = new List<(int, int, int)>();
            int[][] directions = new int[][]
            {
                new int[] { -1, 0, 0 },
                new int[] { 1, 0, 1 },
                new int[] { 0, -1, 2 },
                new int[] { 0, 1, 3 }
            };

            int opposite = lastMove == 0 ? 1 : lastMove == 1 ? 0 : lastMove == 2 ? 3 : lastMove == 3 ? 2 : -1;

            foreach (var d in directions)
            {
                int newRow = row + d[0];
                int newCol = col + d[1];
                if (newRow >= 0 && newRow < 3 && newCol >= 0 && newCol < 3)
                {
                    if (d[2] != opposite)
                    {
                        moves.Add((d[0], d[1], d[2]));
                    }
                }
            }

            return moves;
        }

        // ============================================================
        // ⭐ 题型 14：数独逻辑（4×4）- ⭐ 修复：使用完整数据 + 正确传递 gridHtml
        // ============================================================
        private object GenerateSudokuLogic(int level, int difficulty, int typesCompleted)
        {
            int puzzleIndex;
            if (difficulty <= 30) puzzleIndex = 0;
            else if (difficulty <= 60) puzzleIndex = 1;
            else if (difficulty <= 80) puzzleIndex = 2;
            else puzzleIndex = 3;

            puzzleIndex = Math.Min(puzzleIndex, _sudokuData.Length - 1);

            var data = _sudokuData[puzzleIndex];
            var puzzle = data.puzzle;
            var solution = data.solution;

            // 找到第一个空格的位置
            int emptyRow = -1, emptyCol = -1;
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    if (puzzle[r][c] == 0)
                    {
                        emptyRow = r;
                        emptyCol = c;
                        break;
                    }
                }
                if (emptyRow != -1) break;
            }

            // 如果没找到空格（理论上不可能），返回默认
            if (emptyRow == -1)
            {
                emptyRow = 0;
                emptyCol = 0;
            }

            int correctAnswer = solution[emptyRow][emptyCol];

            var options = new List<string> { correctAnswer.ToString() };
            var numbers = new List<int> { 1, 2, 3, 4 };
            var remaining = numbers.Where(n => n != correctAnswer).ToList();
            for (int i = 0; i < Math.Min(3, remaining.Count); i++)
            {
                options.Add(remaining[i].ToString());
            }
            options = options.OrderBy(_ => _random.Next()).ToList();

            string gridHtml = GenerateSudokuHtml(puzzle, emptyRow, emptyCol);

            int timeLimit = Math.Max(20, 45 - difficulty / 5);

            return new Dictionary<string, object>
            {
                ["type"] = "sudoku",
                ["level"] = level,
                ["question"] = $"🧮 填入缺失的数字（每行每列1-4不重复）<br><span style='color:rgba(255,255,255,0.12);font-size:0.7rem;'>点击选项填空</span>",
                ["gridHtml"] = gridHtml,
                ["emptyRow"] = emptyRow,
                ["emptyCol"] = emptyCol,
                ["correctAnswer"] = correctAnswer.ToString(),
                ["options"] = options,
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("sudoku")
            };
        }

        private string GenerateSudokuHtml(int[][] puzzle, int emptyRow, int emptyCol)
        {
            var sb = new StringBuilder();
            sb.Append("<div style='display:grid;grid-template-columns:repeat(4,1fr);gap:4px;max-width:240px;margin:0 auto;padding:8px;background:rgba(255,255,255,0.02);border-radius:12px;border:1px solid rgba(255,255,255,0.04);'>");

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    int val = puzzle[r][c];
                    // 2x2 宫格边框
                    string borderRight = (c + 1) % 2 == 0 ? "border-right:2px solid rgba(255,255,255,0.06);" : "";
                    string borderBottom = (r + 1) % 2 == 0 ? "border-bottom:2px solid rgba(255,255,255,0.06);" : "";
                    string extraStyle = borderRight + borderBottom;

                    if (r == emptyRow && c == emptyCol)
                    {
                        sb.Append($"<div style='padding:8px;text-align:center;font-size:1.5rem;font-weight:bold;color:#8B5CF6;background:rgba(139,92,246,0.04);border-radius:4px;{extraStyle}' id='sudokuEmpty'>?</div>");
                    }
                    else
                    {
                        sb.Append($"<div style='padding:8px;text-align:center;font-size:1.5rem;font-weight:bold;color:rgba(255,255,255,0.5);border-radius:4px;{extraStyle}'>{val}</div>");
                    }
                }
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        // ============================================================
        // ⭐ 题型 15：字符计数
        // ============================================================
        private object GenerateCharacterCount(int level, int difficulty, int typesCompleted)
        {
            int len = Math.Min(6 + difficulty / 5, 25);
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*()_+-=<>?/";
            string text = "";
            for (int i = 0; i < len; i++) text += chars[_random.Next(chars.Length)];

            char target = chars[_random.Next(chars.Length)];
            int count = text.Count(c => c == target);

            var options = GenerateNumberOptions(count, 4 + difficulty / 10, 5 + difficulty / 5);
            int timeLimit = Math.Max(3, 11 - difficulty / 7);

            return new Dictionary<string, object>
            {
                ["type"] = "countChar",
                ["level"] = level,
                ["question"] = $"🔢 字符「{target}」在「{text}」中出现几次？",
                ["correctAnswer"] = count.ToString(),
                ["options"] = options,
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("countChar")
            };
        }

        // ============================================================
        // ⭐ 题型 16：反色识别
        // ============================================================
        private object GenerateInverseColor(int level, int difficulty, int typesCompleted)
        {
            var colors = new[] { "黑色", "白色", "灰色", "深灰", "浅灰" };
            int idx = _random.Next(colors.Length);
            string color = colors[idx];

            string bg = color == "黑色" ? "#FFFFFF" : color == "白色" ? "#000000" : "#808080";
            string textColor = color == "黑色" ? "#000000" : color == "白色" ? "#FFFFFF" : "#404040";

            int timeLimit = Math.Max(2, 7 - difficulty / 12);
            string[] texts = { "颜色", "色彩", "文字" };
            string displayText = texts[_random.Next(texts.Length)];

            return new Dictionary<string, object>
            {
                ["type"] = "inverseColor",
                ["level"] = level,
                ["question"] = $"🎨 下面文字是什么颜色？（注意背景）",
                ["displayHtml"] = $"<span style='color:{textColor};background:{bg};padding:0.3rem 1.5rem;border-radius:8px;font-size:2rem;font-weight:bold;'>{displayText}</span>",
                ["correctAnswer"] = color,
                ["options"] = new List<string>(colors),
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("inverseColor")
            };
        }

        // ============================================================
        // ⭐ 题型 17：镜像字母
        // ============================================================
        private object GenerateMirrorLetter(int level, int difficulty, int typesCompleted)
        {
            char[] mirrorKeys = new char[] {
                'A','H','I','M','O','T','U','V','W','X','Y',
                'C','D','E','K','P','S','Z'
            };
            char c = mirrorKeys[_random.Next(mirrorKeys.Length)];
            var options = GenerateOptions(c.ToString(), 4 + Math.Min(difficulty / 10, 4));
            int timeLimit = Math.Max(2, 9 - difficulty / 8);

            return new Dictionary<string, object>
            {
                ["type"] = "mirror",
                ["level"] = level,
                ["question"] = $"🪞 字母「{c}」的镜像字母是？",
                ["correctAnswer"] = c.ToString(),
                ["options"] = options,
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("mirror")
            };
        }

        // ============================================================
        // ⭐ 题型 18：方向判断
        // ============================================================
        private object GenerateDirection(int level, int difficulty, int typesCompleted)
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

            int timeLimit = Math.Max(2, 8 - difficulty / 14);

            return new Dictionary<string, object>
            {
                ["type"] = "direction",
                ["level"] = level,
                ["question"] = $"🧭 请选择「{dir}」的相反方向",
                ["correctAnswer"] = correct,
                ["options"] = options.OrderBy(_ => _random.Next()).ToList(),
                ["timeLimit"] = timeLimit,
                ["typesCompleted"] = typesCompleted,
                ["funMessage"] = GetFunMessage("direction")
            };
        }

        // ============================================================
        // ⭐ 题型 19：颜色三重干扰
        // ============================================================
        private object GenerateTripleColorInterference(int level, int difficulty, int typesCompleted)
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
                var allColors = _colorHex.ToArray();
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
            var pool = _colorNames.Where(c => c != correctAnswer).ToList();
            var shuffled = pool.OrderBy(_ => _random.Next()).ToList();

            for (int i = 0; i < Math.Min(optionCount - 1, shuffled.Count); i++)
            {
                options.Add(shuffled[i]);
            }

            string displayHtml = $"<div style='background:{bgColor.Value};padding:2rem 3.5rem;border-radius:20px;border:3px solid rgba(255,255,255,0.05);display:inline-block;box-shadow:0 0 60px {bgColor.Value}30;'>";
            displayHtml += $"<span style='color:{wordColor.Value};font-size:4rem;font-weight:900;text-shadow:0 0 50px {wordColor.Value}50;letter-spacing:10px;'>{displayWord}</span>";
            displayHtml += "</div>";

            int timeLimit = Math.Max(3, 9 - difficulty / 12);

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
