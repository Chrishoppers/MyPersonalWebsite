using Microsoft.AspNetCore.Mvc;

namespace MyPersonalWebsite.Controllers
{
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            return Content("✅ 测试控制器工作正常！路径：/Test/Index", "text/plain");
        }
        
        public IActionResult Echo(string id)
        {
            return Content($"✅ 收到参数：{id ?? "空"}，路径：/Test/Echo/{{id}}", "text/plain");
        }
    }
}
