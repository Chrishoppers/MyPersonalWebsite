using Microsoft.AspNetCore.Mvc;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Controllers
{
    public class TrainController : Controller
    {
        private readonly TrainService _trainService;

        public TrainController(TrainService trainService)
        {
            _trainService = trainService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Query([FromBody] TrainQueryRequest request)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            if (string.IsNullOrEmpty(request.TrainCode) || request.TrainCode.Length < 2)
                return Json(new { success = false, message = "请输入有效的车次号" });

            var result = await _trainService.QueryTrainAsync(request.TrainCode.ToUpper(), request.Date);

            if (result == null)
                return Json(new { success = false, message = "未找到该车次信息" });

            return Json(new { success = true, data = result });
        }

        [HttpGet]
        public IActionResult GetSuggestions(string query)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new List<string>());

            var allTrains = _trainService.GetSupportedTrainCodes();

            if (string.IsNullOrEmpty(query))
                return Json(allTrains.Take(10));

            var results = allTrains
                .Where(t => t.StartsWith(query.ToUpper()))
                .Take(10)
                .ToList();

            return Json(results);
        }

        // ============================================================
        // 🪑 座席状态查询
        // ============================================================
        [HttpPost]
        public IActionResult GetSeatStatus([FromBody] SeatQueryRequest request)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return Json(new { success = false, message = "权限不足" });

            if (string.IsNullOrEmpty(request.SeatNumber))
                return Json(new { success = false, message = "请输入座位号" });

            // 解析座位号：如 "5车3A" 或 "3A"
            var seat = ParseSeatNumber(request.SeatNumber);

            if (seat == null)
                return Json(new { success = false, message = "座位号格式错误，请使用如 5车3A 或 3A 的格式" });

            // 模拟座位状态
            var status = GenerateSeatStatus(seat);

            return Json(new { success = true, data = status });
        }

        private SeatInfo? ParseSeatNumber(string input)
        {
            input = input.Trim().ToUpper();

            // 尝试匹配 "5车3A" 或 "3A"
            var patterns = new[]
            {
                // "5车3A" -> carriage=5, row=3, letter=A
                System.Text.RegularExpressions.Regex.Match(input, @"^(\d+)车(\d+)([A-D])$"),
                // "3A" -> carriage=1, row=3, letter=A
                System.Text.RegularExpressions.Regex.Match(input, @"^(\d+)([A-D])$"),
            };

            foreach (var match in patterns)
            {
                if (match.Success)
                {
                    var seat = new SeatInfo();
                    if (match.Groups.Count >= 4)
                    {
                        seat.Carriage = int.Parse(match.Groups[1].Value);
                        seat.Row = int.Parse(match.Groups[2].Value);
                        seat.Letter = match.Groups[3].Value;
                    }
                    else if (match.Groups.Count >= 3)
                    {
                        seat.Carriage = 1;
                        seat.Row = int.Parse(match.Groups[1].Value);
                        seat.Letter = match.Groups[2].Value;
                    }
                    else
                    {
                        return null;
                    }

                    // 验证字母范围
                    if (!new[] { "A", "B", "C", "D", "E", "F" }.Contains(seat.Letter))
                        return null;

                    return seat;
                }
            }

            return null;
        }

        private SeatStatus GenerateSeatStatus(SeatInfo seat)
        {
            var random = new Random();
            var statuses = new[] { "已售", "可售", "已选" };
            var status = statuses[random.Next(statuses.Length)];

            var positions = new Dictionary<string, string>
            {
                { "A", "靠窗" },
                { "C", "过道" },
                { "D", "过道" },
                { "F", "靠窗" },
                { "B", "中间" },
                { "E", "中间" }
            };

            var position = positions.ContainsKey(seat.Letter) ? positions[seat.Letter] : "中间";

            // 模拟价格（不同车厢价格不同）
            var price = random.Next(150, 600);

            var seatStatus = new SeatStatus
            {
                SeatNumber = $"{seat.Carriage}车{seat.Row}{seat.Letter}",
                Carriage = seat.Carriage,
                Row = seat.Row,
                Letter = seat.Letter,
                Status = status,
                Position = position,
                Price = $"¥{price}",
                IsAvailable = status == "可售",
                IsSelected = status == "已选",
                IsSold = status == "已售",
                CarriageType = seat.Carriage % 2 == 0 ? "二等座" : "一等座",
                Features = new List<string>()
            };

            // 根据座位类型添加特性
            if (seatStatus.CarriageType == "一等座")
            {
                seatStatus.Features.Add("💺 更宽敞");
                seatStatus.Features.Add("🔌 电源插座");
                seatStatus.Features.Add("📺 独立显示屏");
            }
            else
            {
                seatStatus.Features.Add("💺 标准座位");
                if (random.Next(0, 100) > 60)
                    seatStatus.Features.Add("🔌 电源插座");
            }

            if (position == "靠窗")
                seatStatus.Features.Add("🪟 靠窗");
            else if (position == "过道")
                seatStatus.Features.Add("🚶 过道");

            return seatStatus;
        }

        public class SeatQueryRequest
        {
            public string SeatNumber { get; set; } = string.Empty;
        }

        public class SeatInfo
        {
            public int Carriage { get; set; }
            public int Row { get; set; }
            public string Letter { get; set; } = string.Empty;
        }

        public class SeatStatus
        {
            public string SeatNumber { get; set; } = string.Empty;
            public int Carriage { get; set; }
            public int Row { get; set; }
            public string Letter { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Position { get; set; } = string.Empty;
            public string Price { get; set; } = string.Empty;
            public bool IsAvailable { get; set; }
            public bool IsSelected { get; set; }
            public bool IsSold { get; set; }
            public string CarriageType { get; set; } = string.Empty;
            public List<string> Features { get; set; } = new();
        }
    }
}
