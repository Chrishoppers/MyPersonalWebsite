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
        Villager,   // 村民
        Werewolf,   // 狼人
        Seer,       // 预言家
        Witch,      // 女巫
        Guard,      // 守卫
        Hunter,     // 猎人
        Fool,       // 白痴
        Knight      // 骑士（暂未实现）
    }

    public class WerewolfPlayer
    {
        public int SeatNumber { get; set; }
        public string PlayerId { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string AvatarEmoji { get; set; } = "🧑";
        public string ConnectionId { get; set; } = string.Empty;
        public bool IsAlive { get; set; } = true;
        public bool IsSpectator { get; set; } = false;
        public bool IsReady { get; set; } = false;
        public bool IsOnline { get; set; } = true;
        public bool HasRevealed { get; set; } = false;
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
        public bool IsWerewolf => Role == RoleType.Werewolf;
        public bool IsGod => Role == RoleType.Seer || Role == RoleType.Witch || 
                             Role == RoleType.Guard || Role == RoleType.Hunter || 
                             Role == RoleType.Fool || Role == RoleType.Knight;
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

        // 辅助属性
        public int PlayerCount => Players.Count(p => !p.IsSpectator);
        public List<WerewolfPlayer> AlivePlayers => Players.Where(p => p.IsAlive && !p.IsSpectator).ToList();
        public List<WerewolfPlayer> Werewolves => AlivePlayers.Where(p => p.Role == RoleType.Werewolf).ToList();
        public List<WerewolfPlayer> GoodPlayers => AlivePlayers.Where(p => p.Role != RoleType.Werewolf).ToList();
        public List<WerewolfPlayer> Gods => AlivePlayers.Where(p => p.IsGod).ToList();
        public List<WerewolfPlayer> Villagers => AlivePlayers.Where(p => p.Role == RoleType.Villager).ToList();
    }
}
