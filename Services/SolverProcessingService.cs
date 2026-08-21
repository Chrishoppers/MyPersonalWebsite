// 改进 SolverProcessingService，支持更好的问题分割和 AI 解题

using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Hubs;
using MyPersonalWebsite.Models;
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
            var results = new List<DetectedProblem>();

            try
            {
                // 1. 通知开始
                await SendProgress(connectionId, "🔍 开始 OCR 识别...", 5);

                // 2. OCR 识别
                var ocrText = await _ocr.RecognizeAsync(imagePath);
                
                if (string.IsNullOrWhiteSpace(ocrText))
                {
                    await SendProgress(connectionId, "❌ OCR 识别失败，请重试", 0);
                    return results;
                }

                await SendProgress(connectionId, "✅ OCR 识别完成，开始分题...", 30);

                // 3. 分割题目
                var problems = SplitIntoProblems(ocrText);
                var total = problems.Count;

                if (total == 0)
                {
                    await SendProgress(connectionId, "⚠️ 未识别到题目，请确认图片清晰", 0);
                    return results;
                }

                await SendProgress(connectionId, $"📝 识别到 {total} 道题，开始解答...", 40);

                // 4. 逐题解答
                for (int i = 0; i < total; i++)
                {
                    var p = problems[i];
                    var percent = 40 + (int)((i + 1) / (float)total * 50);
                    
                    await SendProgress(connectionId, $"🤖 正在解答第 {i + 1}/{total} 题...", percent);

                    var prob = new DetectedProblem
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ShortText = GetPreview(p, 120),
                        FullText = p
                    };

                    try
                    {
                        var systemPrompt = @"你是一个专业的中学到高中阶段的解题专家（数学/物理/化学/生物/地理/历史/语文）。
要求：
1) 针对用户提供的题目，按步骤给出完整解题过程。
2) 每步包含简短的说明和所用公式/理由。
3) 如果有计算过程，给出关键计算并标注结果。
4) 最终答案用「答案：」开头，单独一行。
5) 输出使用中文，结构清晰，不要输出与题目无关的内容。";

                        var ai = await _deepSeek.GetAIResponseAsync(p, systemPrompt, maxTokens: 1500, temperature: 0.3);
                        prob.AiAnswer = ai;
                        prob.Steps = ParseSteps(ai);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ AI 解答第 {i + 1} 题失败: {ex.Message}");
                        prob.AiAnswer = "⚠️ 解答失败，请重试";
                    }

                    results.Add(prob);
                }

                await SendProgress(connectionId, "✅ 全部解答完成！", 100);

                // 5. 清理临时文件
                try { File.Delete(imagePath); } catch { }

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 处理失败: {ex.Message}");
                await SendProgress(connectionId, $"❌ 处理失败: {ex.Message}", 0);
                return results;
            }
        }

        private async Task SendProgress(string connectionId, string message, int percent)
        {
            if (!string.IsNullOrEmpty(connectionId))
            {
                try
                {
                    await _hub.Clients.Client(connectionId).SendAsync("SolverProgress", $"{message} ({percent}%)");
                }
                catch { }
            }
            Console.WriteLine($"📡 [{percent}%] {message}");
        }

        private List<string> SplitIntoProblems(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();

            // 清理文本
            text = text.Replace("\r\n", "\n").Trim();

            // 多种分割模式
            var patterns = new[]
            {
                // 数字编号：1. 2. 3.
                @"(?=^\s*\d+[\.、\s])",
                // 括号编号：(1) (2)
                @"(?=^\s*\(\d+\))",
                // 中文编号：一、二、三
                @"(?=^\s*[一二三四五六七八九十]+[、\.\s])",
                // 字母编号：A. B. C.
                @"(?=^\s*[A-Z][\.、\s])",
                // 多个换行
                @"(?=\n\n)"
            };

            var parts = new List<string>();
            var combined = text;

            foreach (var pattern in patterns)
            {
                var split = Regex.Split(combined, pattern, RegexOptions.Multiline)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 5)
                    .ToList();

                if (split.Count > 1)
                {
                    parts = split;
                    break;
                }
            }

            // 如果分割后只有一部分，尝试按空行分割
            if (parts.Count <= 1)
            {
                parts = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }

            // 如果还是只有一部分，尝试按句号分割
            if (parts.Count <= 1 && text.Length > 150)
            {
                var sentences = text.Split(new[] { '。', '！', '？' }, StringSplitOptions.RemoveEmptyEntries);
                if (sentences.Length > 2)
                {
                    var mid = sentences.Length / 2;
                    parts = new List<string>
                    {
                        string.Join("。", sentences.Take(mid)) + "。",
                        string.Join("。", sentences.Skip(mid)) + "。"
                    };
                }
            }

            return parts.Where(p => p.Length > 3).ToList();
        }

        private string GetPreview(string text, int maxLen = 100)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return text.Length <= maxLen ? text : text.Substring(0, maxLen) + "...";
        }

        private List<ProblemStep> ParseSteps(string answer)
        {
            var steps = new List<ProblemStep>();
            if (string.IsNullOrWhiteSpace(answer)) return steps;

            var lines = answer.Split('\n');
            var stepIndex = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                // 检测步骤标记
                var match = Regex.Match(trimmed, @"^步骤\s*(\d+)[：:\.\s]");
                if (match.Success)
                {
                    stepIndex++;
                    steps.Add(new ProblemStep
                    {
                        Index = int.Parse(match.Groups[1].Value),
                        Content = trimmed.Replace(match.Value, "").Trim()
                    });
                }
                else if (stepIndex > 0 && steps.Count > 0)
                {
                    // 追加到当前步骤
                    var last = steps[steps.Count - 1];
                    last.Content += "\n" + trimmed;
                }
                else
                {
                    // 非步骤内容
                    steps.Add(new ProblemStep
                    {
                        Index = stepIndex + 1,
                        Content = trimmed
                    });
                    stepIndex++;
                }
            }

            return steps;
        }
    }
}
