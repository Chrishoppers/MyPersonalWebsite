using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Controllers
{
    /// <summary>
    /// DeepSeek API 代理
    /// 前端不持有 Key，Key 只存在服务器
    /// </summary>
    [Route("api/deepseek")]
    [ApiController]
    public class DeepSeekProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<DeepSeekProxyController> _logger;

        public DeepSeekProxyController(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<DeepSeekProxyController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/deepseek/chat
        /// 请求体：{ messages: [{role, content}], temperature?, max_tokens? }
        /// </summary>
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] JsonElement body)
        {
            try
            {
                var apiKey = _config["DeepSeek:ApiKey"];
                var apiUrl = _config["DeepSeek:ApiUrl"] ?? "https://api.deepseek.com/v1/chat/completions";
                var model = _config["DeepSeek:Model"] ?? "deepseek-chat";

                if (string.IsNullOrEmpty(apiKey))
                {
                    return StatusCode(500, new { success = false, message = "DeepSeek API Key 未配置" });
                }

                /* ---- 读取请求体 ---- */
                var messages = body.TryGetProperty("messages", out var m) ? m : default;
                var temperature = body.TryGetProperty("temperature", out var t)
                    ? t.GetDouble() : 0.85;
                var maxTokens = body.TryGetProperty("max_tokens", out var mt)
                    ? mt.GetInt32() : 120;

                if (messages.ValueKind != JsonValueKind.Array)
                {
                    return BadRequest(new { success = false, message = "messages 必须是数组" });
                }

                /* ---- 构造 DeepSeek 请求 ---- */
                var payload = new
                {
                    model = model,
                    messages = messages,
                    temperature = temperature,
                    max_tokens = maxTokens,
                    stream = false
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Content = content
                };
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"DeepSeek 调用失败 ({response.StatusCode}): {responseBody}");
                    return StatusCode((int)response.StatusCode, new
                    {
                        success = false,
                        message = "DeepSeek 调用失败",
                        detail = responseBody
                    });
                }

                /* ---- 解析返回 ---- */
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                string reply = "";
                try
                {
                    reply = root.GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString() ?? "";
                }
                catch
                {
                    reply = "";
                }

                return Ok(new
                {
                    success = true,
                    content = reply
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeepSeek 代理异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}