using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MyPersonalWebsite.Models;
using Microsoft.Extensions.Logging;

namespace MyPersonalWebsite.Services
{
    public class TrainService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TrainService> _logger;

        public TrainService(HttpClient httpClient, ILogger<TrainService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        // ============================================================
        // 查询列车完整信息（主入口）
        // ============================================================
        public async Task<TrainFullDetailInfo?> QueryTrainAsync(string trainCode, string? date = null)
        {
            try
            {
                if (string.IsNullOrEmpty(date))
                {
                    date = DateTime.Now.ToString("yyyy-MM-dd");
                }

                var stops = await GetMockTrainStopsWithDetailAsync(trainCode);

                if (stops == null || !stops.Any())
                {
                    return null;
                }

                var random = new Random();

                var detailInfo = new TrainFullDetailInfo
                {
                    TrainCode = trainCode,
                    TrainType = GetTrainType(trainCode),
                    TrainBrand = trainCode.StartsWith("G") ? "复兴号" : trainCode.StartsWith("D") ? "和谐号" : "普速列车",
                    TrainModel = trainCode.StartsWith("G") ? "CR400AF" : trainCode.StartsWith("D") ? "CRH380B" : "25G型",
                    Consist = stops.Count > 12 ? "16节编组" : "8节编组",
                    MaxSpeed = trainCode.StartsWith("G") ? "350km/h" : trainCode.StartsWith("D") ? "250km/h" : "160km/h",
                    StartStation = stops.First().StationName,
                    EndStation = stops.Last().StationName,
                    DetailStops = stops,
                    TotalStops = stops.Count,
                    TotalDistance = $"{random.Next(800, 2500)}公里",
                    TotalDuration = $"{random.Next(3, 12)}小时{random.Next(10, 50)}分钟",
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
                    QueryTime = DateTime.Now
                };

                CalculateRealTimeStatusDetail(detailInfo);

                return detailInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError($"查询列车详细信息失败: {ex.Message}");
                return null;
            }
        }

        // ============================================================
        // 获取带详细信息的经停站
        // ============================================================
        private async Task<List<TrainStopDetail>> GetMockTrainStopsWithDetailAsync(string trainCode)
        {
            await Task.Delay(200);

            var routeNames = GetRouteData(trainCode);
            if (routeNames == null || !routeNames.Any()) return new List<TrainStopDetail>();

            var now = DateTime.Now;
            var random = new Random();
            var stops = new List<TrainStopDetail>();
            var stationDb = StationDataService.GetStationInfo();

            for (int i = 0; i < routeNames.Count; i++)
            {
                var name = routeNames[i];
                var stationInfo = StationDataService.GetStationInfo(name);

                var doorDirection = i == routeNames.Count - 1 ? "不固定" : (i % 2 == 0 ? "左侧" : "右侧");
                var platforms = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
                var platform = platforms[random.Next(platforms.Length)] + "站台";
                if (i == 0 || i == routeNames.Count - 1)
                {
                    platform = (i == 0 ? "始发" : "终到") + " · " + platform;
                }

                var stop = new TrainStopDetail
                {
                    StationNo = i + 1,
                    StationName = name,
                    StationInfo = stationInfo,
                    StationType = stationInfo?.Type ?? "普通站",
                    ArriveTime = i == 0 ? "始发" : now.AddMinutes(i * 12 + random.Next(0, 5)).ToString("HH:mm"),
                    DepartTime = i == routeNames.Count - 1 ? "终到" : now.AddMinutes(i * 12 + 3 + random.Next(0, 3)).ToString("HH:mm"),
                    StopTime = i == 0 || i == routeNames.Count - 1 ? "—" : $"{random.Next(2, 8)}分钟",
                    IsStart = i == 0,
                    IsEnd = i == routeNames.Count - 1,
                    DayOffset = i > 10 ? 1 : 0,
                    Platform = platform,
                    DoorDirection = doorDirection,
                    TrackNumber = $"{random.Next(1, 12)}股道",
                    WaitingArea = $"候车区{(char)('A' + random.Next(0, 4))}",
                    PlatformSide = doorDirection == "左侧" ? "⬅️ 左侧下车" : doorDirection == "右侧" ? "➡️ 右侧下车" : "⏺ 不固定",
                    LandmarkColor = i % 4 switch
                    {
                        0 => "🟡 黄色（8节正编）",
                        1 => "🟢 绿色（16节正编）",
                        2 => "🟣 紫色（8节反编）",
                        _ => "🔵 蓝色（16节反编）"
                    },
                    CarriageDirection = i % 2 == 0 ? "⏩ 向前（车头方向）" : "⏪ 向后（车尾方向）",
                    BoardingGuide = i == 0 ? "始发站，请根据车票信息在对应检票口候车" :
                                   i == routeNames.Count - 1 ? "终点站，请带好随身物品有序下车" :
                                   $"本站停靠{random.Next(2, 5)}分钟，请勿远离车门",
                    SpecialNote = i % 3 == 0 ? "⚠️ 站台间隙较大，注意脚下安全" :
                                 i % 5 == 0 ? "🚇 可换乘地铁" :
                                 i % 7 == 0 ? "♿ 无障碍电梯位于站台中部" : "",
                    NearbyTransport = stationInfo?.NearbyTransport ?? ""
                };

                stops.Add(stop);
            }

            return stops;
        }

        // ============================================================
        // 路线数据
        // ============================================================
        private List<string> GetRouteData(string trainCode)
        {
            var routes = new Dictionary<string, List<string>>
            {
                // 京沪高铁
                { "G1", new List<string> { "北京南", "天津南", "济南西", "南京南", "上海" } },
                { "G2", new List<string> { "上海", "南京南", "济南西", "天津南", "北京南" } },
                { "G3", new List<string> { "北京南", "济南西", "南京南", "无锡东", "上海" } },
                { "G4", new List<string> { "上海", "无锡东", "南京南", "济南西", "北京南" } },
                { "G5", new List<string> { "北京南", "天津南", "济南西", "徐州东", "南京南", "上海" } },
                { "G6", new List<string> { "上海", "南京南", "徐州东", "济南西", "天津南", "北京南" } },
                { "G7", new List<string> { "北京南", "天津南", "济南西", "徐州东", "南京南", "镇江南", "无锡东", "上海" } },
                { "G8", new List<string> { "上海", "无锡东", "镇江南", "南京南", "徐州东", "济南西", "天津南", "北京南" } },
                { "G9", new List<string> { "北京南", "济南西", "南京南", "苏州北", "上海虹桥" } },
                { "G10", new List<string> { "上海虹桥", "苏州北", "南京南", "济南西", "北京南" } },
                { "G11", new List<string> { "北京南", "天津南", "济南西", "徐州东", "南京南", "常州北", "无锡东", "上海虹桥" } },
                { "G12", new List<string> { "上海虹桥", "无锡东", "常州北", "南京南", "徐州东", "济南西", "天津南", "北京南" } },
                { "G13", new List<string> { "北京南", "济南西", "枣庄", "徐州东", "南京南", "上海" } },
                { "G14", new List<string> { "上海", "南京南", "徐州东", "枣庄", "济南西", "北京南" } },

                // 京广高铁
                { "G79", new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "广州南" } },
                { "G80", new List<string> { "广州南", "长沙南", "武汉", "郑州东", "石家庄", "北京西" } },
                { "G81", new List<string> { "北京西", "保定东", "石家庄", "郑州东", "武汉", "长沙南", "广州南" } },
                { "G82", new List<string> { "广州南", "长沙南", "武汉", "郑州东", "石家庄", "保定东", "北京西" } },
                { "G83", new List<string> { "北京西", "石家庄", "郑州", "武汉", "长沙", "广州" } },
                { "G84", new List<string> { "广州", "长沙", "武汉", "郑州", "石家庄", "北京西" } },
                { "G85", new List<string> { "北京西", "高碑店东", "石家庄", "郑州东", "武汉", "长沙南", "广州南" } },
                { "G86", new List<string> { "广州南", "长沙南", "武汉", "郑州东", "石家庄", "高碑店东", "北京西" } },
                { "G87", new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "衡阳东", "广州南" } },
                { "G88", new List<string> { "广州南", "衡阳东", "长沙南", "武汉", "郑州东", "石家庄", "北京西" } },

                // 京九高铁
                { "G89", new List<string> { "北京西", "济南西", "徐州东", "合肥南", "南昌西", "深圳北" } },
                { "G90", new List<string> { "深圳北", "南昌西", "合肥南", "徐州东", "济南西", "北京西" } },

                // 沪昆高铁
                { "G91", new List<string> { "上海虹桥", "杭州东", "南昌西", "长沙南", "贵阳北", "昆明南" } },
                { "G92", new List<string> { "昆明南", "贵阳北", "长沙南", "南昌西", "杭州东", "上海虹桥" } },
                { "G93", new List<string> { "上海虹桥", "嘉兴南", "杭州东", "金华", "南昌西", "长沙南", "贵阳北", "昆明南" } },
                { "G94", new List<string> { "昆明南", "贵阳北", "长沙南", "南昌西", "金华", "杭州东", "嘉兴南", "上海虹桥" } },

                // 西成高铁
                { "G95", new List<string> { "西安北", "汉中", "广元", "绵阳", "成都东" } },
                { "G96", new List<string> { "成都东", "绵阳", "广元", "汉中", "西安北" } },

                // 京张高铁
                { "G97", new List<string> { "北京北", "清河", "八达岭长城", "张家口" } },
                { "G98", new List<string> { "张家口", "八达岭长城", "清河", "北京北" } },

                // 沿海高铁
                { "G99", new List<string> { "北京南", "天津", "济南", "徐州", "南京", "上海", "杭州", "宁波", "福州", "厦门" } },
                { "G100", new List<string> { "厦门", "福州", "宁波", "杭州", "上海", "南京", "徐州", "济南", "天津", "北京南" } },

                // 成渝高铁
                { "G101", new List<string> { "成都东", "简阳南", "资阳北", "重庆西" } },
                { "G102", new List<string> { "重庆西", "资阳北", "简阳南", "成都东" } },
                { "G103", new List<string> { "成都东", "内江北", "重庆西" } },
                { "G104", new List<string> { "重庆西", "内江北", "成都东" } },

                // 哈大高铁
                { "G105", new List<string> { "哈尔滨西", "长春", "沈阳北", "大连北" } },
                { "G106", new List<string> { "大连北", "沈阳北", "长春", "哈尔滨西" } },
                { "G107", new List<string> { "哈尔滨西", "长春", "沈阳北", "鞍山西", "大连北" } },
                { "G108", new List<string> { "大连北", "鞍山西", "沈阳北", "长春", "哈尔滨西" } },

                // 普速列车
                { "Z1", new List<string> { "北京", "石家庄", "郑州", "武汉", "长沙", "广州" } },
                { "Z2", new List<string> { "广州", "长沙", "武汉", "郑州", "石家庄", "北京" } },
                { "Z3", new List<string> { "北京", "济南", "徐州", "南京", "上海" } },
                { "Z4", new List<string> { "上海", "南京", "徐州", "济南", "北京" } },
                { "Z5", new List<string> { "北京", "保定", "石家庄", "郑州", "武汉", "长沙", "广州" } },
                { "Z6", new List<string> { "广州", "长沙", "武汉", "郑州", "石家庄", "保定", "北京" } },

                { "T1", new List<string> { "北京", "保定", "石家庄", "邢台", "邯郸", "郑州", "武汉", "长沙" } },
                { "T2", new List<string> { "长沙", "武汉", "郑州", "邯郸", "邢台", "石家庄", "保定", "北京" } },
                { "T3", new List<string> { "北京", "天津", "济南", "徐州", "南京", "上海" } },
                { "T4", new List<string> { "上海", "南京", "徐州", "济南", "天津", "北京" } },

                { "K1", new List<string> { "北京", "廊坊", "天津", "沧州", "德州", "济南", "泰安", "徐州", "南京", "上海" } },
                { "K2", new List<string> { "上海", "南京", "徐州", "泰安", "济南", "德州", "沧州", "天津", "廊坊", "北京" } },
                { "K3", new List<string> { "北京", "保定", "石家庄", "邢台", "邯郸", "安阳", "郑州", "武汉", "长沙" } },
                { "K4", new List<string> { "长沙", "武汉", "郑州", "安阳", "邯郸", "邢台", "石家庄", "保定", "北京" } },

                // 动车组
                { "D1", new List<string> { "北京", "天津", "济南", "南京", "上海" } },
                { "D2", new List<string> { "上海", "南京", "济南", "天津", "北京" } },
                { "D3", new List<string> { "北京", "石家庄", "郑州", "武汉", "长沙", "广州" } },
                { "D4", new List<string> { "广州", "长沙", "武汉", "郑州", "石家庄", "北京" } },
                { "D5", new List<string> { "北京", "廊坊", "天津", "沧州", "德州", "济南", "南京", "上海" } },
                { "D6", new List<string> { "上海", "南京", "济南", "德州", "沧州", "天津", "廊坊", "北京" } },

                // 城际
                { "C1", new List<string> { "北京南", "亦庄", "武清", "天津" } },
                { "C2", new List<string> { "天津", "武清", "亦庄", "北京南" } },
                { "C3", new List<string> { "上海", "昆山南", "苏州", "无锡", "常州", "南京" } },
                { "C4", new List<string> { "南京", "常州", "无锡", "苏州", "昆山南", "上海" } },
                { "C5", new List<string> { "广州南", "庆盛", "深圳北", "福田" } },
                { "C6", new List<string> { "福田", "深圳北", "庆盛", "广州南" } },

                // 进港高铁
                { "G99", new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "广州南", "深圳北", "香港西九龙" } },
                { "G100", new List<string> { "香港西九龙", "深圳北", "广州南", "长沙南", "武汉", "郑州东", "石家庄", "北京西" } },
            };

            if (routes.ContainsKey(trainCode))
            {
                return routes[trainCode];
            }

            // 智能匹配：如果用户输入的车次不在列表中，返回一个默认路线
            if (trainCode.StartsWith("G") || trainCode.StartsWith("D") || trainCode.StartsWith("C"))
            {
                return routes["G1"];
            }
            else
            {
                return routes["Z1"];
            }
        }

        // ============================================================
        // 判断列车类型
        // ============================================================
        private string GetTrainType(string trainCode)
        {
            if (string.IsNullOrEmpty(trainCode)) return "未知";
            var first = trainCode[0];
            return first switch
            {
                'G' => "高速动车",
                'D' => "动车组",
                'C' => "城际列车",
                'Z' => "直达特快",
                'T' => "特快列车",
                'K' => "快速列车",
                'Y' => "旅游列车",
                'L' => "临时列车",
                _ => "普速列车"
            };
        }

        // ============================================================
        // 计算实时状态
        // ============================================================
        private void CalculateRealTimeStatusDetail(TrainFullDetailInfo info)
        {
            if (info.DetailStops == null || !info.DetailStops.Any()) return;

            var now = DateTime.Now;
            var totalStops = info.DetailStops.Count;
            var currentTime = now.TimeOfDay;

            int currentIndex = -1;

            for (int i = 0; i < info.DetailStops.Count; i++)
            {
                var stop = info.DetailStops[i];

                if (stop.IsStart)
                {
                    var departTime = ParseTime(stop.DepartTime);
                    if (departTime.HasValue && currentTime > departTime.Value)
                        currentIndex = i;
                    continue;
                }

                if (stop.IsEnd)
                {
                    var arriveTime = ParseTime(stop.ArriveTime);
                    if (arriveTime.HasValue && currentTime > arriveTime.Value)
                    {
                        currentIndex = i;
                        break;
                    }
                    continue;
                }

                var arrive = ParseTime(stop.ArriveTime);
                var depart = ParseTime(stop.DepartTime);

                if (arrive.HasValue && depart.HasValue)
                {
                    if (currentTime >= arrive.Value && currentTime < depart.Value)
                    {
                        currentIndex = i;
                        info.Status = "停靠中";
                        break;
                    }
                    else if (i < info.DetailStops.Count - 1)
                    {
                        var nextArrive = ParseTime(info.DetailStops[i + 1].ArriveTime);
                        if (currentTime >= depart.Value && nextArrive.HasValue && currentTime < nextArrive.Value)
                        {
                            currentIndex = i;
                            info.Status = "运行中";
                            break;
                        }
                    }
                }
            }

            if (currentIndex < 0)
            {
                var lastStop = info.DetailStops.Last();
                var lastArrive = ParseTime(lastStop.ArriveTime);
                if (lastArrive.HasValue && currentTime > lastArrive.Value)
                {
                    info.Status = "已到达";
                    info.CurrentStation = lastStop.StationName;
                    info.ProgressPercent = 100;
                    info.CurrentStationInfo = "🏁 终点站已到达";
                    info.DelayInfo = "正点";
                    return;
                }

                var firstStop = info.DetailStops.First();
                var firstDepart = ParseTime(firstStop.DepartTime);
                if (firstDepart.HasValue && currentTime < firstDepart.Value)
                {
                    info.Status = "未发车";
                    info.CurrentStation = firstStop.StationName;
                    info.CurrentStationInfo = $"🟢 始发站 · {firstStop.Platform}";
                    info.NextStation = info.DetailStops.Count > 1 ? info.DetailStops[1].StationName : "";
                    info.NextArriveTime = info.DetailStops.Count > 1 ? info.DetailStops[1].ArriveTime : "";
                    info.NextStationPlatform = info.DetailStops.Count > 1 ? info.DetailStops[1].Platform : "";
                    info.NextStationDoorSide = info.DetailStops.Count > 1 ? info.DetailStops[1].DoorDirection : "";
                    info.ProgressPercent = 0;
                    info.DelayInfo = "正点";
                    return;
                }
            }

            if (currentIndex >= 0 && currentIndex < info.DetailStops.Count)
            {
                var currentStop = info.DetailStops[currentIndex];
                info.CurrentStation = currentStop.StationName;
                info.ProgressPercent = (int)((double)(currentIndex + 1) / totalStops * 100);

                var statusIcons = new Dictionary<string, string>
                {
                    { "运行中", "🚄" },
                    { "停靠中", "🚉" },
                    { "已到达", "🏁" },
                    { "未发车", "🟢" }
                };
                info.CurrentStationInfo = $"{statusIcons.GetValueOrDefault(info.Status, "📍")} {currentStop.Platform} · {currentStop.WaitingArea} · {currentStop.PlatformSide}";

                if (info.Status == "停靠中")
                {
                    info.CurrentStationInfo += $" · 停靠{currentStop.StopTime}";
                    // 特殊提示
                    if (!string.IsNullOrEmpty(currentStop.SpecialNote))
                    {
                        info.CurrentStationInfo += $" · {currentStop.SpecialNote}";
                    }
                }

                // 下一站信息
                if (currentIndex + 1 < info.DetailStops.Count)
                {
                    var nextStop = info.DetailStops[currentIndex + 1];
                    info.NextStation = nextStop.StationName;
                    info.NextArriveTime = nextStop.ArriveTime;
                    info.NextStationPlatform = nextStop.Platform;
                    info.NextStationDoorSide = nextStop.DoorDirection;
                }
                else
                {
                    info.Status = "已到达";
                    info.NextStation = "";
                    info.NextArriveTime = "";
                    info.ProgressPercent = 100;
                    info.CurrentStationInfo = "🏁 终点站已到达";
                }
            }

            info.DelayInfo = new Random().Next(0, 6) == 0 ? $"晚点{new Random().Next(3, 15)}分钟" : "正点";
        }

        // ============================================================
        // 解析时间
        // ============================================================
        private TimeSpan? ParseTime(string timeStr)
        {
            if (string.IsNullOrEmpty(timeStr) || timeStr == "始发" || timeStr == "终到" || timeStr == "—")
                return null;

            try
            {
                var parts = timeStr.Split(':');
                if (parts.Length == 2)
                {
                    return new TimeSpan(int.Parse(parts[0]), int.Parse(parts[1]), 0);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
