using Microsoft.AspNetCore.Mvc;

namespace MyPersonalWebsite.Controllers
{
    public class PayController : Controller
    {
        // 不带任何依赖，最简单
        public IActionResult Index(string? id)
        {
            // 直接返回文本，不依赖视图
            return Content($"✅ PayController 完全正常！参数：{id ?? "空"}，时间：{DateTime.Now}");
        }
    }
}
