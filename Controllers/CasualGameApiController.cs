using Microsoft.AspNetCore.Mvc;
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Controllers
{
    [Route("api/casual")]
    public class CasualGameApiController : Controller
    {
        private readonly CasualGameService _casualGameService;

        public CasualGameApiController(CasualGameService casualGameService)
        {
            _casualGameService = casualGameService;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] CasualSubmitRequest? req)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { ok = false, msg = "请先登录" });

            var username = HttpContext.Session.GetString("Username") ?? "玩家";
            var result = await _casualGameService.SubmitAsync(
                userId.Value, username, req?.GameKey, req?.Score ?? 0, req?.Combo ?? 0);
            return Json(result);
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> Leaderboard(string? gameKey, int limit = 20)
        {
            var data = await _casualGameService.GetLeaderboardAsync(gameKey, limit);
            return Json(new { ok = true, data });
        }

        [HttpGet("mybest")]
        public async Task<IActionResult> MyBest(string? gameKey)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { ok = false, msg = "请先登录" });

            return Json(await _casualGameService.GetMyBestAsync(userId.Value, gameKey));
        }
    }

    public class CasualSubmitRequest
    {
        public string GameKey { get; set; } = "";
        public int Score { get; set; }
        public int Combo { get; set; }
    }
}
