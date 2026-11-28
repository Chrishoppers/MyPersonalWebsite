using Microsoft.AspNetCore.Mvc;
using MyPersonalWebsite.Services.Tarot;

namespace MyPersonalWebsite.Controllers
{
    /// <summary>
    /// 塔罗牌 API：牌库 + AI 解读 + 占卜历史 + 配额/每日一签 + 管理员接口。
    /// </summary>
    [Route("api/tarot")]
    [ApiController]
    public class TarotApiController : ControllerBase
    {
        private readonly TarotService _tarotService;

        public TarotApiController(TarotService tarotService)
        {
            _tarotService = tarotService;
        }

        /// <summary>GET /api/tarot/deck —— 返回 78 张牌元数据（含正逆位牌义关键词）。</summary>
        [HttpGet("deck")]
        public IActionResult Deck() => Ok(new { success = true, cards = TarotDeck.ForClient() });

        /// <summary>POST /api/tarot/interpret —— AI 解读，含配额校验与每日一签缓存。</summary>
        [HttpPost("interpret")]
        public async Task<IActionResult> Interpret([FromBody] InterpretRequest body)
        {
            if (body == null || body.Cards == null || body.Cards.Count == 0)
            {
                return BadRequest(new { success = false, message = "请先抽牌" });
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Ok(new { success = false, message = "请先登录" });
            }

            var spread = body.Spread ?? "凯尔特十字牌阵";
            var isDaily = body.IsDaily;
            var question = body.Question ?? "";

            if (isDaily)
            {
                // 每日一签：每天只能测一次，已测过直接返回缓存结果
                var cached = await _tarotService.GetTodayDailyAsync(userId.Value);
                if (cached != null)
                {
                    return Ok(new
                    {
                        success = true,
                        fallback = false,
                        content = cached.ResultJson,
                        errorDetail = "",
                        cached = true,
                        cardsJson = cached.CardsJson,
                    });
                }
            }
            else
            {
                // 普通测试：每日限次
                var limit = await _tarotService.GetDailyLimitAsync(userId.Value);
                var used = await _tarotService.GetTodayCountAsync(userId.Value);
                if (used >= limit)
                {
                    return Ok(new { success = false, message = $"今日塔罗测试次数已用完（{used}/{limit}），请明天再来。" });
                }
            }

            // 相似提问检测（短时间内重复问同一问题会越来越不准确）
            string? warning = null;
            if (!isDaily)
            {
                warning = await _tarotService.GetSimilarQuestionWarningAsync(userId.Value, question);
            }

            var result = await _tarotService.InterpretAsync(question, spread, body.Cards, body.Verdict);

            if (result.Success && !string.IsNullOrWhiteSpace(result.Content))
            {
                await _tarotService.SaveReadingAsync(userId.Value, question, spread, body.Cards, result.Content, isDaily, result.PromptTokens, result.CompletionTokens);

                // 非每日一签：若本次用完了额度，发一次通知 + 邮件
                if (!isDaily)
                {
                    var usedAfter = await _tarotService.GetTodayCountAsync(userId.Value);
                    var limitAfter = await _tarotService.GetDailyLimitAsync(userId.Value);
                    if (usedAfter >= limitAfter)
                    {
                        await _tarotService.NotifyQuotaExhaustedAsync(userId.Value, limitAfter);
                    }
                }
            }

            return Ok(new
            {
                success = result.Success,
                fallback = result.Fallback,
                content = result.Content,
                errorDetail = result.ErrorDetail,
                cached = false,
                warning = warning,
            });
        }

        /// <summary>POST /api/tarot/followup —— 追问/补充说明（消耗 1 次占卜次数）。</summary>
        [HttpPost("followup")]
        public async Task<IActionResult> Followup([FromBody] FollowupRequest body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Message) || body.Cards == null || body.Cards.Count == 0)
            {
                return BadRequest(new { success = false, message = "请输入追问内容" });
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Ok(new { success = false, message = "请先登录" });
            }

            // 实时额度检测（追问同样消耗 1 次）
            var limit = await _tarotService.GetDailyLimitAsync(userId.Value);
            var used = await _tarotService.GetTodayCountAsync(userId.Value);
            if (used >= limit)
            {
                return Ok(new { success = false, message = $"今日塔罗测试次数已用完（{used}/{limit}），追问也需消耗次数，请明天再来。" });
            }

            var spread = body.Spread ?? "凯尔特十字牌阵";
            var result = await _tarotService.InterpretFollowupAsync(
                body.OriginalQuestion ?? "", spread, body.PreviousContent ?? "", body.Cards, body.Message.Trim());

            if (result.Success && !string.IsNullOrWhiteSpace(result.Content))
            {
                await _tarotService.SaveReadingAsync(userId.Value, body.Message.Trim(), spread + " · 追问", body.Cards, result.Content, false, result.PromptTokens, result.CompletionTokens);

                var usedAfter = await _tarotService.GetTodayCountAsync(userId.Value);
                var limitAfter = await _tarotService.GetDailyLimitAsync(userId.Value);
                if (usedAfter >= limitAfter)
                {
                    await _tarotService.NotifyQuotaExhaustedAsync(userId.Value, limitAfter);
                }
            }

            return Ok(new { success = result.Success, content = result.Content, fallback = result.Fallback });
        }

        /// <summary>GET /api/tarot/status —— 当前用户配额、今日已用次数、今日每日一签结果。</summary>
        [HttpGet("status")]
        public async Task<IActionResult> Status()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Ok(new { success = false, loggedIn = false });
            }

            var limit = await _tarotService.GetDailyLimitAsync(userId.Value);
            var used = await _tarotService.GetTodayCountAsync(userId.Value);
            var todayDaily = await _tarotService.GetTodayDailyAsync(userId.Value);

            return Ok(new
            {
                success = true,
                loggedIn = true,
                dailyLimit = limit,
                todayCount = used,
                remaining = Math.Max(0, limit - used),
                todayDaily = todayDaily,
            });
        }

        /// <summary>GET /api/tarot/history?limit=20 —— 当前用户的占卜历史。</summary>
        [HttpGet("history")]
        public async Task<IActionResult> History(int limit = 20)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Ok(new { success = false, list = new List<TarotHistoryItem>() });
            }

            var list = await _tarotService.GetHistoryAsync(userId.Value, limit);
            return Ok(new { success = true, list });
        }

        // ============================================================
        // 管理员接口
        // ============================================================

        [HttpGet("admin/readings")]
        public async Task<IActionResult> AdminReadings(int limit = 300)
        {
            if ((HttpContext.Session.GetInt32("IsAdmin") ?? 0) != 1)
                return Ok(new { success = false, message = "无权限" });

            var list = await _tarotService.GetAllReadingsAsync(limit);
            return Ok(new { success = true, list });
        }

        [HttpGet("admin/quotas")]
        public async Task<IActionResult> AdminQuotas()
        {
            if ((HttpContext.Session.GetInt32("IsAdmin") ?? 0) != 1)
                return Ok(new { success = false, message = "无权限" });

            var list = await _tarotService.GetAllQuotasAsync();
            return Ok(new { success = true, list });
        }

        [HttpPost("admin/quota")]
        public async Task<IActionResult> AdminSetQuota([FromBody] SetQuotaRequest body)
        {
            if ((HttpContext.Session.GetInt32("IsAdmin") ?? 0) != 1)
                return Ok(new { success = false, message = "无权限" });

            if (body == null || body.UserId <= 0)
                return Ok(new { success = false, message = "参数错误" });

            var limit = Math.Clamp(body.Limit, 0, 1000);
            await _tarotService.SetDailyLimitAndNotifyAsync(body.UserId, limit);
            return Ok(new { success = true, message = $"已将该用户每日次数调整为 {limit} 次，并已通知。" });
        }

        /// <summary>GET /api/tarot/admin/stats —— 按用户统计 token 用量与估算金额。</summary>
        [HttpGet("admin/stats")]
        public async Task<IActionResult> AdminStats()
        {
            if ((HttpContext.Session.GetInt32("IsAdmin") ?? 0) != 1)
                return Ok(new { success = false, message = "无权限" });

            var list = await _tarotService.GetStatsAsync();
            return Ok(new { success = true, list });
        }

        /// <summary>GET /api/tarot/admin/export-readings —— 导出占卜记录 CSV。</summary>
        [HttpGet("admin/export-readings")]
        public async Task<IActionResult> ExportReadings()
        {
            if ((HttpContext.Session.GetInt32("IsAdmin") ?? 0) != 1)
                return Content("无权限");

            var list = await _tarotService.GetAllReadingsAsync(2000);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ID,用户名,用户ID,问题,牌阵,抽牌,解读,时间,输入Tokens,输出Tokens,是否每日一签");
            foreach (var r in list)
            {
                var cards = "";
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(r.CardsJson);
                    cards = string.Join("; ", doc.RootElement.EnumerateArray().Select(x =>
                        $"{x.GetProperty("positionName").GetString()}·{x.GetProperty("name").GetString()}{(x.GetProperty("reversed").GetBoolean() ? "(逆)" : "")}"));
                }
                catch { cards = r.CardsJson; }
                var row = new[] { r.Id.ToString(), CsvEscape(r.Username), r.UserId.ToString(), CsvEscape(r.Question), CsvEscape(r.Spread), CsvEscape(cards), CsvEscape(r.ResultJson), CsvEscape(r.CreatedAt), r.PromptTokens.ToString(), r.CompletionTokens.ToString(), r.IsDaily ? "是" : "否" };
                sb.AppendLine(string.Join(",", row));
            }
            var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"tarot_readings_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        /// <summary>GET /api/tarot/admin/export-stats —— 导出成本统计 CSV。</summary>
        [HttpGet("admin/export-stats")]
        public async Task<IActionResult> ExportStats()
        {
            if ((HttpContext.Session.GetInt32("IsAdmin") ?? 0) != 1)
                return Content("无权限");

            var list = await _tarotService.GetStatsAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("用户名,用户ID,调用次数,输入Tokens,输出Tokens,总Tokens,估算金额(元)");
            foreach (var s in list)
            {
                var row = new[] { CsvEscape(s.Username), s.UserId.ToString(), s.CallCount.ToString(), s.PromptTokens.ToString(), s.CompletionTokens.ToString(), s.TotalTokens.ToString(), s.Cost.ToString("F4") };
                sb.AppendLine(string.Join(",", row));
            }
            var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"tarot_stats_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        private static string CsvEscape(string s)
        {
            s = s ?? "";
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }

    public sealed class InterpretRequest
    {
        public string? Question { get; set; }
        public string? Spread { get; set; }
        /// <summary>是否牌阵等玩法由前端计算出的初步结论（可空）。</summary>
        public string? Verdict { get; set; }
        /// <summary>是否为每日一签。</summary>
        public bool IsDaily { get; set; }
        public List<DrawnCardDto>? Cards { get; set; }
    }

    public sealed class FollowupRequest
    {
        public string? OriginalQuestion { get; set; }
        public string? Spread { get; set; }
        public string? PreviousContent { get; set; }
        public string? Message { get; set; }
        public List<DrawnCardDto>? Cards { get; set; }
    }

    public sealed class SetQuotaRequest
    {
        public int UserId { get; set; }
        public int Limit { get; set; }
    }
}
