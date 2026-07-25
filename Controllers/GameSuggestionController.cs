using Microsoft.AspNetCore.Mvc;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Models;

namespace MyPersonalWebsite.Controllers
{
    public class GameSuggestionController : Controller
    {
        private readonly GameSuggestionService _suggestionService;
        private readonly DataSyncService _dataSync;

        public GameSuggestionController(GameSuggestionService suggestionService, DataSyncService dataSync)
        {
            _suggestionService = suggestionService;
            _dataSync = dataSync;
        }

        // ============================================================
        // 建议列表
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var suggestions = await _suggestionService.GetAllSuggestionsAsync();
            var users = await _dataSync.GetAllUsersAsync();

            var viewModel = new List<SuggestionViewModel>();
            foreach (var s in suggestions)
            {
                var user = users.FirstOrDefault(u => u.Id == s.UserId);
                var hasVoted = await _suggestionService.HasUserVotedAsync(s.Id, userId.Value);

                viewModel.Add(new SuggestionViewModel
                {
                    Suggestion = s,
                    Username = user?.Username ?? "已删除用户",
                    AvatarUrl = user?.AvatarUrl,
                    IsVoted = hasVoted,
                    VoteCount = s.Votes
                });
            }

            ViewBag.CurrentUserId = userId;
            ViewBag.Username = HttpContext.Session.GetString("Username");
            return View(viewModel);
        }

        // ============================================================
        // 提交建议
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Submit(string gameName, string description)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            if (string.IsNullOrEmpty(gameName) || gameName.Length < 2)
            {
                return Json(new { success = false, message = "游戏名称至少2个字符" });
            }

            var result = await _suggestionService.AddSuggestionAsync(userId.Value, gameName, description);
            if (result)
            {
                return Json(new { success = true, message = "建议已提交，感谢你的参与！" });
            }
            return Json(new { success = false, message = "提交失败，请重试" });
        }

        // ============================================================
        // 投票/取消投票
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> ToggleVote(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            var result = await _suggestionService.ToggleVoteAsync(id, userId.Value);
            if (result.Success)
            {
                // 获取最新投票数
                var suggestion = await _suggestionService.GetSuggestionByIdAsync(id);
                return Json(new { success = true, message = result.Message, votes = suggestion?.Votes ?? 0 });
            }
            return Json(new { success = false, message = result.Message });
        }

        // ============================================================
        // 管理员：更新状态
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var result = await _suggestionService.UpdateStatusAsync(id, status);
            return Json(new { success = result });
        }

        // ============================================================
        // 管理员：删除建议
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var result = await _suggestionService.DeleteSuggestionAsync(id);
            return Json(new { success = result });
        }
    }
}
