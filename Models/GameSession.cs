namespace MyPersonalWebsite.Models
{
    public class GameSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TotalScore { get; set; }
        public int FinalScore { get; set; }
        public int PassedCount { get; set; }
        public int MaxCombo { get; set; }
        public int CheatCount { get; set; }
        public bool MicEnabled { get; set; } = true;
        public bool CamEnabled { get; set; } = true;
        public int PenaltyMic { get; set; } = 8;
        public int PenaltyCam { get; set; } = 5;
        public bool IsCompleted { get; set; }
        public string Status { get; set; } = "playing";
        
        // 导航属性
        public User? User { get; set; }
        public List<GameAnswerLog> AnswerLogs { get; set; } = new();
    }
}
