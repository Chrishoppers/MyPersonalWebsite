using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Hubs;
using System.Text.RegularExpressions;

namespace MyPersonalWebsite.Services
{
    public class SolverProcessingService
    {
        private readonly IOcrService _ocr;
        private readonly DeepSeekService _deepSeek;
        private readonly IHubContext<SolverHub> _hub;

        public SolverProcessingService(IOcrService ocr, DeepSeekService deepSeek, IHubContext<SolverHub> hub)
        {
            _ocr = ocr;
            _deepSeek = deepSeek;
            _hub = hub;
        }

        public async Task<List<DetectedProblem>> ProcessImageAsync(string imagePath, string connectionId = "")
        {
            // Notify start
            if (!string.IsNullOrEmpty(connectionId)) await _hub.Clients.Client(connectionId).SendAsync("SolverProgress", "开始 OCR 识别...");

            var ocrText = await _ocr.RecognizeAsync(imagePath);

            if (!string.IsNullOrWhiteSpace(connectionId)) await _hub.Clients.Client(connectionId).SendAsync("SolverProgress", "OCR 完成，开始分题...");

            var problems = SplitIntoProblems(ocrText);

            var results = new List<DetectedProblem>();
            int index = 1;
            foreach (var p in problems)
            {
                var id = Guid.NewGuid().ToString("N");
                var prob = new DetectedProblem { Id = id, ShortText = GetPreview(p), FullText = p };
                results.Add(prob);

                if (!string.IsNullOrEmpty(connectionId)) await _hub.Clients.Client(connectionId).SendAsync("SolverProgress", $"正在解析第 {index} 题...");

                // Call AI to solve each problem (system prompt tailored)
                var systemPrompt = @"你是一个中学到高中阶段的解题专家（数学/物理/化学）。
要求：
1) 针对用户提供的题目，按步骤给出完整解题过程，每步编号（例如：步骤1、步骤2...）。
2) 每步包含简短的说明和所用公式/理由；如果有计算过程，给出关键计算并标注结果。
3) 在最终答案后输出一个'可进一步解释的子步骤索引'，用中文短句列出哪些步骤可以点按展开。
4) 输出尽量结构化，使用中文。不要输出与题目无关的内容.";

                var ai = await _deepSeek.GetAIResponseAsync(p, systemPrompt, maxTokens: 1200, temperature: 0.2);
                prob.AiAnswer = ai;

                if (!string.IsNullOrEmpty(connectionId)) await _hub.Clients.Client(connectionId).SendAsync("SolverProgress", $"第 {index} 题处理完成");
                index++;
            }

            // Clean up image if temporary
            try { File.Delete(imagePath); } catch { }

            if (!string.IsNullOrEmpty(connectionId)) await _hub.Clients.Client(connectionId).SendAsync("SolverProgress", "全部完成");

            return results;
        }

        private List<string> SplitIntoProblems(string ocrText)
        {
            if (string.IsNullOrWhiteSpace(ocrText)) return new List<string>();

            // Simple heuristic: split by typical numbering patterns or double newlines
            var lines = ocrText.Replace("\r\n", "\n").Split('\n');
            var joined = string.Join("\n", lines.Select(l => l.Trim())).Trim();

            var pattern = @"(?=^\s*(?:\d+|\(|（|①|②|③|一|二|三)\s*[\.)、\s])";
            var options = RegexOptions.Multiline;
            var parts = Regex.Split(joined, pattern, options)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            // Fallback: if only one part, try split by double newline
            if (parts.Count <= 1)
            {
                parts = joined.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }

            // If still single and long, attempt to split by sentence length
            if (parts.Count <= 1 && joined.Length > 200)
            {
                var approx = joined.Length / 2;
                parts = new List<string> { joined.Substring(0, approx).Trim(), joined.Substring(approx).Trim() };
            }

            return parts;
        }

        private string GetPreview(string text, int len = 80)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return text.Length <= len ? text : text.Substring(0, len) + "...";
        }
    }
}
