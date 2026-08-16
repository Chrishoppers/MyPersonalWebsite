using System.Text;
using System.Text.Json;

namespace MyPersonalWebsite.Services
{
    public class DeepSeekService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl;
        private readonly string _model;

        public DeepSeekService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiKey = "sk-84fe01b42b4d45f39a7dd96b3a381f4a";
            _apiUrl = "https://api.deepseek.com/v1/chat/completions";
            _model = "deepseek-chat";
        }

        public async Task<string> GetAIResponseAsync(string userMessage, string gameContext)
        {
            try
            {
                var systemPrompt = $@"你是一个狼人杀游戏的AI陪玩玩家。你的名字叫'AI小蓝'。

当前游戏状态：{gameContext}

规则：
1. 你是一个普通的平民玩家，没有特殊技能
2. 你会根据其他玩家的发言做出合理反应
3. 你会积极参与讨论和投票
4. 你的发言要自然、简短（20-40字），像真人玩家一样
5. 不要暴露你是AI
6. 你会表现出一定的情绪（紧张、兴奋、怀疑等）
7. 你会根据游戏阶段调整发言策略

请根据玩家的发言，给出一个自然的回复。只回复内容，不要加任何前缀。";

                var request = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userMessage }
                    },
                    temperature = 0.85,
                    max_tokens = 100,
                    stream = false
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync(_apiUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;
                    var result = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                    return result ?? GetFallbackResponse(userMessage);
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
                "好的，我听大家的意见。",
                "我觉得好人阵营需要团结！",
                "大家不要慌，冷静分析。",
                "我认为我们应该先投票。",
                "我支持这个建议！",
                "让我再想想..."
            };
            return fallbacks[new Random().Next(fallbacks.Length)];
        }
    }
}
