namespace MyPersonalWebsite.Models
{
    public class DifficultyConfig
    {
        public string Id { get; set; } = "normal";
        public string Label { get; set; } = "普通";
        public int Bots { get; set; } = 3;
        public int BotIntervalMs { get; set; } = 650;
        public int ReleaseIntervalMs { get; set; } = 600;
        public int ReleaseBatch { get; set; } = 3;
        public int CaptchaIntervalMs { get; set; } = 9000;
        public int DurationSeconds { get; set; } = 50;
        public int Target { get; set; } = 12;
        public int SeatRows { get; set; } = 8;
        public int SeatCols { get; set; } = 6;

        public static DifficultyConfig For(string id) => id switch
        {
            "easy" => new DifficultyConfig { Id = "easy", Label = "简单", Bots = 2, BotIntervalMs = 950, ReleaseIntervalMs = 720, ReleaseBatch = 2, CaptchaIntervalMs = 12000, DurationSeconds = 60, Target = 8 },
            "hard" => new DifficultyConfig { Id = "hard", Label = "困难", Bots = 4, BotIntervalMs = 460, ReleaseIntervalMs = 500, ReleaseBatch = 4, CaptchaIntervalMs = 7000, DurationSeconds = 45, Target = 16 },
            "hell" => new DifficultyConfig { Id = "hell", Label = "地狱", Bots = 5, BotIntervalMs = 310, ReleaseIntervalMs = 400, ReleaseBatch = 5, CaptchaIntervalMs = 5200, DurationSeconds = 40, Target = 20 },
            // 闯关战役 1-10 关
            "l1" => Level(1, 1, 1100, 800, 2, 15000, 45, 5),
            "l2" => Level(2, 2, 950, 720, 2, 13000, 45, 6),
            "l3" => Level(3, 2, 820, 650, 3, 11000, 45, 7),
            "l4" => Level(4, 3, 700, 600, 3, 10000, 45, 8),
            "l5" => Level(5, 3, 620, 560, 3, 9000, 45, 9),
            "l6" => Level(6, 4, 540, 520, 4, 8000, 45, 10),
            "l7" => Level(7, 4, 480, 490, 4, 7000, 45, 12),
            "l8" => Level(8, 5, 420, 460, 4, 6000, 45, 14),
            "l9" => Level(9, 5, 360, 430, 5, 5200, 45, 16),
            "l10" => new DifficultyConfig { Id = "l10", Label = "Boss·黄牛之王", Bots = 6, BotIntervalMs = 300, ReleaseIntervalMs = 400, ReleaseBatch = 5, CaptchaIntervalMs = 4500, DurationSeconds = 45, Target = 20 },
            _ => new DifficultyConfig { Id = "normal", Label = "普通", Bots = 3, BotIntervalMs = 650, ReleaseIntervalMs = 600, ReleaseBatch = 3, CaptchaIntervalMs = 9000, DurationSeconds = 50, Target = 12 }
        };

        private static DifficultyConfig Level(int n, int bots, int botInt, int relInt, int batch, int captcha, int dur, int target)
            => new()
            {
                Id = $"l{n}",
                Label = $"第{n}关",
                Bots = bots,
                BotIntervalMs = botInt,
                ReleaseIntervalMs = relInt,
                ReleaseBatch = batch,
                CaptchaIntervalMs = captcha,
                DurationSeconds = dur,
                Target = target
            };
    }

    public class GameSeat
    {
        public string Key { get; set; } = "";      // 例如 "3A"
        public int Row { get; set; }
        public string Col { get; set; } = "";
        public string Status { get; set; } = "locked"; // locked / open / player / bot
        public string Owner { get; set; } = "";
        public bool IsWindow => Col is "A" or "F";
    }

    public class GameBot
    {
        public string Name { get; set; } = "";
        public string Emoji { get; set; } = "🤖";
    }

    public class GrabGame
    {
        public string GameId { get; set; } = "";
        public string TrainNumber { get; set; } = "";
        public string FromStation { get; set; } = "";
        public string ToStation { get; set; } = "";
        public string SeatClass { get; set; } = "二等座";
        public string Difficulty { get; set; } = "normal";
        public string Mode { get; set; } = "timed";      // timed / endless
        public DifficultyConfig Config { get; set; } = new();
        public List<GameSeat> Seats { get; set; } = new();
        public List<GameBot> Bots { get; set; } = new();
        public List<string> NextWave { get; set; } = new(); // 下一波将放票的座位（雷达道具用）

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime EndTime { get; set; }
        public bool Active { get; set; }

        // 玩家战绩
        public int PlayerGrabbed { get; set; }
        public int BotGrabbed { get; set; }
        public int Score { get; set; }
        public int Combo { get; set; }
        public int BestCombo { get; set; }
        public int CaptchaPassed { get; set; }
        public int CaptchaFailed { get; set; }

        // 运行时控制
        public double BotSpeedMul { get; set; } = 1.0;
        public DateTime FreezeUntil { get; set; } = DateTime.MinValue;
        public DateTime SpeedUntil { get; set; } = DateTime.MinValue;
        public DateTime DoubleUntil { get; set; } = DateTime.MinValue;
        public DateTime RobotUntil { get; set; } = DateTime.MinValue;

        // 验证码
        public bool CaptchaActive { get; set; }
        public string CaptchaCode { get; set; } = "";
        public DateTime CaptchaDeadline { get; set; }
        public bool CaptchaAnswered { get; set; }

        // 并发与手感
        [System.Text.Json.Serialization.JsonIgnore]
        public object Lock { get; } = new();
        public DateTime LastReleaseAt { get; set; } = DateTime.UtcNow;
        public bool Ended { get; set; }
    }
}
