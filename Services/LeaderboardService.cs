using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Hubs;
using MyPersonalWebsite.Models;

namespace MyPersonalWebsite.Services
{
    /// <summary>
    /// 多人实时抢票排行榜：记录每位玩家任务结算，并按积分排序广播给所有客户端。
    /// </summary>
    public class LeaderboardService
    {
        private readonly IHubContext<TicketHub> _hub;
        private readonly object _lock = new();
        private readonly List<LeaderboardEntry> _entries = new();

        public LeaderboardService(IHubContext<TicketHub> hub) => _hub = hub;

        public object Submit(string name, int score, string rank, int coins, string difficulty, int timeSec)
        {
            lock (_lock)
            {
                _entries.Add(new LeaderboardEntry
                {
                    Name = string.IsNullOrWhiteSpace(name) ? "匿名玩家" : name.Trim(),
                    Score = score,
                    Rank = rank,
                    Coins = coins,
                    Difficulty = difficulty,
                    TimeSec = timeSec,
                    SubmittedAt = DateTime.Now
                });

                var top = _entries
                    .OrderByDescending(e => e.Score)
                    .ThenBy(e => e.TimeSec)
                    .ThenByDescending(e => e.SubmittedAt)
                    .Take(100)
                    .ToList();
                _entries.Clear();
                _entries.AddRange(top);

                var dto = top.Select(e => new { e.Name, e.Score, e.Rank, e.Coins, e.Difficulty, e.TimeSec }).ToList();
                _ = _hub.Clients.All.SendAsync("leaderboardUpdated", dto);
                return new { ok = true, leaderboard = dto };
            }
        }

        public object Get()
        {
            lock (_lock)
                return _entries
                    .OrderByDescending(e => e.Score)
                    .ThenBy(e => e.TimeSec)
                    .Select(e => new { e.Name, e.Score, e.Rank, e.Coins, e.Difficulty, e.TimeSec }).ToList();
        }
    }
}
