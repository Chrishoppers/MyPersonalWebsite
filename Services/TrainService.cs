using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MyPersonalWebsite.Models;

namespace MyPersonalWebsite.Services
{
    public class TrainService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TrainService> _logger;

        // 使用免费可用的火车票查询API（国内可访问）
        private const string API_URL = "https://kyfw.12306.cn/otn/";

        public TrainService(HttpClient httpClient, ILogger<TrainService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        // ============================================================
        // 查询列车完整信息（主方法）
        // ============================================================
        public async Task<TrainFullInfo?> QueryTrainAsync(string trainCode, string? date = null)
        {
            try
            {
                if (string.IsNullOrEmpty(date))
                {
                    date = DateTime.Now.ToString("yyyy-MM-dd");
                }

                // 使用第三方免费API（这里用模拟数据演示）
                // 实际生产环境可替换为真实API
                var stops = await GetMockTrainStopsAsync(trainCode);

                if (stops == null || !stops.Any())
                {
                    return null;
                }

                var fullInfo = new TrainFullInfo
                {
                    TrainCode = trainCode,
                    TrainType = GetTrainType(trainCode),
                    StartStation = stops.First().StationName,
                    EndStation = stops.Last().StationName,
                    Stops = stops,
                    QueryTime = DateTime.Now
                };

                CalculateRealTimeStatus(fullInfo);

                return fullInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError($"查询列车信息失败: {ex.Message}");
                return null;
            }
        }

        // ============================================================
        // 模拟数据（实际使用请替换为真实API）
        // ============================================================
        private async Task<List<TrainStop>?> GetMockTrainStopsAsync(string trainCode)
        {
            // 模拟网络延迟
            await Task.Delay(300);

            var now = DateTime.Now;

            // 根据车次返回不同路线
            var routes = GetRouteData(trainCode);

            if (routes == null) return null;

            return routes.Select((s, index) => new TrainStop
            {
                StationNo = index + 1,
                StationName = s.StationName,
                ArriveTime = index == 0 ? "始发" : now.AddMinutes(index * 15).ToString("HH:mm"),
                DepartTime = index == routes.Count - 1 ? "终到" : now.AddMinutes(index * 15 + 3).ToString("HH:mm"),
                StopTime = index == 0 || index == routes.Count - 1 ? "—" : $"{new Random().Next(2, 8)}分钟",
                IsStart = index == 0,
                IsEnd = index == routes.Count - 1,
                DayOffset = index > 10 ? 1 : 0
            }).ToList();
        }

        // ============================================================
        // 路线数据
        // ============================================================
        private List<(string StationName)> GetRouteData(string trainCode)
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

                // 京广高铁
                { "G79", new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "广州南" } },
                { "G80", new List<string> { "广州南", "长沙南", "武汉", "郑州东", "石家庄", "北京西" } },
                { "G81", new List<string> { "北京西", "保定东", "石家庄", "郑州东", "武汉", "长沙南", "广州南" } },
                { "G82", new List<string> { "广州南", "长沙南", "武汉", "郑州东", "石家庄", "保定东", "北京西" } },

                // 京九高铁
                { "G89", new List<string> { "北京西", "济南西", "徐州东", "合肥南", "南昌西", "深圳北" } },
                { "G90", new List<string> { "深圳北", "南昌西", "合肥南", "徐州东", "济南西", "北京西" } },

                // 京沪高铁（更多经停）
                { "G7", new List<string> { "北京南", "天津南", "济南西", "徐州东", "南京南", "镇江南", "无锡东", "上海" } },
                { "G8", new List<string> { "上海", "无锡东", "镇江南", "南京南", "徐州东", "济南西", "天津南", "北京南" } },

                // 沪昆高铁
                { "G85", new List<string> { "上海虹桥", "杭州东", "南昌西", "长沙南", "贵阳北", "昆明南" } },
                { "G86", new List<string> { "昆明南", "贵阳北", "长沙南", "南昌西", "杭州东", "上海虹桥" } },

                // 西成高铁
                { "G89", new List<string> { "西安北", "汉中", "广元", "绵阳", "成都东" } },
                { "G90", new List<string> { "成都东", "绵阳", "广元", "汉中", "西安北" } },

                // 普速列车
                { "Z1", new List<string> { "北京", "石家庄", "郑州", "武汉", "长沙", "广州" } },
                { "Z2", new List<string> { "广州", "长沙", "武汉", "郑州", "石家庄", "北京" } },
                { "Z3", new List<string> { "北京", "济南", "徐州", "南京", "上海" } },
                { "Z4", new List<string> { "上海", "南京", "徐州", "济南", "北京" } },
                { "T1", new List<string> { "北京", "保定", "石家庄", "邢台", "邯郸", "郑州", "武汉", "长沙" } },
                { "T2", new List<string> { "长沙", "武汉", "郑州", "邯郸", "邢台", "石家庄", "保定", "北京" } },
                { "K1", new List<string> { "北京", "廊坊", "天津", "沧州", "德州", "济南", "泰安", "徐州", "南京", "上海" } },
                { "K2", new List<string> { "上海", "南京", "徐州", "泰安", "济南", "德州", "沧州", "天津", "廊坊", "北京" } },

                // 更多高铁
                { "G97", new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "衡阳东", "广州南" } },
                { "G98", new List<string> { "广州南", "衡阳东", "长沙南", "武汉", "郑州东", "石家庄", "北京西" } },
                { "G99", new List<string> { "北京西", "保定东", "石家庄", "邢台东", "郑州东", "武汉", "长沙南", "广州南" } },
                { "G100", new List<string> { "广州南", "长沙南", "武汉", "郑州东", "邢台东", "石家庄", "保定东", "北京西" } },

                // 京张高铁
                { "G88", new List<string> { "北京北", "清河", "八达岭长城", "张家口" } },
                { "G89", new List<string> { "张家口", "八达岭长城", "清河", "北京北" } },

                // 沿海高铁
                { "G73", new List<string> { "北京南", "天津", "济南", "徐州", "南京", "上海", "杭州", "宁波", "福州", "厦门" } },
                { "G74", new List<string> { "厦门", "福州", "宁波", "杭州", "上海", "南京", "徐州", "济南", "天津", "北京南" } },

                // 新增常用车次
                { "G21", new List<string> { "北京南", "济南西", "南京南", "上海" } },
                { "G22", new List<string> { "上海", "南京南", "济南西", "北京南" } },
                { "G25", new List<string> { "北京南", "天津南", "济南西", "徐州东", "南京南", "上海" } },
                { "G26", new List<string> { "上海", "南京南", "徐州东", "济南西", "天津南", "北京南" } },
                { "G27", new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "广州南" } },
                { "G28", new List<string> { "广州南", "长沙南", "武汉", "郑州东", "石家庄", "北京西" } },

                { "D1", new List<string> { "北京", "天津", "济南", "南京", "上海" } },
                { "D2", new List<string> { "上海", "南京", "济南", "天津", "北京" } },
                { "D3", new List<string> { "北京", "石家庄", "郑州", "武汉", "长沙", "广州" } },
                { "D4", new List<string> { "广州", "长沙", "武汉", "郑州", "石家庄", "北京" } },
            };

            if (routes.ContainsKey(trainCode))
            {
                return routes[trainCode].Select(s => (s)).ToList();
            }

            // 默认返回京沪线
            return routes["G1"].Select(s => (s)).ToList();
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
        // 计算实时状态（核心）
        // ============================================================
        private void CalculateRealTimeStatus(TrainFullInfo info)
        {
            if (info.Stops == null || !info.Stops.Any()) return;

            var now = DateTime.Now;
            var totalStops = info.Stops.Count;

            // 解析当前时间
            var currentTime = now.TimeOfDay;

            // 找到当前应该在哪一站
            int currentIndex = -1;

            for (int i = 0; i < info.Stops.Count; i++)
            {
                var stop = info.Stops[i];

                // 跳过始发站和终点站的特殊标识
                if (stop.ArriveTime == "始发" || stop.DepartTime == "终到")
                {
                    if (stop.IsStart)
                    {
                        // 检查是否已过始发站
                        var departTime = ParseTime(stop.DepartTime);
                        if (departTime.HasValue && currentTime > departTime.Value)
                        {
                            currentIndex = i;
                        }
                    }
                    if (stop.IsEnd)
                    {
                        var arriveTime = ParseTime(stop.ArriveTime);
                        if (arriveTime.HasValue && currentTime > arriveTime.Value)
                        {
                            currentIndex = i;
                            break;
                        }
                    }
                    continue;
                }

                var arrive = ParseTime(stop.ArriveTime);
                var depart = ParseTime(stop.DepartTime);

                if (arrive.HasValue && depart.HasValue)
                {
                    // 如果当前时间在到达和发车之间 → 停靠中
                    if (currentTime >= arrive.Value && currentTime < depart.Value)
                    {
                        currentIndex = i;
                        info.Status = "停靠中";
                        break;
                    }
                    // 如果当前时间在发车之后，到达之前（区间运行）
                    else if (i < info.Stops.Count - 1)
                    {
                        var nextArrive = ParseTime(info.Stops[i + 1].ArriveTime);
                        if (currentTime >= depart.Value && nextArrive.HasValue && currentTime < nextArrive.Value)
                        {
                            currentIndex = i;
                            info.Status = "运行中";
                            break;
                        }
                    }
                }
            }

            // 如果没找到，判断是否已到终点
            if (currentIndex < 0)
            {
                var lastStop = info.Stops.Last();
                var lastArrive = ParseTime(lastStop.ArriveTime);
                if (lastArrive.HasValue && currentTime > lastArrive.Value)
                {
                    info.Status = "已到达";
                    info.CurrentStation = lastStop.StationName;
                    info.ProgressPercent = 100;
                    return;
                }

                // 还没发车
                var firstStop = info.Stops.First();
                var firstDepart = ParseTime(firstStop.DepartTime);
                if (firstDepart.HasValue && currentTime < firstDepart.Value)
                {
                    info.Status = "未发车";
                    info.CurrentStation = firstStop.StationName;
                    info.NextStation = info.Stops.Count > 1 ? info.Stops[1].StationName : "";
                    info.NextArriveTime = info.Stops.Count > 1 ? info.Stops[1].ArriveTime : "";
                    info.ProgressPercent = 0;
                    return;
                }
            }

            // 更新状态
            if (currentIndex >= 0 && currentIndex < info.Stops.Count)
            {
                var currentStop = info.Stops[currentIndex];
                info.CurrentStation = currentStop.StationName;

                // 计算进度百分比
                info.ProgressPercent = (int)((double)(currentIndex + 1) / totalStops * 100);

                // 下一站
                if (currentIndex + 1 < info.Stops.Count)
                {
                    var nextStop = info.Stops[currentIndex + 1];
                    info.NextStation = nextStop.StationName;
                    info.NextArriveTime = nextStop.ArriveTime;

                    // 如果状态是"停靠中"，下一站就是当前站（停靠结束后才出发）
                    if (info.Status == "停靠中")
                    {
                        // 检查是否已过发车时间
                        var depart = ParseTime(currentStop.DepartTime);
                        if (depart.HasValue && currentTime > depart.Value)
                        {
                            info.Status = "运行中";
                            info.NextStation = nextStop.StationName;
                            info.NextArriveTime = nextStop.ArriveTime;
                        }
                    }
                }
                else
                {
                    info.Status = "已到达";
                    info.NextStation = "";
                    info.NextArriveTime = "";
                    info.ProgressPercent = 100;
                }

                // 检查是否晚点（模拟）
                info.DelayInfo = new Random().Next(0, 5) == 0 ? $"晚点{new Random().Next(3, 15)}分钟" : "正点";
            }
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
                // 处理 HH:mm 格式
                var parts = timeStr.Split(':');
                if (parts.Length == 2)
                {
                    var hours = int.Parse(parts[0]);
                    var minutes = int.Parse(parts[1]);
                    return new TimeSpan(hours, minutes, 0);
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
