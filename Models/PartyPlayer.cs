using System;

namespace MyPersonalWebsite.Models
{
    public class PartyPlayer
    {
        public string PlayerId { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? AvatarEmoji { get; set; } = "🧑";
        public bool IsReady { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime JoinedAt { get; set; }
        public int Score { get; set; }
        public int Combo { get; set; }
        public int PassedLevels { get; set; }
        public string Status { get; set; } = "online";
        public string? ConnectionId { get; set; }
        public DateTime? LastPing { get; set; }
        public bool IsHost { get; set; } // 是否是主控
    }
}
