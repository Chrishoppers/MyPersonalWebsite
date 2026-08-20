using Microsoft.AspNetCore.Mvc;
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Controllers
{
    public partial class SolverController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> Upload()
        {
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var files = Request.Form.Files;
            if (files == null || files.Count == 0) return Json(new { success = false, message = "没有文件" });

            var file = files[0];
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var tmpDir = Path.Combine(Path.GetTempPath(), "mypersonal_ocr");
            try { Directory.CreateDirectory(tmpDir); } catch { }
            var fn = Path.Combine(tmpDir, DateTimeOffset.Now.ToUnixTimeMilliseconds() + ext);

            using (var fs = System.IO.File.Create(fn))
            {
                await file.OpenReadStream().CopyToAsync(fs);
            }

            // connection id from query
            var connectionId = Request.Form["connectionId"].FirstOrDefault() ?? string.Empty;

            var solver = HttpContext.RequestServices.GetService(typeof(SolverProcessingService)) as SolverProcessingService;
            if (solver == null) return Json(new { success = false, message = "服务不可用" });

            var problems = await solver.ProcessImageAsync(fn, connectionId);

            // Return lightweight problem list
            var list = problems.Select(p => new { id = p.Id, shortText = p.ShortText, fullText = p.FullText }).ToList();
            return Json(new { success = true, problems = list });
        }
    }
}
