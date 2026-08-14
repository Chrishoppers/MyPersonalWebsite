using System;
using System.Collections.Generic;

namespace MyPersonalWebsite.Models.Werewolf
{
    public class WerewolfGameState
    {
        public string RoomId { get; set; } = string.Empty;
        public GamePhase Phase { get; set; } = GamePhase.Setup;
        public int Day { get; set; } = 0;
        public int Night { get; set; } = 0;
        public int SheriffId { get; set; } = -1;
        public bool IsSheriffElection { get; set; } = false;
        public bool IsGameOver { get; set; } = false;
        public string Winner { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsPaused { get; set; } = false;
        public double SpeedMultiplier { get; set; } = 1.0;
        public List<WerewolfPlayer> Players { get; set; } = new();
        public List<NightActionLog> NightActions { get; set; } = new();
        public List<VoteRecord> VoteRecords { get; set; } = new();
        public List<GameLog> GameLogs { get; set; } = new();

        public List<WerewolfPlayer> AlivePlayers => Players.FindAll(p => p.IsAlive && !p.IsSpectator);
        public List<WerewolfPlayer> DeadPlayers => Players.FindAll(p => !p.IsAlive && !p.IsSpectator);
        public List<WerewolfPlayer> Werewolves => Players.FindAll(p => p.Role == RoleType.Werewolf && p.IsAlive && !p.IsSpectator);
        public List<WerewolfPlayer> GoodPlayers => Players.FindAll(p => p.Role != RoleType.Werewolf && p.IsAlive && !p.IsSpectator);
        public List<WerewolfPlayer> Gods => Players.FindAll(p => p.IsGod && p.IsAlive && !p.IsSpectator);
        public List<WerewolfPlayer> Villagers => Players.FindAll(p => p.Role == RoleType.Villager && p.IsAlive && !p.IsSpectator);
        public WerewolfPlayer? GetPlayer(int seatNumber) => Players.Find(p => p.SeatNumber == seatNumber);
        public int PlayerCount => Players.Count(p => !p.IsSpectator);
    }

    public enum GamePhase
    {
        Setup, Seating, Dealing, Revealing, SheriffElection,
        NightGuard, NightSeer, NightWerewolf, NightWitch, NightResolve,
        DayAnnounce, DaySpeech, DayVoting, DayPK, DayResolve,
        HunterShoot, GameOver
    }

    public enum RoleType
    {
        Villager, Werewolf, Seer, Witch, Guard, Hunter, Fool, Knight
    }

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
        public bool HasRevealed { get; set; } = false;
        public bool IsSheriff { get; set; } = false;
        public bool IsReady { get; set; } = false;

        // 技能状态
        public bool IsGuardProtected { get; set; } = false;
        public int GuardProtectedNight { get; set; } = -1;
        public bool IsWitchSaved { get; set; } = false;
        public bool IsPoisoned { get; set; } = false;
        public bool IsHunterCanShoot { get; set; } = true;
        public bool IsFoolSkillUsed { get; set; } = false;
        public bool HasVoted { get; set; } = false;
        public int VoteTarget { get; set; } = -1;

        public bool IsGood => Role != RoleType.Werewolf;
        public bool IsWerewolf => Role == RoleType.Werewolf;
        public bool IsGod => Role != RoleType.Werewolf && Role != RoleType.Villager;
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

    public class NightActionLog
    {
        public int NightNumber { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public int ActorSeat { get; set; }
        public int TargetSeat { get; set; }
        public string Result { get; set; } = string.Empty;
        public DateTime ActionTime { get; set; }
    }

    public class VoteRecord
    {
        public int Round { get; set; }
        public int VoterSeat { get; set; }
        public int TargetSeat { get; set; }
        public bool IsSheriffVote { get; set; }
        public bool IsPKVote { get; set; }
    }

    public class GameLog
    {
        public DateTime Time { get; set; }
        public string Event { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }
}
