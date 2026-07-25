using Microsoft.AspNetCore.Mvc;

namespace MyPersonalWebsite.Controllers
{
    public class GameController : Controller
    {
        // ============================================================
        // 🎮 游戏中心主页
        // ============================================================
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            // 获取用户游戏统计数据（后续扩展）
            ViewBag.Username = HttpContext.Session.GetString("Username");
            
            return View();
        }
    }
}
