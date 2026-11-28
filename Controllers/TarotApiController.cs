using Microsoft.AspNetCore.Mvc;
using MyPersonalWebsite.Services.Tarot;

namespace MyPersonalWebsite.Controllers
{
    /// <summary>
    /// 塔罗牌 API：牌库 + AI 解读 + 占卜历史。
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

        /// <summary>POST /api/tarot/interpret —— AI 解读，并保存占卜历史。</summary>
        [HttpPost("interpret")]
        public async Task<IActionResult> Interpret([FromBody] InterpretRequest body)
        {
            if (body == null || body.Cards == null || body.Cards.Count == 0)
            {
                return BadRequest(new { success = false, message = "请先抽牌" });
            }

            var result = await _tarotService.InterpretAsync(
                body.Question ?? "", body.Spread ?? "凯尔特十字牌阵", body.Cards, body.Verdict);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue && result.Success && !string.IsNullOrWhiteSpace(result.Content))
            {
                await _tarotService.SaveReadingAsync(
                    userId.Value, body.Question ?? "", body.Spread ?? "凯尔特十字牌阵", body.Cards, result.Content);
            }

            return Ok(new { success = result.Success, fallback = result.Fallback, content = result.Content });
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
    }

    public sealed class InterpretRequest
    {
        public string? Question { get; set; }
        public string? Spread { get; set; }
        /// <summary>是否牌阵等玩法由前端计算出的初步结论（可空）。</summary>
        public string? Verdict { get; set; }
        public List<DrawnCardDto>? Cards { get; set; }
    }
}
