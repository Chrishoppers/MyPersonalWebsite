using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Hubs;
using MyPersonalWebsite.Models;

namespace MyPersonalWebsite.Services
{
    /// <summary>
    /// 抢票大战引擎：服务端权威座位池 + AI 对手实时抢票 + 验证码 + 多模式 + 道具。
    /// 通过 SignalR 广播实时战况，保证多客户端看到的座位状态一致。
    /// </summary>
    public class GrabGameService
    {
        private readonly IHubContext<TicketHub> _hub;
        private readonly object _gameLock = new();
        private CancellationTokenSource? _cts;
        private GrabGame? _game;

        private static readonly (string Name, string Emoji)[] BotPool =
        {
            ("抢票王·阿伟", "🦾"), ("黄牛·老张", "🐂"), ("手速帝·小美", "⚡"),
            ("脚本侠·阿强", "🤖"), ("五姑娘·婷婷", "🎯"), ("剁手党·大壮", "💪"),
            ("黑科技·老王", "🕶️"), ("鬼才·小明", "👾")
        };

        public GrabGameService(IHubContext<TicketHub> hub) => _hub = hub;

        public GrabGame? Current
        {
            get { lock (_gameLock) return _game; }
        }

        // ---------- 启动 ----------
        public GrabGame Start(string difficulty, string? mode, string? trainNumber, string? from, string? to, string? seatClass)
        {
            lock (_gameLock)
            {
                StopLocked();
                var cfg = DifficultyConfig.For(difficulty);
                bool endless = mode == "endless";
                var g = new GrabGame
                {
                    GameId = Guid.NewGuid().ToString("N")[..8],
                    TrainNumber = string.IsNullOrWhiteSpace(trainNumber) ? "G1001" : trainNumber,
                    FromStation = string.IsNullOrWhiteSpace(from) ? "出发站" : from,
                    ToStation = string.IsNullOrWhiteSpace(to) ? "到达站" : to,
                    Difficulty = cfg.Id,
                    Mode = endless ? "endless" : "timed",
                    Config = cfg,
                    SeatClass = string.IsNullOrWhiteSpace(seatClass) ? "二等座" : seatClass,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddSeconds(endless ? 60 : cfg.DurationSeconds),
                    Active = true
                };

                // 座位池：行×列，列 A-F，A/F 靠窗
                string[] cols = { "A", "B", "C", "D", "E", "F" };
                for (int r = 1; r <= cfg.SeatRows; r++)
                    for (int c = 0; c < cfg.SeatCols; c++)
                        g.Seats.Add(new GameSeat { Key = $"{r}{cols[c]}", Row = r, Col = cols[c] });

                // 对手
                var shuffled = BotPool.OrderBy(_ => Random.Shared.Next()).Take(cfg.Bots).ToList();
                foreach (var b in shuffled)
                    g.Bots.Add(new GameBot { Name = b.Name, Emoji = b.Emoji });

                g.NextWave = PickNextWave(g);

                _game = g;
                _cts = new CancellationTokenSource();
                _ = Task.Run(() => LoopAsync(g, _cts.Token));
                return g;
            }
        }

        public void Stop()
        {
            lock (_gameLock) StopLocked();
        }

        private void StopLocked()
        {
            if (_game != null) _game.Active = false;
            try { _cts?.Cancel(); _cts?.Dispose(); } catch { }
            _cts = null;
        }

        // ---------- 玩家操作 ----------
        public object GrabSeat(string key)
        {
            var g = Current;
            if (g == null || !g.Active) return new { ok = false, reason = "noGame", msg = "当前没有进行中的抢票任务" };

            lock (g.Lock)
            {
                var seat = g.Seats.FirstOrDefault(s => s.Key == key);
                if (seat == null) return new { ok = false, reason = "badSeat", msg = "无效座位" };
                if (seat.Status == "player") return new { ok = false, reason = "yours", msg = "这个座位已经是你的了" };
                if (seat.Status == "bot")
                {
                    g.Combo = 0;
                    return new { ok = false, reason = "taken", msg = "手慢了，被对手抢走了", combo = g.Combo };
                }
                if (seat.Status == "locked")
                {
                    g.Combo = 0;
                    return new { ok = false, reason = "locked", msg = "还未放票，稍等", combo = g.Combo };
                }

                seat.Status = "player";
                seat.Owner = "你";
                g.PlayerGrabbed++;
                g.Combo++;
                if (g.Combo > g.BestCombo) g.BestCombo = g.Combo;

                bool fast = (DateTime.UtcNow - g.LastReleaseAt).TotalMilliseconds < 800;
                bool doubled = DateTime.UtcNow < g.DoubleUntil;
                int gained = 100 + g.Combo * 15 + (fast ? 50 : 0);
                if (doubled) gained *= 2;
                g.Score += gained;

                var result = new { ok = true, score = g.Score, combo = g.Combo, gained, fast, doubled, grabbed = g.PlayerGrabbed };
                _ = _hub.Clients.All.SendAsync("gameClaimed", new { key, owner = "你", by = "player" });
                return result;
            }
        }

        public object AnswerCaptcha(string code)
        {
            var g = Current;
            if (g == null || !g.Active || !g.CaptchaActive) return new { ok = false, msg = "当前无需验证" };
            lock (g.Lock)
            {
                bool correct = string.Equals(code, g.CaptchaCode, StringComparison.OrdinalIgnoreCase);
                return ResolveCaptchaLocked(g, correct);
            }
        }

        public object UsePowerup(string type)
        {
            var g = Current;
            if (g == null || !g.Active) return new { ok = false, msg = "当前没有进行中的抢票任务" };
            lock (g.Lock)
            {
                var now = DateTime.UtcNow;
                switch (type)
                {
                    case "freeze":
                        g.FreezeUntil = now.AddSeconds(3.5);
                        _ = _hub.Clients.All.SendAsync("gamePowerup", new { type = "freeze" });
                        return new { ok = true, type = "freeze", msg = "❄️ 对手被冻结 3.5 秒！" };
                    case "speed":
                        g.BotSpeedMul = 2.0;
                        g.SpeedUntil = now.AddSeconds(5);
                        _ = _hub.Clients.All.SendAsync("gamePowerup", new { type = "speed" });
                        return new { ok = true, type = "speed", msg = "⚡ 对手手速减半 5 秒！" };
                    case "radar":
                        return new { ok = true, type = "radar", keys = g.NextWave.ToList(), msg = "📡 已侦察下一波放票位置！" };
                    case "double":
                        g.DoubleUntil = now.AddSeconds(5);
                        _ = _hub.Clients.All.SendAsync("gamePowerup", new { type = "double" });
                        return new { ok = true, type = "double", msg = "💰 双倍积分 5 秒！" };
                    case "robot":
                        g.RobotUntil = now.AddSeconds(5);
                        _ = _hub.Clients.All.SendAsync("gamePowerup", new { type = "robot" });
                        return new { ok = true, type = "robot", msg = "🤖 机器人助手帮你抢票 5 秒！" };
                    default:
                        return new { ok = false, msg = "未知道具" };
                }
            }
        }

        // ---------- 游戏主循环 ----------
        private async Task LoopAsync(GrabGame g, CancellationToken ct)
        {
            var cfg = g.Config;
            var nextRelease = DateTime.UtcNow.AddMilliseconds(cfg.ReleaseIntervalMs);
            var nextBot = new DateTime[g.Bots.Count];
            for (int i = 0; i < nextBot.Length; i++)
                nextBot[i] = DateTime.UtcNow.AddMilliseconds(cfg.BotIntervalMs * (i + 1) * 0.6);
            var nextCaptcha = DateTime.UtcNow.AddMilliseconds(cfg.CaptchaIntervalMs);
            var nextRobot = DateTime.UtcNow;

            try
            {
                while (!ct.IsCancellationRequested && g.Active && DateTime.UtcNow < g.EndTime)
                {
                    await Task.Delay(50, ct);
                    var now = DateTime.UtcNow;

                    // 无尽模式：随时间推移对手越来越快
                    double ramp = 1.0;
                    if (g.Mode == "endless")
                    {
                        double total = (g.EndTime - g.StartTime).TotalSeconds;
                        double elapsed = total <= 0 ? 1 : (now - g.StartTime).TotalSeconds / total;
                        ramp = 1.0 - 0.5 * Math.Clamp(elapsed, 0, 1);
                    }

                    // 验证码超时
                    if (g.CaptchaActive && !g.CaptchaAnswered && now > g.CaptchaDeadline)
                    {
                        lock (g.Lock) if (g.CaptchaActive && !g.CaptchaAnswered) ResolveCaptchaLocked(g, false);
                    }

                    if (now >= nextRelease) { ReleaseWave(g); nextRelease = now.AddMilliseconds(cfg.ReleaseIntervalMs * ramp); }
                    if (now >= nextCaptcha) { TriggerCaptcha(g); nextCaptcha = now.AddMilliseconds(cfg.CaptchaIntervalMs); }

                    // 机器人助手自动抢票
                    if (now < g.RobotUntil && now >= nextRobot)
                    {
                        RobotGrab(g);
                        nextRobot = now.AddMilliseconds(500);
                    }

                    bool frozen = now < g.FreezeUntil;
                    double mul = (now < g.SpeedUntil) ? g.BotSpeedMul : 1.0;
                    for (int i = 0; i < g.Bots.Count; i++)
                    {
                        if (!frozen && now >= nextBot[i])
                        {
                            BotGrab(g, i);
                            nextBot[i] = now.AddMilliseconds(cfg.BotIntervalMs * mul * ramp * (0.7 + Random.Shared.NextDouble() * 0.6));
                        }
                    }

                    if (AllClaimed(g)) break;
                }
            }
            catch (OperationCanceledException) { }
            finally { EndGame(g); }
        }

        private static List<string> PickNextWave(GrabGame g)
        {
            var locked = g.Seats.Where(s => s.Status == "locked").ToList();
            int n = Math.Min(g.Config.ReleaseBatch, locked.Count);
            var keys = new List<string>(n);
            for (int i = 0; i < n; i++)
            {
                var s = locked[Random.Shared.Next(locked.Count)];
                keys.Add(s.Key);
                locked.Remove(s);
            }
            return keys;
        }

        private void ReleaseWave(GrabGame g)
        {
            lock (g.Lock)
            {
                if (!g.Active) return;
                var keys = new List<string>();
                foreach (var k in g.NextWave)
                {
                    var s = g.Seats.FirstOrDefault(x => x.Key == k && x.Status == "locked");
                    if (s != null) { s.Status = "open"; keys.Add(k); }
                }
                if (keys.Count == 0)
                {
                    var locked = g.Seats.Where(s => s.Status == "locked").ToList();
                    int n = Math.Min(g.Config.ReleaseBatch, locked.Count);
                    for (int i = 0; i < n; i++)
                    {
                        var s = locked[Random.Shared.Next(locked.Count)];
                        s.Status = "open";
                        keys.Add(s.Key);
                        locked.Remove(s);
                    }
                }
                g.NextWave = PickNextWave(g);
                if (keys.Count > 0)
                {
                    g.LastReleaseAt = DateTime.UtcNow;
                    _ = _hub.Clients.All.SendAsync("gameReleased", new { keys });
                }
            }
        }

        private void BotGrab(GrabGame g, int botIdx)
        {
            lock (g.Lock)
            {
                if (!g.Active) return;
                var open = g.Seats.Where(s => s.Status == "open").ToList();
                if (open.Count == 0) return;
                var s = open[Random.Shared.Next(open.Count)];
                s.Status = "bot";
                s.Owner = g.Bots[botIdx].Name;
                g.BotGrabbed++;
                _ = _hub.Clients.All.SendAsync("gameClaimed", new { key = s.Key, owner = s.Owner, by = "bot", bot = botIdx });
            }
        }

        private void RobotGrab(GrabGame g)
        {
            lock (g.Lock)
            {
                if (!g.Active) return;
                var open = g.Seats.Where(s => s.Status == "open").ToList();
                if (open.Count == 0) return;
                var s = open[Random.Shared.Next(open.Count)];
                s.Status = "player";
                s.Owner = "机器人";
                g.PlayerGrabbed++;
                g.Score += 120;
                _ = _hub.Clients.All.SendAsync("gameClaimed", new { key = s.Key, owner = "机器人", by = "robot" });
            }
        }

        private void TriggerCaptcha(GrabGame g)
        {
            lock (g.Lock)
            {
                if (!g.Active || g.CaptchaActive) return;
                g.CaptchaActive = true;
                g.CaptchaAnswered = false;
                g.CaptchaCode = Random.Shared.Next(1000, 10000).ToString();
                g.CaptchaDeadline = DateTime.UtcNow.AddSeconds(2.4);
                _ = _hub.Clients.All.SendAsync("gameCaptcha", new { code = g.CaptchaCode });
            }
        }

        private object ResolveCaptchaLocked(GrabGame g, bool correct)
        {
            g.CaptchaAnswered = true;
            g.CaptchaActive = false;
            if (correct)
            {
                g.CaptchaPassed++;
                g.Score += 150;
                var r = new { ok = true, bonus = 150, score = g.Score, penalty = 0 };
                _ = _hub.Clients.All.SendAsync("gameCaptchaResult", new { ok = true, bonus = 150, score = g.Score });
                return r;
            }
            else
            {
                g.CaptchaFailed++;
                g.Combo = 0;
                g.Score = Math.Max(0, g.Score - 80);
                // 惩罚：对手立刻抢走两个开放座位
                int stolen = 0;
                var open = g.Seats.Where(s => s.Status == "open").ToList();
                foreach (var s in open.Take(2))
                {
                    s.Status = "bot";
                    s.Owner = g.Bots[Random.Shared.Next(g.Bots.Count)].Name;
                    g.BotGrabbed++;
                    stolen++;
                    _ = _hub.Clients.All.SendAsync("gameClaimed", new { key = s.Key, owner = s.Owner, by = "bot" });
                }
                var r = new { ok = false, penalty = 80, score = g.Score, stolen };
                _ = _hub.Clients.All.SendAsync("gameCaptchaResult", new { ok = false, penalty = 80, score = g.Score, stolen });
                return r;
            }
        }

        private static bool AllClaimed(GrabGame g)
        {
            lock (g.Lock) return g.Seats.All(s => s.Status is "player" or "bot");
        }

        private void EndGame(GrabGame g)
        {
            lock (g.Lock)
            {
                if (g.Ended) return;
                g.Ended = true;
                g.Active = false;
            }
            // 仅当该局仍是当前局时才广播结算，避免旧局误伤新局
            lock (_gameLock) { if (!ReferenceEquals(_game, g)) return; }

            bool won;
            string rank;
            if (g.Mode == "endless")
            {
                won = true;
                rank = g.Score >= 2000 ? "S" : g.Score >= 1500 ? "A" : g.Score >= 1000 ? "B" : g.Score >= 500 ? "C" : "D";
            }
            else
            {
                won = g.PlayerGrabbed >= g.Config.Target;
                double ratio = (g.PlayerGrabbed + g.BotGrabbed) == 0 ? 0 : (double)g.PlayerGrabbed / (g.PlayerGrabbed + g.BotGrabbed);
                rank = !won ? (g.PlayerGrabbed > 0 ? "C" : "D")
                            : ratio >= 0.60 ? "S"
                            : ratio >= 0.45 ? "A"
                            : "B";
            }
            int coins = g.Score / 10 + (won ? 50 : 0);

            var payload = new
            {
                gameId = g.GameId,
                mode = g.Mode,
                won,
                grabbed = g.PlayerGrabbed,
                botGrabbed = g.BotGrabbed,
                target = g.Mode == "endless" ? 0 : g.Config.Target,
                score = g.Score,
                rank,
                combo = g.BestCombo,
                captchaPassed = g.CaptchaPassed,
                captchaFailed = g.CaptchaFailed,
                coins,
                seats = g.Seats.Select(s => new { key = s.Key, status = s.Status, owner = s.Owner }).ToList()
            };
            _ = _hub.Clients.All.SendAsync("gameOver", payload);
        }
    }
}
