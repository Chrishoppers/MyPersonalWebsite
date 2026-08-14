using System;
using System.Collections.Generic;

namespace MyPersonalWebsite.Models.Werewolf
{
    /// <summary>
    /// 狼人杀游戏状态
    /// </summary>
    public class WerewolfGameState
    {
        public string RoomId { get; set; } = string.Empty;
        public GamePhase Phase { get; set; } = GamePhase.Setup;
        public int Day { get; set; } = 0;
        public int Night { get; set; } = 0;
        public int SheriffId { get; set; } = -1;
        public List<WerewolfPlayer> Players { get; set; } = new();
        public List<NightActionLog> NightActions { get; set; } = new();
        public List<SpeechLog> SpeechLogs { get; set; } = new();
        public List<VoteRecord> VoteRecords { get; set; } = new();
        public bool IsGameOver { get; set; }
        public string Winner { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        // 快捷属性
        public List<WerewolfPlayer> AlivePlayers => Players.FindAll(p => p.IsAlive);
        public List<WerewolfPlayer> DeadPlayers => Players.FindAll(p => !p.IsAlive);
        public List<WerewolfPlayer> Werewolves => Players.FindAll(p => p.Role == RoleType.Werewolf && p.IsAlive);
        public List<WerewolfPlayer> GoodPlayers => Players.FindAll(p => p.Role != RoleType.Werewolf && p.IsAlive);
        public List<WerewolfPlayer> Villagers => Players.FindAll(p => p.Role == RoleType.Villager && p.IsAlive);
        public List<WerewolfPlayer> Gods => Players.FindAll(p => p.Role != RoleType.Werewolf && p.Role != RoleType.Villager && p.IsAlive);
        public WerewolfPlayer? GetPlayer(int seatNumber) => Players.Find(p => p.SeatNumber == seatNumber);
    }

    /// <summary>
    /// 玩家
    /// </summary>
    public class WerewolfPlayer
    {
        public int SeatNumber { get; set; }
        public string PlayerId { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string AvatarEmoji { get; set; } = "🧑";
        public string ConnectionId { get; set; } = string.Empty;
        public bool IsAlive { get; set; } = true;
        public bool IsOnline { get; set; } = true;
        public bool IsSpectator { get; set; } = false;
        public RoleType Role { get; set; } = RoleType.Villager;
        public bool HasRevealed { get; set; } = false; // 是否已查看身份
        public bool IsSheriff { get; set; } = false;
        public bool IsReady { get; set; } = false;

        // 技能状态
        public bool GuardProtected { get; set; } = false; // 是否被守卫守护
        public int GuardProtectedNight { get; set; } = -1;
        public bool IsWitchSaved { get; set; } = false;
        public bool IsPoisoned { get; set; } = false;
        public bool IsHunterCanShoot { get; set; } = true;
        public bool IsFoolSkillUsed { get; set; } = false; // 白痴技能是否已用
        public bool HasVoted { get; set; } = false;
        public int VoteTarget { get; set; } = -1;

        // 身份验证
        public bool IsGood => Role != RoleType.Werewolf;
        public bool IsWerewolf => Role == RoleType.Werewolf;
        public string RoleDisplay => Role.ToString();
        public string RoleIcon => GetRoleIcon(Role);

        private string GetRoleIcon(RoleType role)
        {
            return role switch
            {
                RoleType.Villager => "👤",
                RoleType.Werewolf => "🐺",
                RoleType.Seer => "🔮",
                RoleType.Witch => "🧪",
                RoleType.Guard => "🛡️",
                RoleType.Hunter => "🔫",
                RoleType.Fool => "🤡",
                RoleType.Knight => "⚔️",
                _ => "❓"
            };
        }
    }

    /// <summary>
    /// 角色类型
    /// </summary>
    public enum RoleType
    {
        Villager,    // 平民
        Werewolf,    // 狼人
        Seer,        // 预言家
        Witch,       // 女巫
        Guard,       // 守卫
        Hunter,      // 猎人
        Fool,        // 白痴
        Knight       // 骑士
    }

    /// <summary>
    /// 游戏阶段
    /// </summary>
    public enum GamePhase
    {
        Setup,           // 设置
        Seating,         // 就坐
        Dealing,         // 发牌
        Revealing,       // 查看身份
        NightGuard,      // 守卫行动
        NightSeer,       // 预言家行动
        NightWerewolf,   // 狼人行动
        NightWitch,      // 女巫行动
        NightResolve,    // 结算死亡
        DayAnnounce,     // 公布死讯
        DaySpeech,       // 发言
        DayVoting,       // 投票
        DayResolve,      // 公布放逐
        GameOver         // 游戏结束
    }

    /// <summary>
    /// 夜间行动日志
    /// </summary>
    public class NightActionLog
    {
        public int NightNumber { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public int ActorSeat { get; set; }
        public int TargetSeat { get; set; }
        public string Result { get; set; } = string.Empty;
        public DateTime ActionTime { get; set; }
    }

    /// <summary>
    /// 发言日志
    /// </summary>
    public class SpeechLog
    {
        public int SeatNumber { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SpeechTime { get; set; }
        public int DurationSeconds { get; set; }
    }

    /// <summary>
    /// 投票记录
    /// </summary>
    public class VoteRecord
    {
        public int Round { get; set; }
        public int VoterSeat { get; set; }
        public int TargetSeat { get; set; }
        public bool IsSheriffVote { get; set; }
    }
}
