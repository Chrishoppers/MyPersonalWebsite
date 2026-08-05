using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Models;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Controllers
{
    public class VerifyGameController : Controller
    {
        private readonly DataSyncService _dataSync;

        public VerifyGameController(DataSyncService dataSync)
        {
            _dataSync = dataSync;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.Username = HttpContext.Session.GetString("Username");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveScore(int score, int level, int maxCombo, int passed)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            try
            {
                var stats = await _dataSync.GetUserGameStatsAsync(userId.Value);

                if (stats == null)
                {
                    stats = new UserGameStats
                    {
                        UserId = userId.Value,
                        TotalPoints = score,
                        MaxCombo = maxCombo,
                        MaxLevel = level,
                        GamesPlayed = 1,
                        UpdatedAt = DateTime.Now
                    };
                    await _dataSync.AddUserGameStatsAsync(stats);
                }
                else
                {
                    if (score > stats.TotalPoints) stats.TotalPoints = score;
                    if (maxCombo > stats.MaxCombo) stats.MaxCombo = maxCombo;
                    if (level > stats.MaxLevel) stats.MaxLevel = level;
                    stats.GamesPlayed += 1;
                    stats.UpdatedAt = DateTime.Now;
                    await _dataSync.UpdateUserGameStatsAsync(stats);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存分数失败: {ex.Message}");
                return Json(new { success = false, message = "保存失败" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRanking()
        {
            try
            {
                var allStats = await _dataSync.GetAllUserGameStatsAsync();
                var users = await _dataSync.GetAllUsersAsync();

                var ranking = allStats
                    .Where(s => s.TotalPoints > 0)
                    .OrderByDescending(s => s.TotalPoints)
                    .ThenByDescending(s => s.MaxLevel)
                    .Take(50)
                    .Select(s =>
                    {
                        var user = users.FirstOrDefault(u => u.Id == s.UserId);
                        return new
                        {
                            userId = s.UserId,
                            username = user?.Username ?? "已删除用户",
                            avatarUrl = user?.AvatarUrl,
                            isAvatarApproved = user?.IsAvatarApproved ?? false,
                            totalPoints = s.TotalPoints,
                            maxCombo = s.MaxCombo,
                            maxLevel = s.MaxLevel,
                            gamesPlayed = s.GamesPlayed
                        };
                    })
                    .ToList();

                return Json(new { success = true, data = ranking });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取排行榜失败: {ex.Message}");
                return Json(new { success = false, data = new System.Collections.Generic.List<object>() });
            }
        }
    }
}
