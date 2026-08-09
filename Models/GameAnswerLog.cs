namespace MyPersonalWebsite.Models
{
    public class GameAnswerLog
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int Level { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime SubmitTime { get; set; }
        public double ElapsedSeconds { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsTimeout { get; set; }
        public bool CheatDetected { get; set; }
        public string? CheatReason { get; set; }
        public int PointsEarned { get; set; }
        public int PenaltyApplied { get; set; }
    }
}
