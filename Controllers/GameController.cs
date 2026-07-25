using Microsoft.AspNetCore.Mvc;

namespace MyPersonalWebsite.Controllers
{
    public class GameController : Controller
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
    }
}
