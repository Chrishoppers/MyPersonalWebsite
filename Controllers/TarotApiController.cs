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
                await _tarotService.SaveReadingAsync(userId.Value, question, spread, body.Cards, result.Content, isDaily);

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

    public sealed class SetQuotaRequest
    {
        public int UserId { get; set; }
        public int Limit { get; set; }
    }
}
