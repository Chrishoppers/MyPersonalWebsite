using System;
using System.Collections.Generic;
using System.Linq;

namespace MyPersonalWebsite.Models.Werewolf
{
    public enum GamePhase
    {
        Setup,           // 等待玩家
        Seating,         // 就坐完成
        Dealing,         // 发牌
        Revealing,       // 查看身份
        SheriffElection, // 警长竞选
        NightGuard,      // 守卫行动
        NightSeer,       // 预言家行动
        NightWerewolf,   // 狼人行动
        NightWitch,      // 女巫行动
        NightResolve,    // 结算夜晚
        DayAnnounce,     // 宣布死亡
        DaySpeech,       // 白天发言
        DayVoting,       // 白天投票
        DayPK,           // PK投票
        DayResolve,      // 结算白天
        HunterShoot,     // 猎人开枪
        GameOver         // 游戏结束
    }

    public enum RoleType
    {
        Villager,      // 村民
        Werewolf,      // 狼人
        Seer,          // 预言家
        Witch,         // 女巫
        Guard,         // 守卫
        Hunter,        // 猎人
        Fool,          // 白痴
        Knight,        // 骑士
        WolfKing,      // 狼王（狼人阵营，死亡时可带走一人）
        Elder,         // 长老（首次被狼人袭击不死亡）
        Cupid,         // 丘比特（首夜指定情侣）
        Thief,         // 盗贼（首夜替换身份牌）
        SilenceElder,  // 禁言长老（白天可禁言一名玩家）
        HiddenWolf,    // 隐狼（狼人阵营，狼队友不相认，验人为好人）
        Bomber,        // 炸弹人（被放逐时随机带走一名投自己的玩家）
        WolfBeauty,    // 狼美人（狼人阵营，夜晚可魅惑一名玩家）
        WhiteWolfKing, // 白狼王（狼人阵营，死亡可开枪）
        OldRogue,      // 老流氓（神民，可免疫一次放逐）
        BearTamer,     // 训熊师（神民，夜晚可查验相邻玩家是否有狼）
        WildChild      // 野孩子（神民，首夜可认一名榜样，榜样出局则变狼）
    }

    public class WerewolfPlayer
    {
        public int SeatNumber { get; set; }
        public bool IsAI { get; set; } = false;  // ⭐ 新增
        public string PlayerId { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string AvatarEmoji { get; set; } = "🧑";
        public string ConnectionId { get; set; } = string.Empty;
        public bool IsAlive { get; set; } = true;
        public bool IsSpectator { get; set; } = false;
        public bool IsReady { get; set; } = false;
        public bool IsOnline { get; set; } = true;
        public bool HasRevealed { get; set; } = false;
        public bool HasConfirmedRules { get; set; } = false;
        public bool HasSurvivedFirstAttack { get; set; } = false;   // 长老首刀免死标记
        public bool HasUsedOldRogueImmunity { get; set; } = false;  // 老流氓免疫放逐标记
        public RoleType LowerRole { get; set; } = RoleType.Villager; // 双身份：下身份（第二张牌）
        public RoleType Role { get; set; } = RoleType.Villager;
        public bool IsHost { get; set; } = false;
public int CheatCount { get; set; } = 0;
        
        // 特殊身份标记
        public bool IsSheriff { get; set; } = false;
        public bool IsFoolSkillUsed { get; set; } = false;
        public bool IsHunterCanShoot { get; set; } = true;
        
        // 夜晚状态
        public bool IsGuardProtected { get; set; } = false;
        public int GuardProtectedNight { get; set; } = 0;
        public bool IsWitchSaved { get; set; } = false;
        public bool IsPoisoned { get; set; } = false;

        // 辅助属性
        public bool IsWerewolf => Role == RoleType.Werewolf || Role == RoleType.WolfKing || Role == RoleType.HiddenWolf ||
                                  Role == RoleType.WolfBeauty || Role == RoleType.WhiteWolfKing;
        // 双身份：两张牌中任一为狼牌即属狼人阵营
        public bool IsWolfCamp => IsWerewolf ||
                                  LowerRole == RoleType.Werewolf || LowerRole == RoleType.WolfKing || LowerRole == RoleType.HiddenWolf ||
                                  LowerRole == RoleType.WolfBeauty || LowerRole == RoleType.WhiteWolfKing;
        public bool IsGod => Role == RoleType.Seer || Role == RoleType.Witch || 
                             Role == RoleType.Guard || Role == RoleType.Hunter || 
                             Role == RoleType.Fool || Role == RoleType.Knight ||
                             Role == RoleType.Elder || Role == RoleType.Cupid ||
                             Role == RoleType.Thief || Role == RoleType.SilenceElder ||
                             Role == RoleType.Bomber || Role == RoleType.OldRogue ||
                             Role == RoleType.BearTamer || Role == RoleType.WildChild;
        public bool IsVillager => Role == RoleType.Villager;
    }

    public class WerewolfGameState
    {
        public string RoomId { get; set; } = string.Empty;
        public GamePhase Phase { get; set; } = GamePhase.Setup;
        public int Day { get; set; } = 0;
        public int Night { get; set; } = 0;
        public DateTime StartedAt { get; set; }
        public bool IsGameOver { get; set; } = false;
        public string? Winner { get; set; } = null;
        
        // 警长相关
        public bool IsSheriffElection { get; set; } = false;
        public int SheriffId { get; set; } = -1;
        
        // 玩家列表
        public List<WerewolfPlayer> Players { get; set; } = new();
        
        // 速度控制
        public double SpeedMultiplier { get; set; } = 1.0;

        // 角色专属状态
        public int SilencedSeatNumber { get; set; } = -1;   // 禁言长老禁言的座位号
        public int LoverSeat1 { get; set; } = -1;           // 丘比特情侣座位 1
        public int LoverSeat2 { get; set; } = -1;           // 丘比特情侣座位 2
        public int CharmedSeatNumber { get; set; } = -1;    // 狼美人魅惑的座位号
        public int RoleModelSeat { get; set; } = -1;        // 野孩子的榜样座位号
        public List<string> SelectedRoles { get; set; } = new();  // 房主自选角色（中文名）
        public bool IsDoubleIdentity { get; set; } = false;       // 双身份模式开关

        // 辅助属性
        public int PlayerCount => Players.Count(p => !p.IsSpectator);
        public List<WerewolfPlayer> AlivePlayers => Players.Where(p => p.IsAlive && !p.IsSpectator).ToList();
        public List<WerewolfPlayer> Werewolves => AlivePlayers.Where(p => p.IsWerewolf).ToList();
        public List<WerewolfPlayer> VotingWerewolves => AlivePlayers.Where(p => p.IsWerewolf && p.Role != RoleType.HiddenWolf).ToList();
        public List<WerewolfPlayer> GoodPlayers => AlivePlayers.Where(p => !p.IsWerewolf).ToList();
        public List<WerewolfPlayer> Gods => AlivePlayers.Where(p => p.IsGod).ToList();
        public List<WerewolfPlayer> Villagers => AlivePlayers.Where(p => p.Role == RoleType.Villager).ToList();
    }
}
