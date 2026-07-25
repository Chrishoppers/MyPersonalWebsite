using Microsoft.AspNetCore.Mvc;
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Controllers
{
    public class DailyQuestionController : Controller
    {
        private readonly DailyQuestionService _dailyService;

        public DailyQuestionController(DailyQuestionService dailyService)
        {
            _dailyService = dailyService;
        }

        // ============================================================
        // 每日一问主页
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var status = await _dailyService.GetTodayStatusAsync(userId.Value);
            var ranking = await _dailyService.GetRankingAsync(20);

            ViewBag.Status = status;
            ViewBag.Ranking = ranking;
            ViewBag.Username = HttpContext.Session.GetString("Username");

            return View();
        }

        // ============================================================
        // 提交答案（AJAX）
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> SubmitAnswer(string answer)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            var result = await _dailyService.SubmitAnswerAsync(userId.Value, answer);
            return Json(new
            {
                success = result.Success,
                isCorrect = result.IsCorrect,
                points = result.Points,
                message = result.Message,
                correctAnswer = result.CorrectAnswer
            });
        }

        // ============================================================
        // 获取排行榜（AJAX）
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetRanking(int limit = 20)
        {
            var ranking = await _dailyService.GetRankingAsync(limit);
            return Json(new { success = true, data = ranking });
        }

        // ============================================================
        // 获取今日状态（AJAX）
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetTodayStatus()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            var status = await _dailyService.GetTodayStatusAsync(userId.Value);
            return Json(new { success = true, data = status });
        }
    }
}
