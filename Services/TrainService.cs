using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyPersonalWebsite.Models;
using Microsoft.Extensions.Logging;

namespace MyPersonalWebsite.Services
{
    public class TrainService
    {
        private readonly ILogger<TrainService> _logger;
        private readonly Random _random = new();

        public TrainService(ILogger<TrainService> logger)
        {
            _logger = logger;
        }

        // ============================================================
        // 主查询方法
        // ============================================================
        public async Task<TrainFullDetailInfo?> QueryTrainAsync(string trainCode, string? date = null)
        {
            try
            {
                await Task.Delay(300);

                // 1. 先查预定义路线表
                var route = GetPredefinedRoute(trainCode);

                // 2. 如果不在表里，根据车次号智能生成
                if (route == null || !route.Any())
                {
                    route = GenerateRouteByNumber(trainCode);
                }

                if (route == null || !route.Any())
                    return null;

                // 3. 构建详细数据
                var stops = BuildDetailedStops(route, trainCode);
                var info = BuildTrainInfo(trainCode, stops);

                // 4. 计算实时状态
                CalculateRealTimeStatus(info);

                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError($"查询列车失败: {ex.Message}");
                return null;
            }
        }

        // ============================================================
        // 获取支持的车次列表（自动补全）
        // ============================================================
        public List<string> GetSupportedTrainCodes()
        {
            var all = new List<string>();
            var routes = GetAllPredefinedRoutes();
            all.AddRange(routes.Keys);
            return all.OrderBy(x => x).ToList();
        }

        // ============================================================
        // 获取所有预定义路线
        // ============================================================
        private Dictionary<string, List<string>> GetAllPredefinedRoutes()
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // ===== 京沪高铁 G1-G16 =====
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
                { "G11", new List<string> { "北京南", "天津南", "济南西", "徐州东", "南京南", "常州北", "无锡东", "上海虹桥" } },
                { "G12", new List<string> { "上海虹桥", "无锡东", "常州北", "南京南", "徐州东", "济南西", "天津南", "北京南" } },
                { "G13", new List<string> { "北京南", "济南西", "枣庄", "徐州东", "南京南", "上海虹桥" } },
                { "G14", new List<string> { "上海虹桥", "南京南", "徐州东", "枣庄", "济南西", "北京南" } },
                { "G15", new List<string> { "北京南", "天津南", "德州东", "济南西", "徐州东", "南京南", "上海虹桥" } },
                { "G16", new List<string> { "上海虹桥", "南京南", "徐州东", "济南西", "德州东", "天津南", "北京南" } },

                // ===== 京广高铁 G79-G90 =====
                { "G79", new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "广州南" } },
                { "G80", new List<string> { "广州南", "长沙南", "武汉", "郑州东", "石家庄", "北京西" } },
                { "G81", new List<string> { "北京西", "保定东", "石家庄", "郑州东", "武汉", "长沙南", "广州南" } },
                { "G82", new List<string> { "广州南", "长沙南", "武汉", "郑州东", "石家庄", "保定东", "北京西" } },
                { "G83", new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "衡阳东", "广州南" } },
                { "G84", new List<string> { "广州南", "衡阳东", "长沙南", "武汉", "郑州东", "石家庄", "北京西" } },
                { "G85", new List<string> { "北京西", "高碑店东", "石家庄", "郑州东", "武汉", "长沙南", "广州南" } },
                { "G86", new List<string> { "广州南", "长沙南", "武汉", "郑州东", "石家庄", "高碑店东", "北京西" } },
                { "G87", new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "衡阳东", "广州南" } },
                { "G88", new List<string> { "广州南", "衡阳东", "长沙南", "武汉", "郑州东", "石家庄", "北京西" } },
                { "G89", new List<string> { "北京西", "保定东", "石家庄", "郑州东", "武汉", "长沙南", "衡阳东", "广州南" } },
                { "G90", new List<string> { "广州南", "衡阳东", "长沙南", "武汉", "郑州东", "石家庄", "保定东", "北京西" } },

                // ===== 京九高铁 =====
                { "G91", new List<string> { "北京西", "济南西", "徐州东", "合肥南", "南昌西", "深圳北" } },
                { "G92", new List<string> { "深圳北", "南昌西", "合肥南", "徐州东", "济南西", "北京西" } },

                // ===== 沪昆高铁 =====
                { "G93", new List<string> { "上海虹桥", "杭州东", "南昌西", "长沙南", "贵阳北", "昆明南" } },
                { "G94", new List<string> { "昆明南", "贵阳北", "长沙南", "南昌西", "杭州东", "上海虹桥" } },
                { "G95", new List<string> { "上海虹桥", "嘉兴南", "杭州东", "金华", "南昌西", "长沙南", "贵阳北", "昆明南" } },
                { "G96", new List<string> { "昆明南", "贵阳北", "长沙南", "南昌西", "金华", "杭州东", "嘉兴南", "上海虹桥" } },

                // ===== 西成高铁 =====
                { "G97", new List<string> { "西安北", "汉中", "广元", "绵阳", "成都东" } },
                { "G98", new List<string> { "成都东", "绵阳", "广元", "汉中", "西安北" } },
                { "G99", new List<string> { "西安北", "阿房宫", "汉中", "广元", "绵阳", "成都东" } },
                { "G100", new List<string> { "成都东", "绵阳", "广元", "汉中", "阿房宫", "西安北" } },

                // ===== 京张高铁 =====
                { "G101", new List<string> { "北京北", "清河", "八达岭长城", "张家口" } },
                { "G102", new List<string> { "张家口", "八达岭长城", "清河", "北京北" } },

                // ===== 沿海高铁 =====
                { "G103", new List<string> { "北京南", "天津", "济南", "徐州", "南京", "上海", "杭州", "宁波", "福州", "厦门" } },
                { "G104", new List<string> { "厦门", "福州", "宁波", "杭州", "上海", "南京", "徐州", "济南", "天津", "北京南" } },

                // ===== 成渝高铁 =====
                { "G105", new List<string> { "成都东", "简阳南", "资阳北", "重庆西" } },
                { "G106", new List<string> { "重庆西", "资阳北", "简阳南", "成都东" } },
                { "G107", new List<string> { "成都东", "内江北", "重庆西" } },
                { "G108", new List<string> { "重庆西", "内江北", "成都东" } },

                // ===== 哈大高铁 =====
                { "G109", new List<string> { "哈尔滨西", "长春", "沈阳北", "大连北" } },
                { "G110", new List<string> { "大连北", "沈阳北", "长春", "哈尔滨西" } },
                { "G111", new List<string> { "哈尔滨西", "长春", "沈阳北", "鞍山西", "大连北" } },
                { "G112", new List<string> { "大连北", "鞍山西", "沈阳北", "长春", "哈尔滨西" } },

                // ===== 进港高铁 =====
                { "G113", new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "广州南", "深圳北", "香港西九龙" } },
                { "G114", new List<string> { "香港西九龙", "深圳北", "广州南", "长沙南", "武汉", "郑州东", "石家庄", "北京西" } },

                // ===== 京邕高铁（真实路线） =====
                { "G310", new List<string> { "南宁东", "柳州", "桂林", "长沙南", "武汉", "郑州东", "新乡东", "石家庄", "北京西" } },
                { "G311", new List<string> { "北京西", "石家庄", "新乡东", "郑州东", "武汉", "长沙南", "桂林", "柳州", "南宁东" } },

                // ===== 其它常用 G 字头 =====
                { "G115", new List<string> { "北京南", "天津南", "济南西", "南京南", "上海虹桥" } },
                { "G116", new List<string> { "上海虹桥", "南京南", "济南西", "天津南", "北京南" } },
                { "G117", new List<string> { "北京南", "天津南", "济南西", "徐州东", "南京南", "上海虹桥" } },
                { "G118", new List<string> { "上海虹桥", "南京南", "徐州东", "济南西", "天津南", "北京南" } },

                // ===== 普速 Z =====
                { "Z1", new List<string> { "北京", "石家庄", "郑州", "武汉", "长沙", "广州" } },
                { "Z2", new List<string> { "广州", "长沙", "武汉", "郑州", "石家庄", "北京" } },
                { "Z3", new List<string> { "北京", "济南", "徐州", "南京", "上海" } },
                { "Z4", new List<string> { "上海", "南京", "徐州", "济南", "北京" } },
                { "Z5", new List<string> { "北京", "保定", "石家庄", "郑州", "武汉", "长沙", "广州" } },
                { "Z6", new List<string> { "广州", "长沙", "武汉", "郑州", "石家庄", "保定", "北京" } },
                { "Z7", new List<string> { "北京", "天津", "济南", "徐州", "南京", "上海" } },
                { "Z8", new List<string> { "上海", "南京", "徐州", "济南", "天津", "北京" } },
                { "Z9", new List<string> { "北京", "石家庄", "郑州", "武汉", "长沙", "广州" } },
                { "Z10", new List<string> { "广州", "长沙", "武汉", "郑州", "石家庄", "北京" } },
                { "Z11", new List<string> { "北京", "天津", "济南", "南京", "上海" } },
                { "Z12", new List<string> { "上海", "南京", "济南", "天津", "北京" } },
                { "Z13", new List<string> { "北京", "保定", "石家庄", "邢台", "邯郸", "郑州", "武汉", "长沙", "广州" } },
                { "Z14", new List<string> { "广州", "长沙", "武汉", "郑州", "邯郸", "邢台", "石家庄", "保定", "北京" } },

                // ===== 普速 T =====
                { "T1", new List<string> { "北京", "保定", "石家庄", "邢台", "邯郸", "郑州", "武汉", "长沙" } },
                { "T2", new List<string> { "长沙", "武汉", "郑州", "邯郸", "邢台", "石家庄", "保定", "北京" } },
                { "T3", new List<string> { "北京", "天津", "济南", "徐州", "南京", "上海" } },
                { "T4", new List<string> { "上海", "南京", "徐州", "济南", "天津", "北京" } },
                { "T5", new List<string> { "北京", "石家庄", "郑州", "武汉", "长沙", "广州" } },
                { "T6", new List<string> { "广州", "长沙", "武汉", "郑州", "石家庄", "北京" } },
                { "T7", new List<string> { "北京", "保定", "石家庄", "邢台", "邯郸", "安阳", "郑州", "武汉", "长沙", "广州" } },
                { "T8", new List<string> { "广州", "长沙", "武汉", "郑州", "安阳", "邯郸", "邢台", "石家庄", "保定", "北京" } },

                // ===== 普速 K =====
                { "K1", new List<string> { "北京", "廊坊", "天津", "沧州", "德州", "济南", "泰安", "徐州", "南京", "上海" } },
                { "K2", new List<string> { "上海", "南京", "徐州", "泰安", "济南", "德州", "沧州", "天津", "廊坊", "北京" } },
                { "K3", new List<string> { "北京", "保定", "石家庄", "邢台", "邯郸", "安阳", "郑州", "武汉", "长沙" } },
                { "K4", new List<string> { "长沙", "武汉", "郑州", "安阳", "邯郸", "邢台", "石家庄", "保定", "北京" } },
                { "K5", new List<string> { "北京", "张家口", "大同", "呼和浩特" } },
                { "K6", new List<string> { "呼和浩特", "大同", "张家口", "北京" } },
                { "K7", new List<string> { "北京", "天津", "济南", "南京", "上海" } },
                { "K8", new List<string> { "上海", "南京", "济南", "天津", "北京" } },
                { "K9", new List<string> { "北京", "石家庄", "郑州", "武汉", "长沙", "广州" } },
                { "K10", new List<string> { "广州", "长沙", "武汉", "郑州", "石家庄", "北京" } },

                // ===== 城际 C =====
                { "C1", new List<string> { "北京南", "亦庄", "武清", "天津" } },
                { "C2", new List<string> { "天津", "武清", "亦庄", "北京南" } },
                { "C3", new List<string> { "上海", "昆山南", "苏州", "无锡", "常州", "南京" } },
                { "C4", new List<string> { "南京", "常州", "无锡", "苏州", "昆山南", "上海" } },
                { "C5", new List<string> { "广州南", "庆盛", "深圳北", "福田" } },
                { "C6", new List<string> { "福田", "深圳北", "庆盛", "广州南" } },
                { "C7", new List<string> { "成都东", "简阳南", "资阳北", "重庆西" } },
                { "C8", new List<string> { "重庆西", "资阳北", "简阳南", "成都东" } },

                // ===== 动车 D =====
                { "D1", new List<string> { "北京", "天津", "济南", "南京", "上海" } },
                { "D2", new List<string> { "上海", "南京", "济南", "天津", "北京" } },
                { "D3", new List<string> { "北京", "石家庄", "郑州", "武汉", "长沙", "广州" } },
                { "D4", new List<string> { "广州", "长沙", "武汉", "郑州", "石家庄", "北京" } },
                { "D5", new List<string> { "北京", "廊坊", "天津", "沧州", "德州", "济南", "南京", "上海" } },
                { "D6", new List<string> { "上海", "南京", "济南", "德州", "沧州", "天津", "廊坊", "北京" } },
                { "D7", new List<string> { "北京", "保定", "石家庄", "郑州", "武汉", "长沙" } },
                { "D8", new List<string> { "长沙", "武汉", "郑州", "石家庄", "保定", "北京" } },
                { "D9", new List<string> { "北京", "天津", "济南", "徐州", "南京", "上海" } },
                { "D10", new List<string> { "上海", "南京", "徐州", "济南", "天津", "北京" } },
            };
        }

        // ============================================================
        // 获取预定义路线
        // ============================================================
        private List<string>? GetPredefinedRoute(string trainCode)
        {
            var routes = GetAllPredefinedRoutes();
            return routes.TryGetValue(trainCode, out var route) ? route : null;
        }

        // ============================================================
        // 根据车次号智能生成路线
        // ============================================================
        private List<string>? GenerateRouteByNumber(string trainCode)
        {
            if (string.IsNullOrEmpty(trainCode)) return null;

            var firstChar = trainCode[0];
            var numberPart = trainCode.Length > 1 ? trainCode.Substring(1) : "";

            if (!int.TryParse(numberPart, out var num))
                return null;

            return firstChar switch
            {
                'G' => GetGNumberTemplate(num),
                'D' => GetDNumberTemplate(num),
                'Z' => GetZNumberTemplate(num),
                'T' => GetTNumberTemplate(num),
                'K' => GetKNumberTemplate(num),
                'C' => GetCNumberTemplate(num),
                _ => null
            };
        }

        // ============================================================
        // G 字头编号规律
        // ============================================================
        private List<string> GetGNumberTemplate(int num)
        {
            // G1-G16：京沪高铁
            if (num >= 1 && num <= 16)
                return new List<string> { "北京南", "天津南", "济南西", "徐州东", "南京南", "上海虹桥" };

            // G79-G90：京广高铁
            if (num >= 79 && num <= 90)
                return new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "广州南" };

            // G91-G92：京九高铁
            if (num >= 91 && num <= 92)
                return new List<string> { "北京西", "济南西", "徐州东", "合肥南", "南昌西", "深圳北" };

            // G93-G96：沪昆高铁
            if (num >= 93 && num <= 96)
                return new List<string> { "上海虹桥", "杭州东", "南昌西", "长沙南", "贵阳北", "昆明南" };

            // G97-G100：西成高铁
            if (num >= 97 && num <= 100)
                return new List<string> { "西安北", "汉中", "广元", "绵阳", "成都东" };

            // G101-G102：京张高铁
            if (num >= 101 && num <= 102)
                return new List<string> { "北京北", "清河", "八达岭长城", "张家口" };

            // G103-G104：沿海高铁
            if (num >= 103 && num <= 104)
                return new List<string> { "北京南", "天津", "济南", "徐州", "南京", "上海", "杭州", "宁波", "福州", "厦门" };

            // G105-G108：成渝高铁
            if (num >= 105 && num <= 108)
                return new List<string> { "成都东", "简阳南", "资阳北", "重庆西" };

            // G109-G112：哈大高铁
            if (num >= 109 && num <= 112)
                return new List<string> { "哈尔滨西", "长春", "沈阳北", "大连北" };

            // G113-G114：进港高铁
            if (num >= 113 && num <= 114)
                return new List<string> { "北京西", "石家庄", "郑州东", "武汉", "长沙南", "广州南", "深圳北", "香港西九龙" };

            // G200-G299：华东方向
            if (num >= 200 && num <= 299)
                return new List<string> { "上海虹桥", "杭州东", "南昌西", "长沙南" };

            // G300-G399：华南方向
            if (num >= 300 && num <= 399)
                return new List<string> { "南宁东", "柳州", "桂林", "长沙南", "武汉", "郑州东", "新乡东", "石家庄", "北京西" };

            // G400-G499：西南方向
            if (num >= 400 && num <= 499)
                return new List<string> { "成都东", "重庆西", "贵阳北", "昆明南" };

            // G500-G599：西北方向
            if (num >= 500 && num <= 599)
                return new List<string> { "西安北", "兰州西", "乌鲁木齐" };

            // G600-G699：东北方向
            if (num >= 600 && num <= 699)
                return new List<string> { "哈尔滨西", "长春", "沈阳北", "大连北" };

            // G700-G799：华中方向
            if (num >= 700 && num <= 799)
                return new List<string> { "郑州东", "武汉", "长沙南" };

            // 默认：京沪
            return new List<string> { "北京南", "天津南", "济南西", "南京南", "上海虹桥" };
        }

        // ============================================================
        // D 字头编号规律
        // ============================================================
        private List<string> GetDNumberTemplate(int num)
        {
            if (num >= 1 && num <= 99)
                return new List<string> { "北京", "天津", "济南", "南京", "上海" };
            if (num >= 100 && num <= 199)
                return new List<string> { "北京", "石家庄", "郑州", "武汉", "长沙" };
            if (num >= 200 && num <= 299)
                return new List<string> { "上海", "杭州", "宁波", "福州", "厦门" };
            if (num >= 300 && num <= 399)
                return new List<string> { "广州", "深圳", "香港西九龙" };
            if (num >= 400 && num <= 499)
                return new List<string> { "成都", "重庆" };
            return new List<string> { "北京", "天津", "济南", "南京", "上海" };
        }

        // ============================================================
        // Z/T/K/C 字头
        // ============================================================
        private List<string> GetZNumberTemplate(int num) =>
            new List<string> { "北京", "石家庄", "郑州", "武汉", "长沙", "广州" };

        private List<string> GetTNumberTemplate(int num) =>
            new List<string> { "北京", "保定", "石家庄", "邢台", "邯郸", "郑州", "武汉" };

        private List<string> GetKNumberTemplate(int num) =>
            new List<string> { "北京", "廊坊", "天津", "沧州", "德州", "济南", "徐州", "南京", "上海" };

        private List<string> GetCNumberTemplate(int num) =>
            new List<string> { "北京南", "亦庄", "武清", "天津" };

        // ============================================================
        // 构建详细的经停站数据
        // ============================================================
        private List<TrainStopDetail> BuildDetailedStops(List<string> routeNames, string trainCode)
        {
            var now = DateTime.Now;
            var stops = new List<TrainStopDetail>();
            var total = routeNames.Count;

            // 根据车型获取时间参数
            var (minInterval, maxInterval, minStop, maxStop, startHourMin, startHourMax) = trainCode[0] switch
            {
                'G' => (6, 12, 2, 4, 6, 9),
                'D' => (8, 15, 3, 6, 6, 9),
                'C' => (6, 10, 2, 4, 6, 9),
                'Z' => (10, 18, 4, 8, 6, 8),
                'T' => (12, 20, 5, 10, 6, 8),
                'K' => (15, 25, 6, 12, 5, 8),
                _ => (10, 20, 4, 8, 6, 8)
            };

            // 特殊车次固定发车时间
            var fixedTimes = new Dictionary<string, (int hour, int minute)>(StringComparer.OrdinalIgnoreCase)
            {
                { "G310", (10, 42) },
                { "G311", (7, 30) },
                { "G79", (8, 00) },
                { "G80", (9, 00) },
                { "G1", (7, 00) },
                { "G2", (8, 00) },
            };

            int startHour, startMinute;
            if (fixedTimes.TryGetValue(trainCode, out var fixedTime))
            {
                startHour = fixedTime.hour;
                startMinute = fixedTime.minute;
            }
            else
            {
                startHour = _random.Next(startHourMin, startHourMax);
                startMinute = _random.Next(0, 60);
            }

            var baseTime = new DateTime(now.Year, now.Month, now.Day, startHour, startMinute, 0);

            for (int i = 0; i < total; i++)
            {
                var name = routeNames[i];
                var stationInfo = StationDataService.GetStationInfo(name);

                // 计算累积时间
                int accumulatedMinutes = 0;
                for (int j = 0; j < i; j++)
                {
                    accumulatedMinutes += _random.Next(minInterval, maxInterval);
                }

                // 特殊车次固定到站时间
                var fixedArriveTimes = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    { "G310", new List<string> { "始发", "11:42", "12:42", "15:23", "16:38", "18:15", "18:40", "19:46", "20:49" } },
                    { "G311", new List<string> { "始发", "08:42", "09:42", "12:23", "13:38", "15:15", "15:40", "16:46", "17:49" } },
                };

                string arriveTime, departTime;

                if (fixedArriveTimes.TryGetValue(trainCode, out var times) && i < times.Count)
                {
                    arriveTime = times[i];
                    if (i == total - 1)
                        departTime = "终到";
                    else if (i == 0)
                        departTime = arriveTime;
                    else
                    {
                        var stopMin = _random.Next(minStop, maxStop);
                        var parts = arriveTime.Split(':');
                        if (parts.Length == 2)
                        {
                            var h = int.Parse(parts[0]);
                            var m = int.Parse(parts[1]) + stopMin;
                            if (m >= 60) { h++; m -= 60; }
                            departTime = $"{h:D2}:{m:D2}";
                        }
                        else
                            departTime = arriveTime;
                    }
                }
                else
                {
                    arriveTime = i == 0 ? "始发" : baseTime.AddMinutes(accumulatedMinutes - 2 + _random.Next(0, 3)).ToString("HH:mm");
                    departTime = i == total - 1 ? "终到" : baseTime.AddMinutes(accumulatedMinutes + _random.Next(minStop, maxStop) + 1).ToString("HH:mm");
                }

                string stopDuration;
                if (i == 0 || i == total - 1)
                    stopDuration = "—";
                else
                    stopDuration = $"{_random.Next(minStop, maxStop)}分钟";

                // 站台信息
                var platforms = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
                var platform = platforms[_random.Next(platforms.Length)] + "站台";
                if (i == 0) platform = "🟢 " + platform;
                if (i == total - 1) platform = "🏁 " + platform;

                // 开门方向
                var doorDirection = i == 0 || i == total - 1 ? "不固定" :
                                   (i % 2 == 0 ? "⬅️ 左侧开门" : "➡️ 右侧开门");

                // 地标颜色
                var landmarkColors = new[] { "🟡 黄色", "🟢 绿色", "🟣 紫色", "🔵 蓝色" };
                var landmarkColor = landmarkColors[i % landmarkColors.Length] + (i % 2 == 0 ? "（8节编组）" : "（16节编组）");

                // 特殊提示
                var specialNotes = new[] {
                    "",
                    "⚠️ 站台间隙较大，注意脚下安全",
                    "🚇 可换乘地铁",
                    "♿ 无障碍电梯位于站台中部",
                    "📢 请提前到车门等候",
                    "🚻 洗手间在站台两端",
                    "📶 全站覆盖Wi-Fi",
                    "🍱 站台有便利店",
                    "☕ 候车室有咖啡机",
                    "🚌 出站可换乘公交"
                };

                // 站台侧信息
                var platformSides = new[] { "左侧", "右侧", "岛式站台", "侧式站台" };
                var platformSide = platformSides[i % platformSides.Length];

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
                    IsEnd = i == total - 1,
                    DayOffset = i > 12 ? 1 : 0,
                    Platform = platform,
                    DoorDirection = doorDirection,
                    TrackNumber = $"{_random.Next(1, 12)}股道",
                    WaitingArea = $"候车区{(char)('A' + _random.Next(0, 5))}",
                    PlatformSide = platformSide,
                    LandmarkColor = landmarkColor,
                    CarriageDirection = i % 2 == 0 ? "⏩ 向前（车头方向）" : "⏪ 向后（车尾方向）",
                    BoardingGuide = i == 0 ? "始发站，请根据车票信息在对应检票口候车" :
                                   i == total - 1 ? "终点站，请带好随身物品有序下车" :
                                   $"本站停靠{stopDuration}，请勿远离车门",
                    SpecialNote = specialNotes[_random.Next(specialNotes.Length)],
                    NearbyTransport = stationInfo?.NearbyTransport ?? ""
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
            var config = firstChar switch
            {
                'G' => ("复兴号", "CR400AF", "350km/h", "高速动车", 1000, 2500),
                'D' => ("和谐号", "CRH380B", "250km/h", "动车组", 800, 2000),
                'C' => ("和谐号", "CRH6A", "200km/h", "城际列车", 300, 800),
                'Z' => ("直达特快", "25T型", "160km/h", "直达特快", 1500, 3000),
                'T' => ("特快", "25K型", "140km/h", "特快列车", 1000, 2500),
                'K' => ("快速", "25G型", "120km/h", "快速列车", 800, 2200),
                _ => ("普速", "25B型", "100km/h", "普速列车", 500, 1500)
            };

            var (brand, model, maxSpeed, trainType, minDist, maxDist) = config;

            // 特殊车次固定距离和时长
            var fixedInfo = new Dictionary<string, (int dist, int hours, int mins)>(StringComparer.OrdinalIgnoreCase)
            {
                { "G310", (2200, 10, 7) },
                { "G311", (2200, 10, 19) },
                { "G79", (2298, 8, 30) },
                { "G80", (2298, 8, 30) },
                { "G1", (1318, 4, 55) },
                { "G2", (1318, 4, 55) },
            };

            int totalDistance, totalHours, totalMinutes;

            if (fixedInfo.TryGetValue(trainCode, out var info))
            {
                totalDistance = info.dist;
                totalHours = info.hours;
                totalMinutes = info.mins;
            }
            else
            {
                totalDistance = _random.Next(minDist, maxDist);
                totalHours = _random.Next(3, 12);
                totalMinutes = _random.Next(10, 50);
            }

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
            var firstStop = info.DetailStops.First();
            var lastStop = info.DetailStops.Last();

            // 特殊车次固定发车时间
            var fixedDepartures = new Dictionary<string, (int hour, int minute)>(StringComparer.OrdinalIgnoreCase)
            {
                { "G310", (10, 42) },
                { "G311", (7, 30) },
                { "G79", (8, 00) },
                { "G80", (9, 00) },
                { "G1", (7, 00) },
                { "G2", (8, 00) },
            };

            DateTime? startTime = null;

            // 尝试从固定时间获取
            if (fixedDepartures.TryGetValue(info.TrainCode, out var fixedTime))
            {
                startTime = new DateTime(now.Year, now.Month, now.Day, fixedTime.hour, fixedTime.minute, 0);
            }
            else
            {
                // 尝试从停站数据解析
                if (!string.IsNullOrEmpty(firstStop.DepartTime) && firstStop.DepartTime != "始发" && firstStop.DepartTime != "—")
                {
                    try
                    {
                        var parts = firstStop.DepartTime.Split(':');
                        if (parts.Length == 2)
                        {
                            var hour = int.Parse(parts[0]);
                            var minute = int.Parse(parts[1]);
                            startTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
                            if (startTime > now.AddHours(2))
                                startTime = startTime.Value.AddDays(-1);
                        }
                    }
                    catch { }
                }
            }

            if (!startTime.HasValue)
                startTime = new DateTime(now.Year, now.Month, now.Day, 6, 0, 0);

            // 计算总行程时间（分钟）
            int totalMinutes = 0;
            for (int i = 0; i < total - 1; i++)
            {
                var current = info.DetailStops[i];
                var next = info.DetailStops[i + 1];

                if (!string.IsNullOrEmpty(current.DepartTime) && current.DepartTime != "始发" && current.DepartTime != "—" &&
                    !string.IsNullOrEmpty(next.ArriveTime) && next.ArriveTime != "终到" && next.ArriveTime != "—")
                {
                    try
                    {
                        var depParts = current.DepartTime.Split(':');
                        var arrParts = next.ArriveTime.Split(':');
                        if (depParts.Length == 2 && arrParts.Length == 2)
                        {
                            var depMin = int.Parse(depParts[0]) * 60 + int.Parse(depParts[1]);
                            var arrMin = int.Parse(arrParts[0]) * 60 + int.Parse(arrParts[1]);
                            var diff = arrMin - depMin;
                            if (diff < 0) diff += 24 * 60;
                            totalMinutes += diff;
                        }
                    }
                    catch { }
                }
            }

            // 如果无法计算，使用估算值
            if (totalMinutes == 0)
            {
                // 根据列车类型估算每站间隔
                var avgInterval = info.TrainCode[0] switch
                {
                    'G' => 10,
                    'D' => 12,
                    'C' => 8,
                    'Z' => 15,
                    'T' => 18,
                    'K' => 22,
                    _ => 15
                };
                totalMinutes = (total - 1) * avgInterval;
            }

            var elapsedMinutes = (now - startTime.Value).TotalMinutes;
            var progress = Math.Min(100, Math.Max(0, elapsedMinutes / totalMinutes * 100));
            progress = Math.Min(100, progress + _random.Next(-3, 5));

            info.ProgressPercent = (int)Math.Round(progress);

            // 确定当前所处的站段
            var index = (int)(progress / 100 * (total - 1));
            index = Math.Max(0, Math.Min(total - 2, index));

            if (progress < 1)
            {
                info.Status = "未发车";
                info.CurrentStation = firstStop.StationName;
                info.CurrentStationInfo = $"🟢 始发站 · {firstStop.Platform}";
                info.NextStation = info.DetailStops.Count > 1 ? info.DetailStops[1].StationName : "";
                info.NextArriveTime = info.DetailStops.Count > 1 ? info.DetailStops[1].ArriveTime : "";
                info.NextStationPlatform = info.DetailStops.Count > 1 ? info.DetailStops[1].Platform : "";
                info.NextStationDoorSide = info.DetailStops.Count > 1 ? info.DetailStops[1].DoorDirection : "";
                info.DelayInfo = "正点";
                return;
            }

            if (progress > 98)
            {
                info.Status = "已到达";
                info.CurrentStation = lastStop.StationName;
                info.CurrentStationInfo = "🏁 终点站已到达";
                info.NextStation = "";
                info.NextArriveTime = "";
                info.ProgressPercent = 100;
                info.DelayInfo = "正点";
                return;
            }

            var currentStop = info.DetailStops[index];
            var nextStop = info.DetailStops[index + 1];

            info.CurrentStation = currentStop.StationName;
            info.NextStation = nextStop.StationName;
            info.NextArriveTime = nextStop.ArriveTime;
            info.NextStationPlatform = nextStop.Platform;
            info.NextStationDoorSide = nextStop.DoorDirection;

            // 判断是否停靠中
            bool isStopping = false;
            if (!string.IsNullOrEmpty(currentStop.ArriveTime) && currentStop.ArriveTime != "始发" && currentStop.ArriveTime != "—")
            {
                try
                {
                    var arrParts = currentStop.ArriveTime.Split(':');
                    if (arrParts.Length == 2)
                    {
                        var arrHour = int.Parse(arrParts[0]);
                        var arrMin = int.Parse(arrParts[1]);
                        var arriveTime = new DateTime(now.Year, now.Month, now.Day, arrHour, arrMin, 0);

                        int stopMin = 2;
                        if (currentStop.StopTime != "—" && currentStop.StopTime.EndsWith("分钟"))
                        {
                            var stopStr = currentStop.StopTime.Replace("分钟", "");
                            if (int.TryParse(stopStr, out var sm)) stopMin = sm;
                        }

                        var departTime = arriveTime.AddMinutes(stopMin);
                        isStopping = now >= arriveTime && now <= departTime;
                    }
                }
                catch { }
            }

            info.Status = isStopping ? "停靠中" : "运行中";

            var statusIcons = new Dictionary<string, string>
            {
                { "运行中", "🚄" },
                { "停靠中", "🚉" },
                { "已到达", "🏁" },
                { "未发车", "🟢" }
            };

            var platformDisplay = currentStop.Platform;
            if (!string.IsNullOrEmpty(platformDisplay))
                platformDisplay = platformDisplay.Replace("🟢 ", "").Replace("🏁 ", "");

            info.CurrentStationInfo = $"{statusIcons.GetValueOrDefault(info.Status, "📍")} {platformDisplay} · {currentStop.WaitingArea}";

            if (isStopping)
            {
                info.CurrentStationInfo += $" · 停靠{currentStop.StopTime}";
                if (!string.IsNullOrEmpty(currentStop.SpecialNote))
                    info.CurrentStationInfo += $" · {currentStop.SpecialNote}";
            }

            // 晚点模拟（10%概率）
            info.DelayInfo = _random.Next(0, 100) < 10 ? $"晚点{_random.Next(3, 20)}分钟" : "正点";

            if (isStopping)
            {
                var stopProgress = (double)index / (total - 1) * 100;
                info.ProgressPercent = (int)Math.Round(stopProgress);
            }
        }
    }
}
