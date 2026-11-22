using System.Collections.Generic;

namespace MyPersonalWebsite.Utilities
{
    public static class WerewolfRules
    {
        /// <summary>
        /// 获取完整规则（分页）- 基于官方标准
        /// </summary>
        public static List<RulePage> GetRulePages(int playerCount, List<string> selectedRoles)
        {
            var is屠边 = playerCount >= 8; // 8人及以上默认屠边
            var hasCupid = selectedRoles.Contains("丘比特");

            return new List<RulePage>
            {
                // ===== 第1页：胜利条件（官方标准） =====
                new RulePage
                {
                    Title = "🏆 胜利条件",
                    Icon = "🏆",
                    Order = 1,
                    Content = new List<string>
                    {
                        "⭐ 好人阵营胜利",
                        "  • 所有狼人阵营玩家出局[citation:1][citation:6]",
                        "",
                        "🐺 狼人阵营胜利（屠边）",
                        $"  • 所有神职出局 或 所有平民出局[citation:1][citation:4][citation:10]",
                        "",
                        hasCupid ? "❤️ 第三方阵营胜利（人狼情侣）" : "",
                        hasCupid ? "  • 除情侣阵营外所有玩家出局（屠城）[citation:1][citation:12]" : "",
                        "",
                        $"📊 当前配置：{playerCount}人局 | {(is屠边 ? "屠边" : "屠城")}规则"
                    },
                    AdditionalInfo = "⚡ 特殊板子可能存在额外胜利条件"
                },

                // ===== 第2页：阵营与角色 =====
                new RulePage
                {
                    Title = "👥 阵营与角色",
                    Icon = "👥",
                    Order = 2,
                    Content = new List<string>
                    {
                        "🌟 好人阵营（神职 + 平民）",
                        "  • 胜利条件：所有狼人出局",
                        "",
                        "🐺 狼人阵营",
                        "  • 胜利条件：屠边（杀光所有神职或所有平民）",
                        "",
                        hasCupid ? "❤️ 第三方阵营（丘比特 + 情侣）" : "",
                        hasCupid ? "  • 胜利条件：屠城（除情侣外全灭）" : "",
                        "",
                        "💡 神职包括：预言家、女巫、守卫、猎人等",
                        "💡 平民无特殊技能，通过发言投票找出狼人"
                    },
                    AdditionalInfo = "📱 点击角色名称可查看详细技能"
                },

                // ===== 第3页：昼夜流程 =====
                new RulePage
                {
                    Title = "🌙 昼夜流程",
                    Icon = "🌙",
                    Order = 3,
                    Content = new List<string>
                    {
                        "🌙 夜晚阶段（按顺序执行）",
                        "  1. 守卫 → 守护一名玩家",
                        "  2. 预言家 → 查验一名玩家",
                        "  3. 狼人 → 统一意见杀害一名玩家",
                        "  4. 女巫 → 解药救人 / 毒药杀人",
                        "  5. 天亮",
                        "",
                        "☀️ 白天阶段（按顺序执行）",
                        "  1. 公布死讯",
                        "  2. 发言阶段（顺序发言）",
                        "  3. 投票阶段",
                        "  4. 公布放逐结果",
                        "  5. 天黑",
                        "",
                        "💡 首夜女巫可以自救",
                        "💡 守卫第一天可以守护自己"
                    },
                    AdditionalInfo = "⚡ 所有操作在手机上完成"
                },

                // ===== 第4页：发言与投票规则 =====
                new RulePage
                {
                    Title = "📢 发言与投票",
                    Icon = "📢",
                    Order = 4,
                    Content = new List<string>
                    {
                        "📢 发言规则",
                        "  • 每人发言时间：60秒",
                        "  • 从死者左手边开始顺序发言",
                        "  • 第一天竞选警长（可选）",
                        "  • 警长拥有归票权，投票算1.5票",
                        "",
                        "🗳️ 投票规则",
                        "  • 每人一票，投给要放逐的玩家",
                        "  • 平票则PK发言",
                        "  • PK后再平票则平安日（无人被放逐）",
                        "",
                        "💀 遗言规则",
                        "  • 第一天死亡有遗言",
                        "  • 之后死亡无遗言"
                    },
                    AdditionalInfo = "💡 点击发言顺序可查看当前发言人"
                },

                // ===== 第5页：角色技能速查 =====
                new RulePage
                {
                    Title = "🃏 角色技能速查",
                    Icon = "🃏",
                    Order = 5,
                    Content = BuildRoleList(selectedRoles),
                    AdditionalInfo = "📱 点击角色名称查看详细技能说明"
                },

                // ===== 第6页：特殊规则 =====
                new RulePage
                {
                    Title = "⚡ 特殊规则",
                    Icon = "⚡",
                    Order = 6,
                    Content = new List<string>
                    {
                        "🛡️ 同守同救（奶穿）",
                        "  • 同一晚被守护 + 女巫解救 = 死亡",
                        "",
                        "🔫 猎人",
                        "  • 死亡时可开枪带走一人",
                        "  • 被女巫毒杀不能开枪",
                        "",
                        "🤡 白痴",
                        "  • 被投票放逐时可翻牌免死一次",
                        "  • 翻牌后失去投票权，但仍可发言",
                        "",
                        "🐺 狼人自爆[citation:2]",
                        "  • 白天可自爆，立即进入黑夜",
                        "  • 自爆可打断当前发言",
                        "  • 警长竞选阶段自爆会'吞警徽'",
                        "",
                        "💀 狼人刀人规则[citation:2]",
                        "  • 所有狼人必须统一意见",
                        "  • 意见不统一时按多数意见处理",
                        "  • 刀数相同时随机选择目标"
                    },
                    AdditionalInfo = "📖 详细规则可在游戏中查看"
                }
            };
        }

        private static List<string> BuildRoleList(List<string> selectedRoles)
        {
            var list = new List<string>();
            var roleDetails = GetRoleShortDescriptions();

            list.Add("🃏 本局角色配置：");
            list.Add("");

            foreach (var role in selectedRoles)
            {
                if (roleDetails.ContainsKey(role))
                {
                    list.Add($"  {roleDetails[role]}");
                }
                else
                {
                    list.Add($"  • {role}");
                }
            }

            list.Add("");
            list.Add("💡 点击角色名称可查看详细技能说明");

            return list;
        }

        public static Dictionary<string, string> GetRoleShortDescriptions()
        {
            return new Dictionary<string, string>
            {
                { "狼人", "🐺 狼人：每晚共同杀害一名玩家" },
                { "预言家", "🔮 预言家：每晚查验一名玩家身份" },
                { "女巫", "🧪 女巫：解药救人，毒药杀人" },
                { "守卫", "🛡️ 守卫：每晚守护一人免遭袭击" },
                { "猎人", "🔫 猎人：死亡时开枪带走一人" },
                { "白痴", "🤡 白痴：被放逐时翻牌免死一次" },
                { "骑士", "⚔️ 骑士：白天挑战一名玩家" },
                { "丘比特", "❤️ 丘比特：首夜指定两名玩家成为情侣" },
                { "狼王", "👑 狼王：死亡时带走一人" },
                { "长老", "🧓 长老：首次被狼人袭击不会死亡" },
                { "盗贼", "🗡️ 盗贼：首夜可替换自己的一张身份牌" },
                { "禁言长老", "🔇 禁言长老：白天可禁言一名玩家" },
                { "隐狼", "🫥 隐狼：隐藏于狼群之外，狼队友不相认" },
                { "平民", "👤 平民：无特殊技能，通过发言投票找出狼人" }
            };
        }

        /// <summary>
        /// 获取角色详细说明
        /// </summary>
        public static Dictionary<string, RoleDetail> GetRoleDetails()
        {
            return new Dictionary<string, RoleDetail>
            {
                { "狼人", new RoleDetail
                {
                    Name = "🐺 狼人",
                    Description = "每晚可以共同杀害一名玩家。[citation:2]",
                    Detail = @"
【技能】每晚可共同杀害一名玩家。
【规则】所有狼人需投票统一意见，否则无法行动。[citation:2]
【自爆】白天可自爆，立即进入黑夜，可打断当前发言。[citation:2]
【吞警徽】警长竞选阶段自爆会导致本局无警长。[citation:2]
【胜利条件】好人阵营全部出局（屠边）。",
                    Tips = "💡 狼人之间可以交流战术！"
                }},
                { "预言家", new RoleDetail
                {
                    Name = "🔮 预言家",
                    Description = "每晚可以查验一名玩家的阵营。",
                    Detail = @"
【技能】每晚可查验一名玩家身份（好人/狼人）。
【规则】查验结果仅自己可见。
【胜利条件】所有狼人出局。",
                    Tips = "💡 建议首夜查验身边坐着的玩家！"
                }},
                { "女巫", new RoleDetail
                {
                    Name = "🧪 女巫",
                    Description = "拥有解药和毒药各一瓶。",
                    Detail = @"
【技能】拥有解药和毒药各一瓶，每晚最多使用一瓶。
【规则】解药可救人（首夜可自救），毒药可杀人。
【特殊】同守同救（奶穿）会导致目标死亡。",
                    Tips = "💡 解药和毒药各只能用一次，请谨慎使用！"
                }},
                { "守卫", new RoleDetail
                {
                    Name = "🛡️ 守卫",
                    Description = "每晚守护一名玩家免遭狼人袭击。",
                    Detail = @"
【技能】每晚可守护一名玩家免受狼人袭击。
【规则】不能连续两晚守护同一个人。
【特殊】第一天可以守护自己，之后不能守护自己。
【特殊】同守同救（奶穿）会导致目标死亡。",
                    Tips = "💡 记住上一晚守护了谁，别重复守护！"
                }},
                { "猎人", new RoleDetail
                {
                    Name = "🔫 猎人",
                    Description = "死亡时可以开枪带走一名玩家。",
                    Detail = @"
【技能】死亡时可开枪带走一名玩家。
【规则】被毒杀不能开枪。
【特殊】可自行选择是否开枪。",
                    Tips = "💡 如果被狼人刀死，可以开枪！"
                }},
                { "白痴", new RoleDetail
                {
                    Name = "🤡 白痴",
                    Description = "被放逐时可翻牌免死一次。",
                    Detail = @"
【技能】被投票放逐时可翻牌免死一次。
【规则】翻牌后失去投票权，但仍可发言。
【胜利条件】好人阵营胜利。",
                    Tips = "💡 翻牌后你依然可以继续游戏！"
                }},
                { "骑士", new RoleDetail
                {
                    Name = "⚔️ 骑士",
                    Description = "白天可挑战一名玩家。",
                    Detail = @"
【技能】白天可挑战一名玩家。
【规则】若挑战目标为狼人，目标直接死亡。
【特殊】若挑战目标为好人，骑士直接死亡。",
                    Tips = "💡 挑战前请仔细推理！"
                }},
                { "丘比特", new RoleDetail
                {
                    Name = "❤️ 丘比特",
                    Description = "首夜指定两名玩家成为情侣。",
                    Detail = @"
【技能】首夜可指定两名玩家成为情侣。
【规则】若情侣为人狼，则形成第三方阵营。
【胜利条件（第三方）】除情侣外所有玩家出局。[citation:12]",
                    Tips = "💡 人狼情侣会改变游戏走向！"
                }},
                { "狼王", new RoleDetail
                {
                    Name = "👑 狼王",
                    Description = "死亡时可以带走一名玩家。",
                    Detail = @"
【技能】死亡时可带走一名玩家（与猎人技能相同，属于狼人阵营）。
【规则】被女巫毒杀同样可以开枪。
【胜利条件】狼人阵营胜利。",
                    Tips = "💡 狼王出局时记得选择要带走的玩家！"
                }},
                { "长老", new RoleDetail
                {
                    Name = "🧓 长老",
                    Description = "首次被狼人袭击不会死亡。",
                    Detail = @"
【技能】首次被狼人袭击时不会死亡。
【规则】若被女巫毒杀或被投票放逐，仍会正常出局。
【胜利条件】好人阵营胜利。",
                    Tips = "💡 长老对狼人有很强的抗性！"
                }},
                { "盗贼", new RoleDetail
                {
                    Name = "🗡️ 盗贼",
                    Description = "首夜可替换自己的一张身份牌。",
                    Detail = @"
【技能】首夜可从备用身份牌中替换自己的一张身份。
【规则】替换后，本局身份以新牌为准。
【胜利条件】按最终身份所属阵营判定。",
                    Tips = "💡 盗贼可改变自己的阵营归属！"
                }},
                { "禁言长老", new RoleDetail
                {
                    Name = "🔇 禁言长老",
                    Description = "白天可禁言一名玩家。",
                    Detail = @"
【技能】白天可禁言一名玩家，使其本轮无法发言。
【规则】禁言后该玩家仍可投票。
【胜利条件】好人阵营胜利。",
                    Tips = "💡 禁言关键位置，打乱狼人节奏！"
                }},
                { "隐狼", new RoleDetail
                {
                    Name = "🫥 隐狼",
                    Description = "隐藏于狼群之外，狼队友不相认。",
                    Detail = @"
【技能】属于狼人阵营，但其他狼人看不到你，你也看不到其他狼人。
【规则】夜间不参与狼人刀人，被预言家查验为好人。
【胜利条件】狼人阵营胜利。",
                    Tips = "💡 隐狼是狼人阵营的暗子！"
                }},
                { "平民", new RoleDetail
                {
                    Name = "👤 平民",
                    Description = "无特殊技能，通过发言投票找出狼人。",
                    Detail = @"
【技能】无特殊技能。
【规则】通过发言和投票帮助好人阵营获胜。
【胜利条件】好人阵营胜利。",
                    Tips = "💡 你的发言和投票非常重要！"
                }}
            };
        }

        /// <summary>
        /// 双身份狼人杀规则（密室大逃脱8 / 开心狼人 双身份玩法）
        /// </summary>
        public static List<RulePage> GetDoubleIdentityRules(int playerCount)
        {
            return new List<RulePage>
            {
                new RulePage
                {
                    Title = "🃏 双身份 · 核心规则",
                    Icon = "🃏",
                    Order = 1,
                    Content = new List<string>
                    {
                        "每位玩家拥有【两张身份牌】（上身份 + 下身份），顺序由系统随机分配。",
                        "上身份为【显性】身份，下身份为【隐性】身份。",
                        "只有上身份出局后，下身份才会变为显性；两张身份都出局才算真正淘汰。",
                        "",
                        $"📊 当前配置：{playerCount} 人双身份局"
                    },
                    AdditionalInfo = "⚡ 一个玩家 = 两条命 = 两套技能"
                },
                new RulePage
                {
                    Title = "🧭 阵营判定",
                    Icon = "🧭",
                    Order = 2,
                    Content = new List<string>
                    {
                        "🐺 两张牌中【含有狼人牌】→ 狼人阵营。",
                        "🌟 两张牌中【都没有狼人牌】→ 好人阵营。",
                        "👤 两张牌【都是村民】→ 纯血好人（金宝宝）。",
                        "✨ 含有神职牌的好人 → 神民。"
                    },
                    AdditionalInfo = "💡 阵营只看两张牌里有没有狼"
                },
                new RulePage
                {
                    Title = "🏆 胜利条件",
                    Icon = "🏆",
                    Order = 3,
                    Content = new List<string>
                    {
                        "🐺 狼人阵营胜利：所有【纯血村民（金宝宝）】出局，或所有【神民】出局（屠边）。",
                        "🌟 好人阵营胜利：所有【狼人阵营】玩家（含狼身份）出局。",
                        "☀️ 特殊：连续三晚平安夜，好人阵营胜利（密室大逃脱8 规则）。"
                    },
                    AdditionalInfo = "⚡ 屠边：清掉神民或金宝宝任一即可"
                },
                new RulePage
                {
                    Title = "🌙 特殊规则",
                    Icon = "🌙",
                    Order = 4,
                    Content = new List<string>
                    {
                        "🔮 预言家只能查验【上身份】，得到该身份的好/狼反馈。",
                        "🐺 狼人身份不在同一层时，狼队友【不能相认】。",
                        "🛡️ 神职牌在【隐性身份】时不能使用技能（无需睁眼）。",
                        "💀 只要一个身份出局，就有遗言。",
                        "🧪 女巫不能同时使用解药和毒药。"
                    },
                    AdditionalInfo = "⚡ 双身份的信息差是核心乐趣"
                },
                new RulePage
                {
                    Title = "🃏 角色配置（密室大逃脱8 版）",
                    Icon = "🃏",
                    Order = 5,
                    Content = new List<string>
                    {
                        "神职：预言家、守卫、女巫、骑士",
                        "功能：盗贼、禁言长老",
                        "狼队：小狼 ×2、隐狼 ×1",
                        "平民：其余为村民牌"
                    },
                    AdditionalInfo = "💡 每张身份牌独立睁眼结算"
                }
            };
        }
    }

    public class RulePage
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<string> Content { get; set; } = new();
        public string AdditionalInfo { get; set; } = string.Empty;
    }

    public class RoleDetail
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string Tips { get; set; } = string.Empty;
    }
}
