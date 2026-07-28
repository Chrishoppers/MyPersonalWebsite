using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyPersonalWebsite.Models;

namespace MyPersonalWebsite.Services
{
    public class TrainService
    {
        private readonly Random _random = new();

        // ============================================================
        // 主查询方法
        // ============================================================
        public async Task<TrainFullDetailInfo?> QueryTrainAsync(string trainCode, string? date = null)
        {
            await Task.Delay(300); // 模拟网络延迟

            var route = GetRouteData(trainCode);
            if (route == null || !route.Any())
            {
                // 如果车次不存在，返回一个默认的智能生成路线
                route = GenerateSmartRoute(trainCode);
                if (route == null) return null;
            }

            var stops = BuildDetailedStops(route, trainCode);
            var info = BuildTrainInfo(trainCode, stops);

            // 计算实时状态
            CalculateRealTimeStatus(info);

            return info;
        }

        // ============================================================
        // 智能生成路线（车次不存在时）
        // ============================================================
        private List<string>? GenerateSmartRoute(string trainCode)
        {
            if (string.IsNullOrEmpty(trainCode)) return null;

            var firstChar = trainCode[0];
            var majorCities = new List<string> { "北京", "上海", "广州", "深圳", "成都", "武汉", "南京", "西安", "杭州", "重庆" };

            switch (firstChar)
            {
                case 'G':
                    return new List<string> { "北京南", "济南西", "徐州东", "南京南", "上海虹桥" };
                case 'D':
                    return new List<string> { "北京", "天津", "济南", "南京", "上海" };
                case 'Z':
                    return new List<string> { "北京", "石家庄", "郑州", "武汉", "长沙", "广州" };
                case 'T':
                    return new List<string> { "北京", "保定", "石家庄", "邢台", "邯郸", "郑州", "武汉" };
                case 'K':
                    return new List<string> { "北京", "廊坊", "天津", "沧州", "德州", "济南", "徐州", "南京", "上海" };
                case 'C':
                    return new List<string> { "北京南", "亦庄", "武清", "天津" };
                default:
                    return null;
            }
        }

        // ============================================================
        // 构建详细的经停站数据
        // ============================================================
        private List<TrainStopDetail> BuildDetailedStops(List<string> routeNames, string trainCode)
        {
            var now = DateTime.Now;
            var stops = new List<TrainStopDetail>();
            var totalStops = routeNames.Count;

            for (int i = 0; i < totalStops; i++)
            {
                var name = routeNames[i];
                var stationInfo = StationDataService.GetStationInfo(name);

                // 计算时间：始发站6:00发车，每站间隔8-15分钟
                var baseMinutes = i * (8 + _random.Next(0, 8));
                var arriveTime = i == 0 ? "始发" : now.AddMinutes(baseMinutes - 2 + _random.Next(0, 4)).ToString("HH:mm");
                var departTime = i == totalStops - 1 ? "终到" : now.AddMinutes(baseMinutes + 2 + _random.Next(0, 3)).ToString("HH:mm");

                // 停靠时间
                string stopDuration;
                if (i == 0 || i == totalStops - 1)
                    stopDuration = "—";
                else if (trainCode.StartsWith("G"))
                    stopDuration = $"{_random.Next(2, 4)}分钟";
                else if (trainCode.StartsWith("D"))
                    stopDuration = $"{_random.Next(3, 6)}分钟";
                else
                    stopDuration = $"{_random.Next(5, 12)}分钟";

                // 站台信息
                var platformNumbers = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13" };
                var platform = platformNumbers[_random.Next(platformNumbers.Length)] + "站台";
                if (i == 0) platform = "🟢 始发·" + platform;
                if (i == totalStops - 1) platform = "🏁 终到·" + platform;

                // 开门方向
                var doorDirection = i == 0 || i == totalStops - 1 ? "不固定" : (i % 2 == 0 ? "⬅️ 左侧开门" : "➡️ 右侧开门");

                // 地标颜色（站台地标）
                var landmarkColors = new[] { "🟡 黄色（8节编组）", "🟢 绿色（8节编组）", "🟣 紫色（16节编组）", "🔵 蓝色（16节编组）" };
                var landmarkColor = landmarkColors[i % landmarkColors.Length];

                // 特殊提示
                var specialNotes = new[] {
                    "", "", "⚠️ 站台间隙较大，注意脚下安全",
                    "🚇 可换乘地铁", "♿ 无障碍电梯位于站台中部",
                    "📢 请提前到车门等候", "🚻 洗手间在站台两端"
                };

                var stop = new TrainStopDetail
                {
                    StationNo = i + 1,
                    StationName = name,
                    StationInfo = stationInfo,
                    StationType = stationInfo?.Type ?? "普通站",
                    ArriveTime = arriveTime,
                    DepartTime = departTime,
                    StopTime = stopDuration,
                    IsStart = i == 0,
                    IsEnd = i == totalStops - 1,
                    DayOffset = i > 12 ? 1 : 0,
                    Platform = platform,
                    DoorDirection = doorDirection,
                    TrackNumber = $"{_random.Next(1, 12)}股道",
                    WaitingArea = $"候车区{(char)('A' + _random.Next(0, 5))}",
                    PlatformSide = doorDirection == "左侧开门" ? "⬅️ 左侧下车" : doorDirection == "右侧开门" ? "➡️ 右侧下车" : "⏺ 不固定",
                    LandmarkColor = landmarkColor,
                    CarriageDirection = i % 2 == 0 ? "⏩ 向前（车头方向）" : "⏪ 向后（车尾方向）",
                    BoardingGuide = i == 0 ? "始发站，请根据车票信息在对应检票口候车" :
                                   i == totalStops - 1 ? "终点站，请带好随身物品有序下车" :
                                   $"本站停靠{stopDuration}，请勿远离车门",
                    SpecialNote = specialNotes[_random.Next(specialNotes.Length)],
                    NearbyTransport = stationInfo?.NearbyTransport ?? "",
                    // 增强数据
                    PlatformSide = i % 2 == 0 ? "左侧" : "右侧"
                };

                stops.Add(stop);
            }

            return stops;
        }

        // ============================================================
        // 构建列车完整信息
        // ============================================================
        private TrainFullDetailInfo BuildTrainInfo(string trainCode, List<TrainStopDetail> stops)
        {
            var firstChar = trainCode[0];
            var (brand, model, maxSpeed, trainType) = firstChar switch
            {
                'G' => ("复兴号", "CR400AF", "350km/h", "高速动车"),
                'D' => ("和谐号", "CRH380B", "250km/h", "动车组"),
                'C' => ("和谐号", "CRH6A", "200km/h", "城际列车"),
                'Z' => ("直达", "25T型", "160km/h", "直达特快"),
                'T' => ("特快", "25K型", "140km/h", "特快列车"),
                'K' => ("快速", "25G型", "120km/h", "快速列车"),
                _ => ("普速", "25B型", "100km/h", "普速列车")
            };

            var totalDistance = firstChar switch
            {
                'G' => _random.Next(1000, 2500),
                'D' => _random.Next(800, 2000),
                'Z' => _random.Next(1500, 3000),
                'T' => _random.Next(1000, 2500),
                'K' => _random.Next(800, 2200),
                _ => _random.Next(500, 1500)
            };

            var totalHours = _random.Next(3, 12);
            var totalMinutes = _random.Next(10, 50);

            return new TrainFullDetailInfo
            {
                TrainCode = trainCode,
                TrainType = trainType,
                TrainBrand = brand,
                TrainModel = model,
                MaxSpeed = maxSpeed,
                Consist = stops.Count > 12 ? "16节编组" : stops.Count > 8 ? "12节编组" : "8节编组",
                StartStation = stops.First().StationName,
                EndStation = stops.Last().StationName,
                DetailStops = stops,
                TotalStops = stops.Count,
                TotalDistance = $"{totalDistance}公里",
                TotalDuration = $"{totalHours}小时{totalMinutes}分钟",
                Stops = stops.Select(s => new TrainStop
                {
                    StationNo = s.StationNo,
                    StationName = s.StationName,
                    ArriveTime = s.ArriveTime,
                    DepartTime = s.DepartTime,
                    StopTime = s.StopTime,
                    IsStart = s.IsStart,
                    IsEnd = s.IsEnd,
                    DayOffset = s.DayOffset
                }).ToList(),
                QueryTime = DateTime.Now,
                Status = "运行中",
                DelayInfo = "正点",
                ProgressPercent = 0
            };
        }

        // ============================================================
        // 计算实时状态
        // ============================================================
        private void CalculateRealTimeStatus(TrainFullDetailInfo info)
        {
            if (info.DetailStops == null || !info.DetailStops.Any()) return;

            var now = DateTime.Now;
            var total = info.DetailStops.Count;

            // 使用当前时间模拟列车位置
            var hourOffset = now.Hour - 6; // 假设6点发车
            var minuteOffset = now.Minute;
            var totalMinutes = hourOffset * 60 + minuteOffset;

            // 每个站平均间隔约12分钟
            var currentIndex = Math.Min(total - 1, Math.Max(0, totalMinutes / 10));

            // 让进度更自然一些
            var progress = (double)currentIndex / (total - 1) * 100;
            progress = Math.Min(100, Math.Max(0, progress + _random.Next(-2, 3)));

            info.ProgressPercent = (int)progress;

            if (progress < 3)
            {
                info.Status = "未发车";
                info.CurrentStation = info.DetailStops.First().StationName;
                info.CurrentStationInfo = $"🟢 始发站 · {info.DetailStops.First().Platform}";
                info.NextStation = info.DetailStops.Count > 1 ? info.DetailStops[1].StationName : "";
                info.NextArriveTime = info.DetailStops.Count > 1 ? info.DetailStops[1].ArriveTime : "";
                info.NextStationPlatform = info.DetailStops.Count > 1 ? info.DetailStops[1].Platform : "";
                info.NextStationDoorSide = info.DetailStops.Count > 1 ? info.DetailStops[1].DoorDirection : "";
                info.DelayInfo = "正点";
                return;
            }

            if (progress > 97)
            {
                info.Status = "已到达";
                info.CurrentStation = info.DetailStops.Last().StationName;
                info.CurrentStationInfo = "🏁 终点站已到达";
                info.NextStation = "";
                info.NextArriveTime = "";
                info.ProgressPercent = 100;
                info.DelayInfo = "正点";
                return;
            }

            // 确定当前站和下一站
            var index = (int)(progress / 100 * (total - 1));
            index = Math.Max(0, Math.Min(total - 2, index));

            var currentStop = info.DetailStops[index];
            var nextStop = info.DetailStops[index + 1];

            info.CurrentStation = currentStop.StationName;
            info.NextStation = nextStop.StationName;
            info.NextArriveTime = nextStop.ArriveTime;
            info.NextStationPlatform = nextStop.Platform;
            info.NextStationDoorSide = nextStop.DoorDirection;

            // 判断是否停靠中（模拟：约40%概率在停靠）
            var isStopping = _random.Next(0, 100) < 35;
            info.Status = isStopping ? "停靠中" : "运行中";

            var statusIcons = new Dictionary<string, string>
            {
                { "运行中", "🚄" },
                { "停靠中", "🚉" },
                { "已到达", "🏁" },
                { "未发车", "🟢" }
            };

            info.CurrentStationInfo = $"{statusIcons.GetValueOrDefault(info.Status, "📍")} {currentStop.Platform} · {currentStop.WaitingArea}";

            if (isStopping)
            {
                info.CurrentStationInfo += $" · 停靠{currentStop.StopTime}";
                if (!string.IsNullOrEmpty(currentStop.SpecialNote))
                {
                    info.CurrentStationInfo += $" · {currentStop.SpecialNote}";
                }
            }

            // 晚点模拟（5%概率）
            info.DelayInfo = _random.Next(0, 100) < 5 ? $"晚点{_random.Next(3, 15)}分钟" : "正点";
        }

        // ============================================================
        // 路线数据（真实车次路线）
        // ============================================================
        private List<string>? GetRouteData(string trainCode)
        {
            var routes = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // === 京沪高铁 ===
                { "G1", new List<string> { "北京南", "天津南", "济南西", "南京南", "上海虹桥" } },
                { "G2", new List<string> { "上海虹桥", "南京南", "济南西", "天津南", "北京南" } },
                { "G3", new List<string> { "北京南", "济南西", "南京南", "无锡东", "上海虹桥" } },
                { "G4", new List<string> { "上海虹桥", "无锡东", "南京南", "济南西", "北京南" } },
                { "G5", new List<string> { "北京南", "天津南", "德州东", "济南西", "徐州东", "南京南", "上海虹桥" } },
                { "G6", new List<string> { "上海虹桥", "南京南", "徐州东", "济南西", "德州东", "天津南", "北京南" } },
                { "G7", new List<string> { "北京南", "天津南", "济南西", "徐州东", "南京南", "苏州北", "上海虹桥" } },
                { "G8", new List<string> { "上海虹桥", "苏州北", "南京南", "徐州东", "济南西", "天津南", "北京南" } },
                { "G9", new List<string> { "北京南", "济南西", "南京南", "苏州北", "上海虹桥" } },
                { "G10", new List<string> { "上海虹桥", "苏州北", "南京南", "济南西", "北京南" } },

                // === 京广高铁 ===
                { "G79", new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "广州南" } },
                { "G80", new List<string> { "广州南", "长沙南", "武汉", "郑州东", "石家庄", "北京西" } },
                { "G81", new List<string> { "北京西", "保定东", "石家庄", "郑州东", "武汉", "长沙南", "广州南" } },
                { "G82", new List<string> { "广州南", "长沙南", "武汉", "郑州东", "石家庄", "保定东", "北京西" } },
                { "G83", new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "衡阳东", "广州南" } },
                { "G84", new List<string> { "广州南", "衡阳东", "长沙南", "武汉", "郑州东", "石家庄", "北京西" } },

                // === 沪昆高铁 ===
                { "G93", new List<string> { "上海虹桥", "杭州东", "南昌西", "长沙南", "贵阳北", "昆明南" } },
                { "G94", new List<string> { "昆明南", "贵阳北", "长沙南", "南昌西", "杭州东", "上海虹桥" } },

                // === 西成高铁 ===
                { "G97", new List<string> { "西安北", "汉中", "广元", "绵阳", "成都东" } },
                { "G98", new List<string> { "成都东", "绵阳", "广元", "汉中", "西安北" } },

                // === 成渝高铁 ===
                { "G105", new List<string> { "成都东", "简阳南", "资阳北", "重庆西" } },
                { "G106", new List<string> { "重庆西", "资阳北", "简阳南", "成都东" } },

                // === 沿海高铁 ===
                { "G103", new List<string> { "北京南", "天津", "济南", "徐州", "南京", "上海", "杭州", "宁波", "福州", "厦门" } },
                { "G104", new List<string> { "厦门", "福州", "宁波", "杭州", "上海", "南京", "徐州", "济南", "天津", "北京南" } },

                // === 普速 Z ===
                { "Z1", new List<string> { "北京", "石家庄", "郑州", "武汉", "长沙", "广州" } },
                { "Z2", new List<string> { "广州", "长沙", "武汉", "郑州", "石家庄", "北京" } },
                { "Z3", new List<string> { "北京", "济南", "徐州", "南京", "上海" } },
                { "Z4", new List<string> { "上海", "南京", "徐州", "济南", "北京" } },

                // === 普速 T ===
                { "T1", new List<string> { "北京", "保定", "石家庄", "邢台", "邯郸", "郑州", "武汉", "长沙" } },
                { "T2", new List<string> { "长沙", "武汉", "郑州", "邯郸", "邢台", "石家庄", "保定", "北京" } },
                { "T3", new List<string> { "北京", "天津", "济南", "徐州", "南京", "上海" } },
                { "T4", new List<string> { "上海", "南京", "徐州", "济南", "天津", "北京" } },

                // === 普速 K ===
                { "K1", new List<string> { "北京", "廊坊", "天津", "沧州", "德州", "济南", "泰安", "徐州", "南京", "上海" } },
                { "K2", new List<string> { "上海", "南京", "徐州", "泰安", "济南", "德州", "沧州", "天津", "廊坊", "北京" } },
                { "K3", new List<string> { "北京", "保定", "石家庄", "邢台", "邯郸", "安阳", "郑州", "武汉", "长沙" } },
                { "K4", new List<string> { "长沙", "武汉", "郑州", "安阳", "邯郸", "邢台", "石家庄", "保定", "北京" } },

                // === 城际 C ===
                { "C1", new List<string> { "北京南", "亦庄", "武清", "天津" } },
                { "C2", new List<string> { "天津", "武清", "亦庄", "北京南" } },
                { "C3", new List<string> { "上海", "昆山南", "苏州", "无锡", "常州", "南京" } },
                { "C4", new List<string> { "南京", "常州", "无锡", "苏州", "昆山南", "上海" } },
                { "C5", new List<string> { "广州南", "庆盛", "深圳北", "福田" } },
                { "C6", new List<string> { "福田", "深圳北", "庆盛", "广州南" } },
                { "C7", new List<string> { "成都东", "简阳南", "资阳北", "重庆西" } },
                { "C8", new List<string> { "重庆西", "资阳北", "简阳南", "成都东" } },

                // === 京张高铁 ===
                { "G101", new List<string> { "北京北", "清河", "八达岭长城", "张家口" } },
                { "G102", new List<string> { "张家口", "八达岭长城", "清河", "北京北" } },
            };

            return routes.TryGetValue(trainCode, out var route) ? route : null;
        }
    }
}
