namespace MyPersonalWebsite.Models
{
    /// <summary>
    /// 多人实时抢票排行榜条目。
    /// </summary>
    public class LeaderboardEntry
    {
        public string Name { get; set; } = "";
        public int Score { get; set; }
        public string Rank { get; set; } = "";
        public int Coins { get; set; }
        public string Difficulty { get; set; } = "";
        public int TimeSec { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
