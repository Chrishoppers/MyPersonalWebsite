using System.Collections.Concurrent;
using MyPersonalWebsite.Models;

namespace MyPersonalWebsite.Services
{
    public class TrainSystem
    {
        public enum TicketingMode { Normal, SpringFestival, NationalDay, MayDay }

        public TicketingMode CurrentMode { get; set; } = TicketingMode.Normal;
        public int WaitingCount { get; set; }
        public List<Train> Trains { get; set; } = new();
        public ConcurrentQueue<Order> OrderQueue { get; } = new();
        private readonly List<Order> _allOrders = new();
        public int ServerCount { get; set; } = 3;
        public int TrafficRate { get; set; } = 10;
        public int PeakQueue { get; private set; }
        public int ProcessedTotal { get; private set; }

        private bool _isRunning;
        private CancellationTokenSource? _cts;
        private readonly object _seatLock = new();
        private readonly object _ordersLock = new();
        private readonly object _countersLock = new();

        // 线路走廊：(站名, 电报码, 距始发公里数)
        private static readonly (string Name, string Code, int Km)[][] Corridors =
        {
            new (string, string, int)[] { ("北京南","BJP",0), ("天津西","TXP",120), ("济南西","JWK",406), ("徐州东","XDH",692), ("南京南","NKH",1023), ("无锡东","WXH",1201), ("上海虹桥","AOH",1318) },
            new (string, string, int)[] { ("北京西","BXP",0), ("石家庄","SJP",281), ("郑州东","ZAF",693), ("武汉","WHN",1209), ("长沙南","CWQ",1591), ("广州南","IZQ",2298), ("深圳北","IOQ",2400) },
            new (string, string, int)[] { ("上海虹桥","AOH",0), ("杭州东","HGH",159), ("南昌西","NCG",741), ("长沙南","CWQ",1083), ("贵阳北","KQW",1793), ("昆明南","KOM",2252) },
            new (string, string, int)[] { ("上海虹桥","AOH",0), ("宁波","NGH",314), ("温州南","VRH",589), ("福州南","FYS",883), ("厦门北","XKS",1109), ("深圳北","IOQ",1618) },
            new (string, string, int)[] { ("北京南","BJP",0), ("沈阳北","SBT",703), ("长春","CCT",1003), ("哈尔滨西","VAB",1241) },
            new (string, string, int)[] { ("西安北","EAY",0), ("成都东","ICW",658), ("重庆西","CXW",1097) }
        };

        private static readonly string[] Surnames = {
            "张","李","王","刘","陈","杨","赵","黄","周","吴","徐","孙","胡","朱","高","林","何","郭","马","罗",
            "梁","宋","郑","谢","韩","唐","冯","于","董","萧","程","曹","袁","邓","许","傅","沈","曾","彭","吕"
        };
        private static readonly string[] GivenNames = {
            "伟","芳","娜","敏","静","丽","强","磊","洋","涛","杰","军","玲","超","宇","婷","鑫","鹏","晨","雪",
            "睿","琪","浩","轩","琳","博","文","龙","飞","峰","欣","雨","阳","志","明","慧","俊","凯","华","平"
        };
        private static readonly string[] MaleNames = { "伟","强","磊","军","超","宇","鑫","鹏","龙","飞","峰","志","明","俊","凯","华","平","建","辉","浩","轩","博" };
        private static readonly string[] FemaleNames = { "芳","娜","敏","静","丽","玲","婷","雪","睿","琪","琳","欣","雨","阳","慧","文","悦","颖","晓","云" };

        public event Action<Order>? OnOrderCreated;
        public event Action<Order>? OnOrderStatusChanged;

        public void RaiseOrderStatusChanged(Order o) => OnOrderStatusChanged?.Invoke(o);

        // ---------- 初始化 ----------
        public void InitializeDefaultTrains()
        {
            Trains.Clear();
            var names = new[] { "和谐号", "复兴号", "和谐号", "复兴号", "和谐号", "复兴号" };
            var nums = new[] { "G101", "G201", "G301", "D401", "G501", "D601" };
            for (int i = 0; i < Corridors.Length; i++)
            {
                Trains.Add(BuildTrain(nums[i], $"{names[i]}{nums[i]}", Corridors[i], 0, Corridors[i].Length, i == 3 || i == 5));
            }
        }

        private static Train BuildTrain(string number, string name, (string Name, string Code, int Km)[] corridor, int start, int count, bool isD)
        {
            var t = new Train(number, name)
            {
                MaxSpeed = isD ? 250 : 300,
                DepartureTime = DateTime.Now.Date.AddHours(Random.Shared.Next(6, 14))
            };
            int depart = 0;
            for (int i = 0; i < count; i++)
            {
                var (stationName, code, km) = corridor[start + i];
                int arrive = i == 0 ? 0 : depart;
                int stop = i == 0 || i == count - 1 ? 0 : Random.Shared.Next(2, 5);
                t.Stations.Add(new Station(i + 1, stationName, code, km, arrive, depart)
                {
                    StopMinutes = stop
                });
                depart = arrive + stop + (int)Math.Round((corridor[start + Math.Min(i + 1, count - 1)].Km - km) * 60.0 / t.MaxSpeed);
            }
            // 修正中间站到达时间
            for (int i = 1; i < t.Stations.Count; i++)
            {
                t.Stations[i].ArriveMinutes = t.Stations[i - 1].DepartMinutes +
                    (int)Math.Round((t.Stations[i].Distance - t.Stations[i - 1].Distance) * 60.0 / t.MaxSpeed);
                if (i < t.Stations.Count - 1)
                    t.Stations[i].DepartMinutes = t.Stations[i].ArriveMinutes + t.Stations[i].StopMinutes;
            }
            return t;
        }

        public void AddTrain()
        {
            int ci = Random.Shared.Next(Corridors.Length);
            var corridor = Corridors[ci];
            int start = Random.Shared.Next(Math.Max(1, corridor.Length - 4));
            int count = corridor.Length - start;
            if (count < 3) { start = 0; count = Math.Max(3, corridor.Length); }
            string num = $"G{701 + Trains.Count}";
            Trains.Add(BuildTrain(num, $"和谐号{num}", corridor, start, count, Random.Shared.Next(2) == 0));
        }

        public void RemoveTrain()
        {
            if (Trains.Count > 2) Trains.RemoveAt(Trains.Count - 1);
        }

        public List<Order> AllOrdersSnapshot()
        {
            lock (_ordersLock) return _allOrders.ToList();
        }

        public IEnumerable<Order> RecentOrders(int count = 200)
        {
            lock (_ordersLock) return _allOrders.OrderByDescending(o => o.CreateTime).Take(count).ToList();
        }

        // ---------- 系统启停 ----------
        public void StartSystem()
        {
            if (_isRunning) return;
            _isRunning = true;
            AdjustServers();
        }

        public void AdjustServers()
        {
            if (!_isRunning) return;
            try { _cts?.Cancel(); } catch { }
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _ = Task.Run(() => GenLoop(token));
            for (int i = 0; i < ServerCount; i++)
                _ = Task.Run(() => ProcLoop(token));
        }

        public void StopSystem()
        {
            _isRunning = false;
            try { _cts?.Cancel(); _cts?.Dispose(); } catch { }
        }

        // ---------- 模拟流量生成 ----------
        private async Task GenLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                int n = CurrentMode switch
                {
                    TicketingMode.SpringFestival => TrafficRate,
                    TicketingMode.NationalDay => TrafficRate * 3 / 4,
                    TicketingMode.MayDay => TrafficRate * 2 / 3,
                    _ => Math.Max(1, TrafficRate / 2)
                };
                for (int i = 0; i < n; i++)
                {
                    var o = MakeOrder();
                    EnqueueOrder(o);
                }
                await Task.Delay(600, ct);
            }
        }

        private async Task ProcLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (OrderQueue.TryDequeue(out var o))
                {
                    o.Status = OrderStatus.Processing;
                    o.ProcessTime = DateTime.Now;
                    OnOrderStatusChanged?.Invoke(o);

                    int delay = Math.Max(20, 220 / Math.Max(1, ServerCount));
                    try { await Task.Delay(delay + Random.Shared.Next(40), ct); }
                    catch { break; }

                    DoProcess(o);
                    OnOrderStatusChanged?.Invoke(o);
                    lock (_countersLock) ProcessedTotal++;
                }
                else
                {
                    try { await Task.Delay(30, ct); } catch { break; }
                }
            }
        }

        private Order MakeOrder()
        {
            if (Trains.Count == 0) return new Order { Source = "simulated" };
            var t = Trains[Random.Shared.Next(Trains.Count)];
            var o = new Order { TrainNumber = t.TrainNumber, Source = "simulated" };
            int fi = Random.Shared.Next(t.Stations.Count - 1);
            int ti = Random.Shared.Next(fi + 1, t.Stations.Count);
            o.TrainName = t.TrainName;
            o.FromStation = t.Stations[fi].StationName;
            o.ToStation = t.Stations[ti].StationName;
            o.DepartureTime = t.DepartureTime.AddMinutes(t.Stations[fi].DepartMinutes).ToString("HH:mm");
            o.ArrivalTime = t.DepartureTime.AddMinutes(t.Stations[ti].ArriveMinutes).ToString("HH:mm");
            for (int i = fi; i <= ti; i++) o.PassedStations.Add(t.Stations[i].StationName);

            int pc = CurrentMode switch
            {
                TicketingMode.SpringFestival => Random.Shared.Next(2, 5),
                TicketingMode.NationalDay => Random.Shared.Next(1, 4),
                TicketingMode.MayDay => Random.Shared.Next(1, 3),
                _ => Random.Shared.Next(1, 3)
            };
            for (int p = 0; p < pc; p++)
            {
                var sc = (CarriageType)Random.Shared.Next(3);
                string scs = Carriage.TypeName(sc);
                string[] cols = sc == CarriageType.Business ? new[] { "A", "F" }
                              : sc == CarriageType.FirstClass ? new[] { "A", "C", "D", "F" }
                              : new[] { "A", "B", "C", "D", "F" };
                var (id, age, gender) = GenerateId();
                int typeRand = Random.Shared.Next(100);
                var ptype = typeRand < 80 ? PassengerType.Adult : typeRand < 90 ? PassengerType.Child : PassengerType.Student;
                if (ptype == PassengerType.Child) age = Random.Shared.Next(6, 15);
                if (ptype == PassengerType.Student) age = Random.Shared.Next(18, 24);
                o.Passengers.Add(new PassengerInfo(RandomName(gender), cols[Random.Shared.Next(cols.Length)], scs)
                {
                    IdNumber = id,
                    Age = age,
                    Gender = gender,
                    PassengerType = ptype
                });
            }
            return o;
        }

        private static string RandomName(string gender)
        {
            var pool = gender == "女" ? FemaleNames : MaleNames;
            return Surnames[Random.Shared.Next(Surnames.Length)] + pool[Random.Shared.Next(pool.Length)];
        }

        private static (string Id, int Age, string Gender) GenerateId()
        {
            int year = Random.Shared.Next(1960, 2021);
            int month = Random.Shared.Next(1, 13);
            int day = Random.Shared.Next(1, 29);
            int age = DateTime.Now.Year - year;
            int genderBit = Random.Shared.Next(0, 10);
            string gender = genderBit % 2 == 1 ? "男" : "女";
            string area = new[] { "320102", "110101", "310101", "440103", "510104" }[Random.Shared.Next(5)];
            string birth = year + month.ToString("D2") + day.ToString("D2");
            string seq = Random.Shared.Next(100, 1000).ToString();
            return (area + birth + seq + genderBit, age, gender);
        }

        // ---------- 下单（供用户接口复用） ----------
        public void EnqueueOrder(Order o)
        {
            lock (_ordersLock)
            {
                _allOrders.Add(o);
                if (OrderQueue.Count > PeakQueue) PeakQueue = OrderQueue.Count;
            }
            OnOrderCreated?.Invoke(o);

            if (_isRunning)
            {
                // 系统运行中：进入模拟抢票队列
                OrderQueue.Enqueue(o);
            }
            else
            {
                // 系统未启动：用户订单立即出票（短暂排队动画）
                _ = ProcessImmediately(o);
            }
        }

        private async Task ProcessImmediately(Order o)
        {
            o.Status = OrderStatus.Processing;
            o.ProcessTime = DateTime.Now;
            OnOrderStatusChanged?.Invoke(o);
            await Task.Delay(Random.Shared.Next(300, 900));
            DoProcess(o);
            OnOrderStatusChanged?.Invoke(o);
            lock (_countersLock) ProcessedTotal++;
        }

        // ---------- 出票核心 ----------
        public void DoProcess(Order o)
        {
            var t = Trains.FirstOrDefault(x => x.TrainNumber == o.TrainNumber);
            if (t == null) { o.FailReason = "列车不存在"; o.Status = OrderStatus.Failed; o.FinishTime = DateTime.Now; return; }

            var assigned = new List<string>();
            lock (_seatLock)
            {
                var allSeats = t.Carriages.SelectMany(c => c.Seats).ToList();
                foreach (var p in o.Passengers)
                {
                    var candidates = allSeats.Where(s => s.Type == ToCarriageType(p.SeatClass) && IsFree(s, o)).ToList();
                    var best = candidates.Count > 0 ? FindBestSeat(candidates, p) : null;

                    if (best == null)
                    {
                        var upgrade = p.SeatClass switch
                        {
                            "二等座" => new[] { "一等座", "商务座" },
                            "一等座" => new[] { "商务座" },
                            _ => Array.Empty<string>()
                        };
                        foreach (var up in upgrade)
                        {
                            var ups = allSeats.Where(s => s.Type == ToCarriageType(up) && IsFree(s, o)).ToList();
                            if (ups.Count > 0) { best = FindBestSeat(ups, p); break; }
                        }
                    }
                    if (best != null)
                    {
                        Occupy(best, o);
                        assigned.Add(best.SeatNumber);
                    }
                }
            }

            o.AssignedSeats = assigned;
            o.TotalPrice = CalculatePrice(t, o);
            o.FinishTime = DateTime.Now;

            if (assigned.Count > 0)
            {
                o.Status = OrderStatus.Success;
                o.FailReason = assigned.Count < o.Passengers.Count
                    ? $"已出 {assigned.Count}/{o.Passengers.Count} 座（部分自动升舱）" : "";
            }
            else if (assigned.Count == 0)
            {
                if (CurrentMode == TicketingMode.SpringFestival && o.AttemptCount < o.MaxWaitlistRetries)
                {
                    o.AttemptCount++;
                    o.Status = OrderStatus.Waitlisted;
                    o.FailReason = $"候补排队中（第 {o.AttemptCount} 次尝试）";
                    lock (_countersLock) WaitingCount++;
                    OrderQueue.Enqueue(o);
                }
                else
                {
                    o.Status = OrderStatus.Failed;
                    o.FailReason = "该区间车票已售罄";
                }
            }
        }

        private static CarriageType ToCarriageType(string seatClass) => seatClass switch
        {
            "商务座" => CarriageType.Business,
            "一等座" => CarriageType.FirstClass,
            _ => CarriageType.SecondClass
        };

        public static decimal RatePerKm(string seatClass) => seatClass switch
        {
            "商务座" => 1.75m,
            "一等座" => 0.88m,
            _ => 0.55m
        };

        private static decimal CalculatePrice(Train t, Order o)
        {
            int km = t.DistanceBetween(o.FromStation, o.ToStation);
            decimal sum = 0;
            foreach (var p in o.Passengers)
            {
                decimal basePrice = km * RatePerKm(p.SeatClass) * p.Discount;
                sum += Math.Round(basePrice * 2) / 2; // 四舍五入到 0.5 元
            }
            return sum;
        }

        private Seat? FindBestSeat(List<Seat> seats, PassengerInfo p)
        {
            if (seats.Count == 0) return null;
            var pref = (p.PreferredSeat ?? "").Trim();
            if (pref.Length == 0 || pref == "any") return seats[Random.Shared.Next(seats.Count)];

            // 精确匹配完整座位号（如 "3车5A"）
            var exact = seats.FirstOrDefault(s => s.SeatNumber == pref);
            if (exact != null) return exact;

            // 列偏好：window → A/F，aisle → C/D，单列字母 → 该列
            string[] wanted = pref switch
            {
                "window" => new[] { "A", "F" },
                "aisle" => new[] { "C", "D" },
                _ => pref.Length == 1 && "ABCDEF".Contains(pref, StringComparison.OrdinalIgnoreCase)
                    ? new[] { pref.ToUpperInvariant() }
                    : Array.Empty<string>()
            };
            if (wanted.Length > 0)
            {
                var byCol = seats.Where(s => wanted.Contains(s.Column)).ToList();
                if (byCol.Count > 0) return byCol[Random.Shared.Next(byCol.Count)];
            }
            return seats[Random.Shared.Next(seats.Count)];
        }

        private static void Occupy(Seat s, Order o)
        {
            for (int i = 0; i < o.PassedStations.Count - 1; i++)
                s.SectionOccupied[$"{o.PassedStations[i]}-{o.PassedStations[i + 1]}"] = true;
        }

        private static bool IsFree(Seat s, Order o)
        {
            for (int i = 0; i < o.PassedStations.Count - 1; i++)
            {
                if (s.SectionOccupied.TryGetValue($"{o.PassedStations[i]}-{o.PassedStations[i + 1]}", out bool v) && v)
                    return false;
            }
            return true;
        }

        // ---------- 座位状态查询 ----------
        public string GetSeatStatus(Seat s, Train t, int idx)
        {
            if (idx < 0 || idx >= t.Stations.Count - 1) return "free";
            string sec = $"{t.Stations[idx].StationName}-{t.Stations[idx + 1].StationName}";
            if (s.SectionOccupied.TryGetValue(sec, out bool v) && v) return "occupied";
            if (idx + 1 < t.Stations.Count - 1)
            {
                string ns = $"{t.Stations[idx + 1].StationName}-{t.Stations[idx + 2].StationName}";
                if (s.SectionOccupied.TryGetValue(ns, out bool v2) && v2) return "next";
            }
            return "free";
        }

        // ---------- 统计 ----------
        public object GetStats()
        {
            List<Order> all;
            lock (_ordersLock) all = _allOrders.ToList();
            return new
            {
                queue = OrderQueue.Count,
                total = all.Count,
                success = all.Count(o => o.Status == OrderStatus.Success || o.Status == OrderStatus.Paid),
                failed = all.Count(o => o.Status == OrderStatus.Failed),
                waiting = WaitingCount,
                peakQueue = PeakQueue,
                processed = ProcessedTotal,
                revenue = all.Where(o => o.Status is OrderStatus.Success or OrderStatus.Paid).Sum(o => o.TotalPrice),
                successRate = all.Count == 0 ? 0 : Math.Round(all.Count(o => o.Status is OrderStatus.Success or OrderStatus.Paid) * 100.0 / all.Count, 1),
                trains = Trains.Select(t => new
                {
                    t.TrainNumber,
                    t.TrainName,
                    stations = t.Stations.Count,
                    seats = t.TotalSeats,
                    sold = t.Carriages.Sum(c => c.Seats.Sum(s => s.SectionOccupied.Count > 0 ? 1 : 0))
                }).ToList()
            };
        }
    }
}
