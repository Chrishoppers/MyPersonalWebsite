using Microsoft.AspNetCore.Mvc;
using MyPersonalWebsite.Services;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System;

namespace MyPersonalWebsite.Controllers
{
    public class SolverController : Controller
    {
        private readonly DeepSeekService _deepSeek;
        private readonly SolverProcessingService _solverProcessing;

        public SolverController(DeepSeekService deepSeek, SolverProcessingService solverProcessing)
        {
            _deepSeek = deepSeek;
            _solverProcessing = solverProcessing;
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

        // Health endpoint for quick deployment checks. Returns 200 OK with a simple text body.
        [HttpGet("/Solver/health")]
        public IActionResult Health()
        {
            return Content("solver ok");
        }

        [HttpPost]
        public async Task<IActionResult> Ask(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return Json(new { success = false, message = "问题不能为空" });
            }

            var systemPrompt = @"你是一个中学到高中阶段的解题专家（数学/物理/化学）。
要求：
1) 针对用户提供的题目，按步骤给出完整解题过程，每步编号（例如：步骤1、步骤2...）。
2) 每步包含简短的说明和所用公式/理由；如果有计算过程，给出关键计算并标注结果。
3) 在最终答案后输出一个'可进一步解释的子步骤索引'，用中文短句列出哪些步骤可以点按展开（例如：步骤2 的某一小步说明）。
4) 输出尽量结构化，使用中文。不要输出与题目无关的内容。

现在开始解题，题目：";

            var gameContext = systemPrompt + question;
            var aiAnswer = await _deepSeek.GetAIResponseAsync(question, gameContext);
            return Json(new { success = true, answer = aiAnswer });
        }

        [HttpPost]
        public async Task<IActionResult> ExplainStep(string question, int stepIndex)
        {
            if (string.IsNullOrWhiteSpace(question))
                return Json(new { success = false, message = "参数缺失" });

            var prompt = $"题目：{question}\n请给出对 第{stepIndex}步 的详细讲解，包含思路和必要的例题或类比，要求通俗易懂。";
            var system = "你是解题讲解专家，回答要通俗、分条、有示例。";
            var ai = await _deepSeek.GetAIResponseAsync(prompt, system);
            return Json(new { success = true, explanation = ai });
        }

        [HttpPost]
        public async Task<IActionResult> Upload()
        {
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

            // connection id from form
            var connectionId = Request.Form["connectionId"].FirstOrDefault() ?? string.Empty;

            if (_solverProcessing == null) return Json(new { success = false, message = "服务不可用" });

            var problems = await _solverProcessing.ProcessImageAsync(fn, connectionId);

            var list = problems.Select(p => new { id = p.Id, shortText = p.ShortText, fullText = p.FullText, aiAnswer = p.AiAnswer }).ToList();
            return Json(new { success = true, problems = list });
        }
    }
}
