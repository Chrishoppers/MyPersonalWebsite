using Microsoft.AspNetCore.Mvc;

namespace MyPersonalWebsite.Controllers
{
    public class TarotController : Controller
    {
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

        /// <summary>管理员塔罗管理面板。</summary>
        public IActionResult Admin()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }
    }
}
