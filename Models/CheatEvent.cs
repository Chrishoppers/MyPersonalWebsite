namespace MyPersonalWebsite.Models
{
    public class CheatEvent
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? EventDetail { get; set; }
        public DateTime DetectedAt { get; set; }
        public int PenaltyAmount { get; set; } = 5;
    }
}
