using Microsoft.AspNetCore.Mvc;

namespace MyPersonalWebsite.Controllers
{
    public class PayController : Controller
    {
        public IActionResult Index(string? id)
        {
            ViewBag.OrderId = id ?? "空";
            return View();
        }
    }
}
