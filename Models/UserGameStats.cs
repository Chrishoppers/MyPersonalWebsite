using System;

namespace MyPersonalWebsite.Models
{
    public class UserGameStats
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TotalPoints { get; set; } = 0;
        public int MaxCombo { get; set; } = 0;
        public int MaxLevel { get; set; } = 0;
        public int GamesPlayed { get; set; } = 0;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public User? User { get; set; }
    }
}
