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
    }
}
