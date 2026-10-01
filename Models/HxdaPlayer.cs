using System;
using System.Collections.Generic;

namespace MyPersonalWebsite.Models
{
    public class HxdaPlayer
    {
        public string? Id { get; set; }
        public string? VisitorName { get; set; }
        public string? RealName { get; set; }
        public string? SignatureImage { get; set; }
        public string? SignatureDate { get; set; }
        public bool AdminLoggedIn { get; set; } = false;
        public string? OnlineOverride { get; set; }
        public int Attention { get; set; } = 0;
        public int RefreshCount { get; set; } = 0;
        public int TabSwitches { get; set; } = 0;
        public int SearchCount { get; set; } = 0;
        public string? NodeRead { get; set; } = "[]";
        public string? PmMessages { get; set; } = "[]";
        public string? EndingsSeen { get; set; } = "[]";
        public string? CurrentPage { get; set; }
        public bool GatePassed { get; set; } = false;
        public string? GateSignatureName { get; set; }
        public bool IsOnline { get; set; } = false;
        public string? LastSeenAt { get; set; }
        public string? CreatedAt { get; set; }

        public bool IsOnlineNow
        {
            get
            {
                if (string.IsNullOrEmpty(LastSeenAt)) return false;
                if (DateTime.TryParse(LastSeenAt, out var t))
                    return (DateTime.UtcNow - t).TotalSeconds < 60;
                return false;
            }
        }

        public List<string> NodeReadList
        {
            get
            {
                try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(NodeRead ?? "[]") ?? new(); }
                catch { return new(); }
            }
        }

        public List<string> EndingsSeenList
        {
            get
            {
                try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(EndingsSeen ?? "[]") ?? new(); }
                catch { return new(); }
            }
        }

        public List<HxdaPmMessage> PmMessagesList
        {
            get
            {
                try { return System.Text.Json.JsonSerializer.Deserialize<List<HxdaPmMessage>>(PmMessages ?? "[]") ?? new(); }
                catch { return new(); }
            }
        }
    }

    public class HxdaPmMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
        public string? Time { get; set; }
    }

    public class HxdaEvent
    {
        public int Id { get; set; }
        public string? PlayerId { get; set; }
        public string? EventType { get; set; }
        public string? EventData { get; set; }
        public string? CreatedAt { get; set; }
    }

    public class HxdaIntervention
    {
        public int Id { get; set; }
        public string? PlayerId { get; set; }
        public string? Type { get; set; }
        public string? Payload { get; set; }
        public bool Active { get; set; }
        public bool Consumed { get; set; }
        public string? CreatedAt { get; set; }
    }

    public class HxdaSearchRecord
    {
        public string? PlayerId { get; set; }
        public string? VisitorName { get; set; }
        public string? RealName { get; set; }
        public string? CurrentPage { get; set; }
        public string? Word { get; set; }
        public string? Time { get; set; }
        public string? CreatedAt { get; set; }
    }

    public class HxdaPmRecord
    {
        public string? PlayerId { get; set; }
        public string? VisitorName { get; set; }
        public string? RealName { get; set; }
        public string? Role { get; set; }
        public string? Content { get; set; }
        public string? Time { get; set; }
    }
}