using System;

namespace MyPersonalWebsite.Models
{
    public class VerifyGameStat
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TotalScore { get; set; } = 0;
        public int MaxCombo { get; set; } = 0;
        public int MaxLevel { get; set; } = 0;
        public int GamesPlayed { get; set; } = 0;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public User? User { get; set; }
    }
}
