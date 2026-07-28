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
        // ============================================================
// 获取支持的车次列表（自动补全）
// ============================================================
[HttpGet]
public IActionResult GetSuggestions(string query)
{
    var allTrains = new[]
    {
        "G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9", "G10",
        "G79", "G80", "G81", "G82", "G83", "G84",
        "G93", "G94", "G97", "G98", "G101", "G102", "G103", "G104", "G105", "G106",
        "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8",
        "Z1", "Z2", "Z3", "Z4", "Z5", "Z6", "Z7", "Z8",
        "T1", "T2", "T3", "T4", "T5", "T6",
        "K1", "K2", "K3", "K4", "K5", "K6", "K7", "K8",
        "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8"
    };

    if (string.IsNullOrEmpty(query))
        return Json(allTrains.Take(10));

    var results = allTrains
        .Where(t => t.StartsWith(query.ToUpper()))
        .Take(10)
        .ToList();

    return Json(results);
}

        // ============================================================
        // 列车查询页面（管理员专用）
        // ============================================================
        [HttpGet]
        public IActionResult Index()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
                return RedirectToAction("Login", "Auth");

            return View();
        }

        // ============================================================
        // 查询列车（AJAX）
        // ============================================================
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
    }
}
