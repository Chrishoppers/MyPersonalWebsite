using System;
using System.Collections.Generic;

namespace MyPersonalWebsite.Models
{
    public class HxdaPlayer
    {
        public string Id { get; set; } = "";
        public string VisitorName { get; set; } = "";
        public string RealName { get; set; } = "";
        public string SignatureImage { get; set; } = "";
        public string SignatureDate { get; set; } = "";
        public bool AdminLoggedIn { get; set; } = false;
        public string OnlineOverride { get; set; } = "";
        public int Attention { get; set; } = 0;
        public int RefreshCount { get; set; } = 0;
        public int TabSwitches { get; set; } = 0;
        public int SearchCount { get; set; } = 0;
        public string NodeRead { get; set; } = "[]";
        public string PmMessages { get; set; } = "[]";
        public string EndingsSeen { get; set; } = "[]";
        public string CurrentPage { get; set; } = "";
        public bool GatePassed { get; set; } = false;
        public string GateSignatureName { get; set; } = "";
        public bool IsOnline { get; set; } = false;
        public string LastSeenAt { get; set; } = "";
        public string CreatedAt { get; set; } = "";

        /* 安全版 IsOnlineNow：任何异常都返回 false */
        public bool IsOnlineNow
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(LastSeenAt)) return false;
                    DateTime t;
                    if (DateTime.TryParse(LastSeenAt, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out t))
                    {
                        var diff = (DateTime.UtcNow - t.ToUniversalTime()).TotalSeconds;
                        return diff >= 0 && diff < 60;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public List<string> NodeReadList
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(NodeRead)) return new List<string>();
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(NodeRead) ?? new List<string>();
                }
                catch { return new List<string>(); }
            }
        }

        public List<string> EndingsSeenList
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(EndingsSeen)) return new List<string>();
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(EndingsSeen) ?? new List<string>();
                }
                catch { return new List<string>(); }
            }
        }

        public List<HxdaPmMessage> PmMessagesList
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(PmMessages)) return new List<HxdaPmMessage>();
                    return System.Text.Json.JsonSerializer.Deserialize<List<HxdaPmMessage>>(PmMessages) ?? new List<HxdaPmMessage>();
                }
                catch { return new List<HxdaPmMessage>(); }
            }
        }
    }

    public class HxdaPmMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public string Time { get; set; } = "";
    }

    public class HxdaEvent
    {
        public int Id { get; set; } = 0;
        public string PlayerId { get; set; } = "";
        public string EventType { get; set; } = "";
        public string EventData { get; set; } = "";
        public string CreatedAt { get; set; } = "";
    }

    public class HxdaIntervention
    {
        public int Id { get; set; } = 0;
        public string PlayerId { get; set; } = "";
        public string Type { get; set; } = "";
        public string Payload { get; set; } = "";
        public bool Active { get; set; } = false;
        public bool Consumed { get; set; } = false;
        public string CreatedAt { get; set; } = "";
    }

    public class HxdaSearchRecord
    {
        public string PlayerId { get; set; } = "";
        public string VisitorName { get; set; } = "";
        public string RealName { get; set; } = "";
        public string CurrentPage { get; set; } = "";
        public string Word { get; set; } = "";
        public string Time { get; set; } = "";
        public string CreatedAt { get; set; } = "";
    }

    public class HxdaPmRecord
    {
        public string PlayerId { get; set; } = "";
        public string VisitorName { get; set; } = "";
        public string RealName { get; set; } = "";
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public string Time { get; set; } = "";
    }
}