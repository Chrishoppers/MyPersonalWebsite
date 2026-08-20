using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MyPersonalWebsite.Services
{
    public class DeepSeekService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl;
        private readonly string _defaultModel;

        public DeepSeekService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["DeepSeek:ApiKey"] ?? string.Empty;
            _apiUrl = config["DeepSeek:ApiUrl"] ?? "https://api.deepseek.com/v1/chat/completions";
            _defaultModel = config["DeepSeek:Model"] ?? "deepseek-chat";
        }

        /// <summary>
        /// 获取 AI 回复（支持自定义 system prompt）
        /// </summary>
        public async Task<string> GetAIResponseAsync(string userMessage, string? systemPrompt = null, int maxTokens = 800, double temperature = 0.6)
        {
            try
            {
                // 若未提供 systemPrompt，则使用兼容旧行为的狼人杀默认提示
                var system = systemPrompt ?? $@"你是一个狼人杀游戏的AI陪玩玩家。你的名字叫'AI小蓝'。\n\n规则：\n1. 你是一个普通的平民玩家，没有特殊技能\n2. 你会根据其他玩家的发言做出合理反应\n3. 你会积极参与讨论和投票\n4. 你的发言要自然、简短（20-40字），像真人玩家一样\n5. 不要暴露你是AI\n6. 你会表现出一定的情绪（紧张、兴奋、怀疑等）\n7. 你会根据游戏阶段调整发言策略\n\n请根据玩家的发言，给出一个自然的回复。只回复内容，不要加任何前缀。";

                var request = new
                {
                    model = _defaultModel,
                    messages = new[]
                    {
                        new { role = "system", content = system },
                        new { role = "user", content = userMessage }
                    },
                    temperature = temperature,
                    max_tokens = maxTokens,
                    stream = false
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.Clear();
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                }

                var response = await _httpClient.PostAsync(_apiUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;
                    try
                    {
                        var result = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                        return result ?? GetFallbackResponse(userMessage);
                    }
                    catch
                    {
                        // 如果返回格式不符合预期，直接返回原始文本
                        return responseBody;
                    }
                }

                Console.WriteLine($"DeepSeek API 错误: {responseBody}");
                return GetFallbackResponse(userMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeepSeek 请求失败: {ex.Message}");
                return GetFallbackResponse(userMessage);
            }
        }

        private string GetFallbackResponse(string userMessage)
        {
            var fallbacks = new[]
            {
                "嗯，我觉得你说得有道理！",
                "我同意，大家觉得呢？",
                "这个情况确实需要讨论一下。",
                "我有点不确定，大家怎么看？",
            };

            var idx = new Random().Next(0, fallbacks.Length);
            return fallbacks[idx];
        }
    }
}
