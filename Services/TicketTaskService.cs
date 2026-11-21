using MyPersonalWebsite.Models;

namespace MyPersonalWebsite.Services
{
    /// <summary>
    /// 真实抢票任务引擎：下发拟真 12306 抢票任务（始发/终点城市、乘客需求、候选乘客、支付方式与密码），
    /// 对玩家提交的购票与支付进行多维度评分。
    /// </summary>
    public class TicketTaskService
    {
        private readonly TrainSystem _trainSystem;
        private readonly object _lock = new();
        private TicketTask? _task;

        private static readonly string[] Surnames = {
            "张","李","王","刘","陈","杨","赵","黄","周","吴","徐","孙","胡","朱","高","林","何","郭","马","罗",
            "梁","宋","郑","谢","韩","唐","冯","于","董","萧","程","曹","袁","邓","许","傅","沈","曾","彭","吕"
        };
        private static readonly string[] MaleNames = { "伟","强","磊","军","超","宇","鑫","鹏","龙","飞","峰","志","明","俊","凯","华","平","建","辉","浩","轩","博","哲","阳","晨" };
        private static readonly string[] FemaleNames = { "芳","娜","敏","静","丽","玲","婷","雪","睿","琪","琳","欣","雨","阳","慧","文","悦","颖","晓","云","洁","梦","丹","倩" };
        private static readonly string[] Relations = { "同事","邻居","表弟","同学","客户","朋友","亲戚","室友" };
        private static readonly string[] PayMethods = { "alipay", "wechat", "card", "unionpay", "ecard" };

        public TicketTaskService(TrainSystem trainSystem) => _trainSystem = trainSystem;

        public TicketTask? Current
        {
            get { lock (_lock) return _task; }
        }

        // ---------- 任务下发 ----------
        public object Start(string difficulty)
        {
            lock (_lock)
            {
                _task = Generate(difficulty);
                return ToClientDto(_task);
            }
        }

        public object CurrentDto()
        {
            lock (_lock)
            {
                if (_task == null) return new { active = false };
                return ToClientDto(_task);
            }
        }

        public object Stop()
        {
            lock (_lock) { if (_task != null) _task.Active = false; }
            return new { ok = true };
        }

        // ---------- 购票校验 + 评分 ----------
        public object Submit(TaskSubmitRequest req)
        {
            TicketTask? task;
            lock (_lock) { task = _task; }

            if (task == null || !task.Active) return new { ok = false, msg = "当前没有进行中的抢票任务" };
            if (DateTime.Now > task.Deadline) { task.Active = false; return new { ok = false, msg = "任务已超时" }; }
            if (task.Submitted) return new { ok = false, msg = "本任务已提交过订单" };

            var dims = new List<TicketTaskDimension>();
            var feedback = new List<string>();

            // 解析玩家提交的乘客
            var submitted = req.Passengers ?? new List<TaskSubmitPassenger>();
            var submittedNames = submitted.Select(p => (p.Name ?? "").Trim()).Where(n => n.Length > 0).ToList();

            var train = _trainSystem.Trains.FirstOrDefault(x => x.TrainNumber == req.Train);

            // 维度 1：出发站 / 到达站
            bool fromOk = string.Equals(req.From, task.FromStation, StringComparison.Ordinal);
            bool toOk = string.Equals(req.To, task.ToStation, StringComparison.Ordinal);
            dims.Add(new TicketTaskDimension { Key = "station", Name = "站点选择", Max = 20, Earned = (fromOk ? 10 : 0) + (toOk ? 10 : 0), Passed = fromOk && toOk,
                Note = fromOk && toOk ? "出发/到达站均正确" : (fromOk ? "到达站错误" : toOk ? "出发站错误" : "出发/到达站均错误") });
            if (!fromOk) feedback.Add($"出发站应为「{task.FromStation}」（你填了 {req.From}）");
            if (!toOk) feedback.Add($"到达站应为「{task.ToStation}」（你填了 {req.To}）");

            // 维度 2：车次
            bool trainOk = string.Equals(req.Train, task.TrainNumber, StringComparison.Ordinal);
            dims.Add(new TicketTaskDimension { Key = "train", Name = "车次选择", Max = 10, Earned = trainOk ? 10 : 0, Passed = trainOk,
                Note = trainOk ? $"车次 {task.TrainNumber} 正确" : $"车次应为 {task.TrainNumber}" });
            if (!trainOk) feedback.Add($"车次应为 {task.TrainNumber}（你选了 {req.Train}）");

            // 维度 3：乘客选择
            var targetNames = task.Passengers.Select(p => p.Name).ToList();
            int correctSelected = targetNames.Count(n => submittedNames.Contains(n));
            int wrongSelected = submittedNames.Count(n => !targetNames.Contains(n));
            int passengerScore = Math.Max(0, (int)Math.Round(15.0 * correctSelected / Math.Max(1, targetNames.Count)) - 10 * wrongSelected);
            dims.Add(new TicketTaskDimension { Key = "passenger", Name = "乘客选择", Max = 15, Earned = passengerScore, Passed = wrongSelected == 0 && correctSelected == targetNames.Count,
                Note = wrongSelected == 0 && correctSelected == targetNames.Count ? "乘客全部选对" : $"应选 {targetNames.Count} 人，选对 {correctSelected} 人，误选 {wrongSelected} 人" });
            if (correctSelected != targetNames.Count || wrongSelected > 0)
                feedback.Add($"目标乘客：{string.Join("、", targetNames)}；你选了 {string.Join("、", submittedNames.Count > 0 ? submittedNames : new List<string> { "（无）" })}");

            // 维度 3b：乘客类型（成人/儿童/学生）
            int typeScore = 0, typeMax = 10, typeMatched = 0;
            foreach (var tp in task.Passengers)
            {
                var sp = submitted.FirstOrDefault(p => string.Equals(p.Name?.Trim(), tp.Name, StringComparison.Ordinal));
                if (sp == null) continue;
                typeMatched++;
                if (string.Equals(sp.PassengerType, tp.PassengerType, StringComparison.Ordinal)) typeScore++;
            }
            if (typeMatched == 0) typeMatched = 1;
            typeScore = (int)Math.Round(typeMax * (double)typeScore / typeMatched);
            dims.Add(new TicketTaskDimension { Key = "passengerType", Name = "乘客类型", Max = typeMax, Earned = typeScore, Passed = typeScore == typeMax,
                Note = typeScore == typeMax ? "乘客类型正确" : $"应选 {string.Join("/", task.Passengers.Select(p => p.PassengerType).Distinct())}" });
            if (typeScore != typeMax) feedback.Add("乘客类型（成人/儿童/学生）与需求不符");

            // 维度 4/5：席位 + 座位偏好（按匹配到的目标乘客逐人核对）
            int seatClassScore = 0, seatPrefScore = 0;
            int seatClassMax = 15, seatPrefMax = 10;
            int matched = 0;
            foreach (var tp in task.Passengers)
            {
                var sp = submitted.FirstOrDefault(p => string.Equals(p.Name?.Trim(), tp.Name, StringComparison.Ordinal));
                if (sp == null) continue;
                matched++;
                if (string.Equals(sp.SeatClass, tp.SeatClass, StringComparison.Ordinal)) seatClassScore++;
                if (SeatPrefMatches(tp.SeatPref, sp.PreferredSeat)) seatPrefScore++;
            }
            if (matched == 0) matched = 1;
            seatClassScore = (int)Math.Round(seatClassMax * (double)seatClassScore / matched);
            seatPrefScore = (int)Math.Round(seatPrefMax * (double)seatPrefScore / matched);
            dims.Add(new TicketTaskDimension { Key = "seatClass", Name = "席别", Max = seatClassMax, Earned = seatClassScore, Passed = seatClassScore == seatClassMax,
                Note = seatClassScore == seatClassMax ? "席别正确" : $"应选 {string.Join("/", task.Passengers.Select(p => p.SeatClass).Distinct())}" });
            dims.Add(new TicketTaskDimension { Key = "seatPref", Name = "座位偏好", Max = seatPrefMax, Earned = seatPrefScore, Passed = seatPrefScore == seatPrefMax,
                Note = seatPrefScore == seatPrefMax ? "座位偏好正确" : "座位偏好与需求不符" });
            if (seatClassScore != seatClassMax) feedback.Add("席别与乘客需求不符");
            if (seatPrefScore != seatPrefMax) feedback.Add("座位偏好与乘客需求不符");

            // 维度 6：发车时间段
            int hour = -1;
            if (train != null)
            {
                int fi = train.Stations.FindIndex(s => s.StationName == req.From);
                int ti = train.Stations.FindIndex(s => s.StationName == req.To);
                if (fi >= 0 && ti > fi)
                    hour = train.DepartureTime.AddMinutes(train.Stations[fi].DepartMinutes).Hour;
            }
            string wantPeriod = task.Passengers[0].TimePeriod;
            bool periodOk = string.IsNullOrEmpty(wantPeriod) || (hour >= 0 && PeriodMatches(wantPeriod, hour));
            dims.Add(new TicketTaskDimension { Key = "time", Name = "发车时段", Max = 10, Earned = periodOk ? 10 : 0, Passed = periodOk,
                Note = string.IsNullOrEmpty(wantPeriod) ? "本任务无时段要求" : (periodOk ? $"时段「{wantPeriod}」正确" : $"需在「{wantPeriod}」发车") });
            if (!periodOk) feedback.Add($"乘客要求在「{wantPeriod}」发车");

            // 创建真实订单（复用 TrainSystem 出票）
            string? orderId = null;
            string failMsg = "";
            if (train != null && submittedNames.Count > 0)
            {
                int fi = train.Stations.FindIndex(s => s.StationName == req.From);
                int ti = train.Stations.FindIndex(s => s.StationName == req.To);
                if (fi >= 0 && ti > fi)
                {
                    var o = new Order
                    {
                        TrainNumber = req.Train,
                        TrainName = train.TrainName,
                        FromStation = req.From,
                        ToStation = req.To,
                        DepartureTime = train.DepartureTime.AddMinutes(train.Stations[fi].DepartMinutes).ToString("HH:mm"),
                        ArrivalTime = train.DepartureTime.AddMinutes(train.Stations[ti].ArriveMinutes).ToString("HH:mm"),
                        Source = "task"
                    };
                    for (int i = fi; i <= ti; i++) o.PassedStations.Add(train.Stations[i].StationName);
                    foreach (var sp in submitted)
                    {
                        var cls = string.IsNullOrWhiteSpace(sp.SeatClass) ? "二等座" : sp.SeatClass;
                        var pt = ToPassengerType(sp.PassengerType);
                        int age = pt == PassengerType.Child ? Random.Shared.Next(6, 15)
                                : pt == PassengerType.Student ? Random.Shared.Next(18, 24)
                                : Random.Shared.Next(18, 60);
                        o.Passengers.Add(new PassengerInfo(sp.Name ?? "乘客", sp.PreferredSeat ?? "any", cls)
                        {
                            Age = age,
                            Gender = Random.Shared.Next(2) == 0 ? "男" : "女",
                            PassengerType = pt
                        });
                    }
                    _trainSystem.EnqueueOrder(o);
                    orderId = o.OrderId;
                }
                else failMsg = "所选站点与车次不匹配";
            }
            else failMsg = failMsg.Length > 0 ? failMsg : "车次不存在";

            int bookingScore = dims.Sum(d => d.Earned);
            task.Score = bookingScore;
            task.Dimensions = dims;
            task.Feedback = feedback;
            task.Submitted = true;
            task.LastOrderId = orderId ?? "";
            task.Rank = RankOf(bookingScore);

            return new
            {
                ok = true,
                orderId,
                failMsg,
                score = bookingScore,
                maxScore = 90,
                rank = task.Rank,
                dimensions = dims,
                feedback
            };
        }

        // ---------- 支付校验 ----------
        public object Pay(TaskPayRequest req)
        {
            TicketTask? task;
            lock (_lock) { task = _task; }
            if (task == null || !task.Active) return new { ok = false, msg = "当前没有进行中的抢票任务" };
            if (!task.Submitted) return new { ok = false, msg = "请先完成购票" };

            bool methodOk = string.Equals(req.Method, task.PayMethod, StringComparison.Ordinal);
            bool pwdOk = string.Equals(req.Password, task.PayPassword, StringComparison.Ordinal);

            var methodDim = new TicketTaskDimension { Key = "payMethod", Name = "支付方式", Max = 5, Earned = methodOk ? 5 : 0, Passed = methodOk,
                Note = methodOk ? "支付方式正确" : $"应使用 {PayMethodName(task.PayMethod)}" };
            var pwdDim = new TicketTaskDimension { Key = "payPassword", Name = "支付密码", Max = 5, Earned = pwdOk ? 5 : 0, Passed = pwdOk,
                Note = pwdOk ? "支付密码正确" : "支付密码错误" };

            // 合并维度
            var dims = task.Dimensions.Where(d => d.Key != "payMethod" && d.Key != "payPassword").ToList();
            dims.Add(methodDim);
            dims.Add(pwdDim);
            task.Dimensions = dims;
            task.Score = dims.Sum(d => d.Earned);
            task.Paid = true;
            task.Rank = RankOf(task.Score);

            // 标记真实订单已支付
            if (!string.IsNullOrEmpty(req.OrderId))
            {
                var o = _trainSystem.AllOrdersSnapshot().FirstOrDefault(x => x.OrderId == req.OrderId);
                if (o != null && o.Status == OrderStatus.Success)
                {
                    o.Status = OrderStatus.Paid;
                    _trainSystem.RaiseOrderStatusChanged(o);
                }
            }

            int elapsed = Math.Max(0, (int)(DateTime.Now - task.StartTime).TotalSeconds);
            int speedCoins = Math.Max(0, (task.TimeLimitSeconds - elapsed) / 10);
            int coins = task.Score / 10 + (task.Rank == "S" ? 50 : task.Rank == "A" ? 30 : task.Rank == "B" ? 15 : 0) + speedCoins;

            return new
            {
                ok = true,
                score = task.Score,
                maxScore = 100,
                rank = task.Rank,
                coins,
                elapsed,
                speedCoins,
                dimensions = dims,
                methodOk,
                pwdOk
            };
        }

        // ---------- 工具 ----------
        private TicketTask Generate(string difficulty)
        {
            var (paxCount, decoys, timeLimit, exactStation) = DifficultyFor(difficulty);
            if (_trainSystem.Trains.Count == 0) _trainSystem.InitializeDefaultTrains();

            var train = _trainSystem.Trains[Random.Shared.Next(_trainSystem.Trains.Count)];
            // 挑选一段：起点与终点城市不同
            int fi = Random.Shared.Next(train.Stations.Count - 1);
            int ti = Random.Shared.Next(fi + 1, train.Stations.Count);
            var from = train.Stations[fi].StationName;
            var to = train.Stations[ti].StationName;

            int departMin = train.Stations[fi].DepartMinutes;
            string departTime = train.DepartureTime.AddMinutes(departMin).ToString("HH:mm");
            string arriveTime = train.DepartureTime.AddMinutes(train.Stations[ti].ArriveMinutes).ToString("HH:mm");
            string period = PeriodOf(train.DepartureTime.AddMinutes(departMin).Hour);
            // 难度越高，乘客要求越多：easy 无座位/时段要求；normal 加座位偏好；hard 加时段；hell 全部
            bool timeReq = difficulty == "hard" || difficulty == "hell";

            string fromHint = exactStation ? from : SuffixLabel(from);
            string toHint = exactStation ? to : SuffixLabel(to);

            // 席位与座位偏好按乘客逐个生成（越高难度越可能各不相同）
            string payMethod = PayMethods[Random.Shared.Next(PayMethods.Length)];
            string payPassword = Random.Shared.Next(100000, 1000000).ToString();

            var task = new TicketTask
            {
                TaskId = Guid.NewGuid().ToString("N")[..8],
                Difficulty = difficulty,
                FromCity = CityOf(from),
                ToCity = CityOf(to),
                FromStation = from,
                ToStation = to,
                FromStationHint = fromHint,
                ToStationHint = toHint,
                TrainNumber = train.TrainNumber,
                TrainName = train.TrainName,
                DepartureTime = departTime,
                ArrivalTime = arriveTime,
                TimeLimitSeconds = timeLimit,
                StartTime = DateTime.Now,
                Deadline = DateTime.Now.AddSeconds(timeLimit),
                Active = true,
                PayMethod = payMethod,
                PayPassword = payPassword
            };

            // 目标乘客
            var usedNames = new HashSet<string>();
            var classes = ValidSeatClasses(train);
            for (int i = 0; i < paxCount; i++)
            {
                var name = UniqueName(usedNames);
                var p = new TicketTaskPassenger
                {
                    Name = name,
                    IdNumber = RandomId(),
                    PassengerType = difficulty == "easy" ? "成人" : (Random.Shared.Next(100) < 75 ? "成人" : (Random.Shared.Next(2) == 0 ? "儿童" : "学生")),
                    SeatClass = classes[Random.Shared.Next(classes.Length)],
                    SeatPref = difficulty == "easy" ? "any" : (Random.Shared.Next(2) == 0 ? "window" : "aisle"),
                    TimePeriod = timeReq ? period : "",
                    StationPref = exactStation ? "" : $"{fromHint}出发 · {toHint}到达",
                    PayMethod = payMethod,
                    PayPassword = payPassword
                };
                task.Passengers.Add(p);
                usedNames.Add(name);
            }

            // 候选乘客：目标 + 干扰
            var candidates = new List<PassengerCandidate>();
            foreach (var p in task.Passengers)
                candidates.Add(new PassengerCandidate { Name = p.Name, IdNumber = p.IdNumber, IsTarget = true, Role = "乘客" });
            var decoyNames = new HashSet<string>();
            for (int i = 0; i < decoys; i++)
            {
                var name = UniqueName(usedNames);
                decoyNames.Add(name); usedNames.Add(name);
                candidates.Add(new PassengerCandidate { Name = name, IdNumber = RandomId(), IsTarget = false, Role = Relations[Random.Shared.Next(Relations.Length)] });
            }
            task.Candidates = candidates.OrderBy(_ => Random.Shared.Next()).ToList();

            return task;
        }

        private static string UniqueName(HashSet<string> used)
        {
            for (int i = 0; i < 200; i++)
            {
                var gender = Random.Shared.Next(2) == 0;
                var pool = gender ? MaleNames : FemaleNames;
                var name = Surnames[Random.Shared.Next(Surnames.Length)] + pool[Random.Shared.Next(pool.Length)];
                if (used.Add(name)) return name;
            }
            return "乘客" + used.Count;
        }

        private static string RandomId()
        {
            string area = new[] { "320102", "110101", "310101", "440103", "510104" }[Random.Shared.Next(5)];
            int year = Random.Shared.Next(1960, 2005);
            int month = Random.Shared.Next(1, 13);
            int day = Random.Shared.Next(1, 29);
            int seq = Random.Shared.Next(100, 1000);
            int g = Random.Shared.Next(0, 10);
            return area + year + month.ToString("D2") + day.ToString("D2") + seq + g;
        }

        private static (int pax, int decoys, int timeLimit, bool exact) DifficultyFor(string difficulty) => difficulty switch
        {
            "easy" => (1, 2, 300, true),
            "hard" => (2, 3, 180, false),
            "hell" => (3, 4, 150, false),
            _ => (1, 2, 240, false) // normal
        };

        private static string CityOf(string name)
        {
            if (name.EndsWith("虹桥")) return name[..^2];
            if (name.EndsWith("南站") || name.EndsWith("北站") || name.EndsWith("东站") || name.EndsWith("西站")) return name[..^2];
            if (name.EndsWith("南") || name.EndsWith("北") || name.EndsWith("东") || name.EndsWith("西")) return name[..^1];
            return name;
        }

        private static string SuffixLabel(string name)
        {
            if (name.EndsWith("虹桥")) return "虹桥站";
            if (name.EndsWith("南站")) return "南站";
            if (name.EndsWith("北站")) return "北站";
            if (name.EndsWith("东站")) return "东站";
            if (name.EndsWith("西站")) return "西站";
            if (name.EndsWith("南")) return "南站";
            if (name.EndsWith("北")) return "北站";
            if (name.EndsWith("东")) return "东站";
            if (name.EndsWith("西")) return "西站";
            return "本站";
        }

        private static string PeriodOf(int hour) => hour switch
        {
            < 12 => "上午",
            < 14 => "中午",
            < 18 => "下午",
            _ => "晚间"
        };

        private static bool PeriodMatches(string period, int hour) => period switch
        {
            "上午" => hour >= 6 && hour < 12,
            "中午" => hour >= 11 && hour < 14,
            "下午" => hour >= 13 && hour < 18,
            "晚间" => hour >= 18 && hour < 24,
            _ => true
        };

        private static bool SeatPrefMatches(string want, string got)
        {
            var w = (want ?? "").Trim();
            var g = (got ?? "").Trim();
            if (string.IsNullOrEmpty(w) || w == "any") return true;
            if (w == "window") return g == "window" || g == "A" || g == "F";
            if (w == "aisle") return g == "aisle" || g == "C" || g == "D";
            return w == g;
        }

        private static PassengerType ToPassengerType(string t) => t switch
        {
            "儿童" => PassengerType.Child,
            "学生" => PassengerType.Student,
            _ => PassengerType.Adult
        };

        private static string[] ValidSeatClasses(Train t)
            => t.Carriages.Any(c => c.Type == CarriageType.SoftSleeper)
                ? new[] { "硬座", "硬卧", "软卧", "无座" }
                : new[] { "二等座", "一等座", "商务座", "无座" };

        private static string RankOf(int score)
            => score >= 90 ? "S" : score >= 75 ? "A" : score >= 60 ? "B" : score >= 40 ? "C" : "D";

        private static string PayMethodName(string m) => m switch
        {
            "alipay" => "支付宝",
            "wechat" => "微信支付",
            "card" => "银行卡",
            "unionpay" => "云闪付",
            "ecard" => "铁路E卡通",
            _ => m
        };

        private static object ToClientDto(TicketTask task) => new
        {
            taskId = task.TaskId,
            difficulty = task.Difficulty,
            timeLimitSeconds = task.TimeLimitSeconds,
            deadline = task.Deadline,
            fromCity = task.FromCity,
            toCity = task.ToCity,
            fromStationHint = task.FromStationHint,
            toStationHint = task.ToStationHint,
            payMethod = task.PayMethod,
            payPassword = task.PayPassword,
            submitted = task.Submitted,
            paid = task.Paid,
            passengers = task.Passengers.Select(p => new
            {
                p.Name, p.IdNumber, p.PassengerType, p.SeatClass, p.SeatPref, p.TimePeriod, p.StationPref, p.PayMethod
            }).ToList(),
            candidates = task.Candidates.Select(c => new
            {
                c.Name, c.IdNumber, c.Role
            }).ToList()
        };
    }

    public class TaskSubmitRequest
    {
        public string Train { get; set; } = "";
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public List<TaskSubmitPassenger> Passengers { get; set; } = new();
    }

    public class TaskSubmitPassenger
    {
        public string Name { get; set; } = "";
        public string PassengerType { get; set; } = "";
        public string SeatClass { get; set; } = "";
        public string PreferredSeat { get; set; } = "";
    }

    public class TaskPayRequest
    {
        public string OrderId { get; set; } = "";
        public string Method { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
