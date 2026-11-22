using Microsoft.AspNetCore.Mvc;

namespace MyPersonalWebsite.Controllers
{
    public class SpyGameController : Controller
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

        public IActionResult Room(string? code)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.RoomCode = code;
            return View();
        }
    }
}
