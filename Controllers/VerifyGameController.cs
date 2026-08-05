using Microsoft.AspNetCore.Mvc;
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Controllers
{
    public class VerifyGameController : Controller
    {
        private readonly VerifyGameService _verifyGameService;

        public VerifyGameController(VerifyGameService verifyGameService)
        {
            _verifyGameService = verifyGameService;
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

        [HttpGet]
        public IActionResult GetChallenge(int level)
        {
            var challenge = _verifyGameService.GenerateChallenge(level);
            return Json(new { success = true, data = challenge });
        }
    }
}
