namespace MyPersonalWebsite.Services.Tarot;

/// <summary>
/// 单张塔罗牌（含正逆位牌义）。
/// </summary>
public sealed class TarotCard
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string NameEn { get; init; } = "";
    /// <summary>major | minor</summary>
    public string Arcana { get; init; } = "major";
    /// <summary>小阿卡纳花色：wands | cups | swords | pentacles（大阿卡纳为 null）</summary>
    public string? Suit { get; init; }
    /// <summary>小阿卡纳花色中文名（权杖/圣杯/宝剑/星币）</summary>
    public string? SuitName { get; init; }
    /// <summary>小阿卡纳点数中文（王牌/二/…/国王）</summary>
    public string? Rank { get; init; }
    /// <summary>牌面主视觉符号（emoji）</summary>
    public string Symbol { get; init; } = "";
    /// <summary>大阿卡纳罗马数字（愚者为 0）</summary>
    public string Roman { get; init; } = "";
    /// <summary>真实塔罗牌卡面图片 URL（公有领域 Rider-Waite-Smith，来自 GitHub 镜像，源图出自 Wikimedia Commons）</summary>
    public string ImageUrl { get; init; } = "";
    /// <summary>正位牌义（关键词/短句）</summary>
    public string Upright { get; init; } = "";
    /// <summary>逆位牌义（关键词/短句）</summary>
    public string Reversed { get; init; } = "";
}

/// <summary>
/// 伟特塔罗 78 张牌静态牌库（单一数据源）。
/// </summary>
public static class TarotDeck
{
    private static readonly Lazy<List<TarotCard>> _cards = new(Build);

    public static List<TarotCard> All => _cards.Value;

    public static TarotCard? GetById(int id)
    {
        var all = All;
        return id >= 0 && id < all.Count ? all[id] : null;
    }

    /// <summary>返回适合前端展示的精简数据（含牌义关键词，便于即时展示）。</summary>
    public static IReadOnlyList<TarotCard> ForClient() => All;

    private static List<TarotCard> Build()
    {
        var list = new List<TarotCard>(78);
        list.AddRange(BuildMajor());
        list.AddRange(BuildMinor());
        return list;
    }

    private static IEnumerable<TarotCard> BuildMajor()
    {
        // (中文名, 英文名, 罗马数字, 符号, 正位, 逆位)
        var data = new (string Name, string NameEn, string Roman, string Symbol, string Up, string Rev)[]
        {
            ("愚者",     "The Fool",           "0",   "🤸", "新的开始、纯真、冒险、无限可能",         "鲁莽、犹豫不决、逃避责任"),
            ("魔术师",   "The Magician",       "I",   "✨", "创造、意志力、才能、行动力",             "操纵、才能未用、计划受阻"),
            ("女祭司",   "The High Priestess", "II",  "🌙", "直觉、潜意识的智慧、静观",               "忽视直觉、隐藏的秘密、表面信息"),
            ("皇后",     "The Empress",        "III", "🌸", "丰饶、母性、创造、滋养",                 "依赖、创作瓶颈、忽视自我"),
            ("皇帝",     "The Emperor",        "IV",  "👑", "权威、稳定、秩序、领导力",               "专制、僵化、失去控制"),
            ("教皇",     "The Hierophant",     "V",   "⛪", "传统、信仰、导师、规则",                 "叛逆、打破常规、质疑权威"),
            ("恋人",     "The Lovers",         "VI",  "💞", "爱情、结合、和谐、抉择",                 "失衡、分离、价值观冲突"),
            ("战车",     "The Chariot",        "VII", "🏇", "意志、胜利、前进、自律",                 "失控、方向不明、犹豫"),
            ("力量",     "Strength",           "VIII","🦁", "勇气、内在力量、耐心、温柔的力量",       "自我怀疑、软弱、失去勇气"),
            ("隐士",     "The Hermit",         "IX",  "🏮", "内省、独处、寻求真理",                   "孤立、逃避、拒绝指导"),
            ("命运之轮", "Wheel of Fortune",   "X",   "🎡", "转机、命运、周期、好运",                 "停滞、厄运、抗拒改变"),
            ("正义",     "Justice",            "XI",  "⚖️", "公正、真相、因果、责任",                 "不公、逃避责任、偏见"),
            ("倒吊人",   "The Hanged Man",     "XII", "🙃", "换位思考、牺牲、等待、新视角",           "无谓牺牲、拖延、僵持"),
            ("死神",     "Death",              "XIII","💀", "结束与重生、蜕变、放下",                 "抗拒改变、停滞、恐惧结束"),
            ("节制",     "Temperance",         "XIV", "⚗️", "平衡、调和、耐心、中庸",                 "失衡、过度、急躁"),
            ("恶魔",     "The Devil",          "XV",  "⛓️", "束缚、诱惑、沉迷、阴影",                 "挣脱束缚、觉醒、重获自由"),
            ("塔",       "The Tower",          "XVI", "🗼", "剧变、崩塌、真相揭露、破而后立",         "避免灾难、拖延改变、恐惧"),
            ("星星",     "The Star",           "XVII","⭐", "希望、疗愈、灵感、宁静",                 "失去信心、绝望、灵感枯竭"),
            ("月亮",     "The Moon",           "XVIII","🌕","幻象、不安、潜意识的迷惘",               "真相渐明、走出迷雾、释放恐惧"),
            ("太阳",     "The Sun",            "XIX", "☀️", "成功、喜悦、活力、光明",                 "短暂挫折、缺乏热情、延迟的成功"),
            ("审判",     "Judgement",          "XX",  "🎺", "觉醒、重生、召唤、清算",                 "自我怀疑、错过机会、无法释怀"),
            ("世界",     "The World",          "XXI", "🌍", "圆满、完成、成就、整合",                 "未完成、缺少闭环、停滞"),
        };

        var id = 0;
        foreach (var d in data)
        {
            yield return new TarotCard
            {
                Id = id,
                ImageUrl = $"https://raw.githubusercontent.com/camik86/dsh-tarot-page/main/assets/cards/major-{id:00}.webp",
                Name = d.Name,
                NameEn = d.NameEn,
                Arcana = "major",
                Symbol = d.Symbol,
                Roman = d.Roman,
                Upright = d.Up,
                Reversed = d.Rev,
            };
            id++;
        }
    }

    private static IEnumerable<TarotCard> BuildMinor()
    {
        var suits = new[]
        {
            new
            {
                Suit = "wands", SuitName = "权杖", SuitEn = "Wands", Symbol = "🔥", Code = "wands",
                Up = new[]
                {
                    "新的激情、灵感、行动的开端", "规划、远见、抉择", "扩张、远航、等待回报",
                    "庆祝、稳定、归家、和谐", "竞争、冲突、磨炼", "胜利、认可、自信",
                    "坚守、防御、坚持立场", "飞速进展、消息、行动", "坚韧、最后的坚守、警惕",
                    "负担、责任、压力", "热情、探索、好消息", "冒险、冲动、行动力",
                    "自信、魅力、热情", "领导者、远见、魄力",
                },
                Rev = new[]
                {
                    "拖延、缺乏方向、创意受阻", "恐惧未知、计划不周、犹豫", "阻碍、延迟、目光短浅",
                    "家庭矛盾、暂缓庆祝、不稳定", "化解冲突、避免争执、内耗", "失败、缺乏认可、自负",
                    "力不从心、退让、被压垮", "延迟、阻碍、错失时机", "疲惫、放弃、过度防备",
                    "放下重担、减压、委派", "三分钟热度、方向不清", "鲁莽、半途而废、急躁",
                    "嫉妒、专横、缺乏自信", "专制、傲慢、缺乏执行力",
                },
            },
            new
            {
                Suit = "cups", SuitName = "圣杯", SuitEn = "Cups", Symbol = "💧", Code = "cups",
                Up = new[]
                {
                    "新的感情、爱、直觉涌流", "结合、相互吸引、伙伴关系", "友谊、庆祝、团聚",
                    "倦怠、沉思、错过机会", "失落、遗憾、聚焦失去", "怀旧、童年、纯真、重逢",
                    "幻想、选择过多、白日梦", "离开、追寻更深的意义", "愿望成真、满足、喜悦",
                    "圆满、家庭幸福、和谐", "温柔、灵感、爱的讯息", "浪漫、追求理想、魅力",
                    "共情、温柔、直觉", "情绪成熟、包容、智慧",
                },
                Rev = new[]
                {
                    "情感压抑、错过爱、空虚", "关系失衡、分离、误解", "过度放纵、流言、孤立",
                    "重新振作、接受新机会", "接受过去、重拾希望、宽恕", "沉溺过去、无法成长",
                    "认清现实、做出选择", "害怕改变、停滞、犹豫不决", "虚荣、不满足、愿望落空",
                    "家庭失和、理想破灭", "情绪化、幼稚、幻想", "不切实际、情绪波动、诱骗",
                    "情绪失衡、依赖、敏感过度", "情绪压抑、冷漠、操纵",
                },
            },
            new
            {
                Suit = "swords", SuitName = "宝剑", SuitEn = "Swords", Symbol = "⚔️", Code = "swords",
                Up = new[]
                {
                    "真相、清晰、突破", "僵局、权衡、回避决定", "心碎、悲伤、背叛",
                    "休息、恢复、沉思", "冲突、胜负、代价", "过渡、离开困境、平静",
                    "策略、隐瞒、独行", "受限、自我设限、无力感", "焦虑、噩梦、自责",
                    "终结、谷底、崩溃", "好奇、警觉、新想法", "果断、进取、直言",
                    "独立、理性、坦率", "理智、公正、权威",
                },
                Rev = new[]
                {
                    "混乱、误判、真相被掩盖", "做出决定、真相浮出", "疗愈、释怀、原谅",
                    "疲惫、无法安宁、躁动", "和解、放下争执、释然", "抗拒改变、滞留、不安",
                    "坦白、暴露、自我欺骗", "挣脱束缚、重获自由", "走出焦虑、释然、求助",
                    "触底反弹、恢复、转机", "多疑、冲动、言辞伤人", "冲动、鲁莽、攻击性",
                    "冷漠、刻薄、怨恨", "冷酷、专断、滥用权力",
                },
            },
            new
            {
                Suit = "pentacles", SuitName = "星币", SuitEn = "Pentacles", Symbol = "🪙", Code = "pentacles",
                Up = new[]
                {
                    "新的机会、财富、落地", "平衡、多任务、适应", "合作、技艺、认可",
                    "守财、稳定、控制", "匮乏、困顿、孤立", "给予、慷慨、互助",
                    "耐心、耕耘、等待回报", "精进、专注、技艺", "独立、富足、自给自足",
                    "传承、财富、家族圆满", "学习、务实、新计划", "勤奋、可靠、稳步前进",
                    "丰盛、持家、务实", "成功、稳重、富足",
                },
                Rev = new[]
                {
                    "错失机会、财务不稳、拖延", "失衡、超负荷、混乱", "合作不顺、批评、缺乏协作",
                    "吝啬、过度占有、放手", "转机、援助、走出困境", "不平等、施舍、债务",
                    "急躁、投入无果、半途而废", "敷衍、缺乏专注、停滞", "依赖、挥霍、缺乏安全感",
                    "财务纠纷、不稳定、破裂", "拖延、缺乏自律、不切实际", "僵化、无聊、停滞",
                    "失衡、自我忽视、物质主义", "贪婪、固执、物欲",
                },
            },
        };

        var ranks = new[] { "王牌", "二", "三", "四", "五", "六", "七", "八", "九", "十", "侍从", "骑士", "王后", "国王" };
        var ranksEn = new[] { "Ace", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Page", "Knight", "Queen", "King" };

        var id = 22;
        foreach (var suit in suits)
        {
            for (var i = 0; i < ranks.Length; i++)
            {
                yield return new TarotCard
                {
                    Id = id,
                    ImageUrl = $"https://raw.githubusercontent.com/camik86/dsh-tarot-page/main/assets/cards/{suit.Code}-{i + 1:00}.webp",
                    Name = suit.SuitName + ranks[i],
                    NameEn = ranksEn[i] + " of " + suit.SuitEn,
                    Arcana = "minor",
                    Suit = suit.Suit,
                    SuitName = suit.SuitName,
                    Rank = ranks[i],
                    Symbol = suit.Symbol,
                    Upright = suit.Up[i],
                    Reversed = suit.Rev[i],
                };
                id++;
            }
        }
    }
}
