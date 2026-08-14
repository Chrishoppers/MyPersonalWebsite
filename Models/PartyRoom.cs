using System;
using System.Collections.Generic;

namespace MyPersonalWebsite.Models
{
    public class PartyRoom
    {
        public string RoomId { get; set; } = string.Empty;
        public string HostUserId { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public int MaxPlayers { get; set; } = 20;
        public int MinPlayers { get; set; } = 2;
        public string Status { get; set; } = "waiting"; // waiting | ready | playing | ended
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public List<PartyPlayer> Players { get; set; } = new();
        public List<string> AdminUserIds { get; set; } = new();
        public string? Password { get; set; }
        public bool IsPublic { get; set; } = true;
        public string GameMode { get; set; } = "verify";
        public int CurrentLevel { get; set; } = 1;
    }
}
