using System.Text.Json;
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Services.BoardGame;

public sealed class ChatModerationService
{
    private static readonly string[] BlockWords = ["傻逼", "去死", "操你", "他妈", "废物", "垃圾", "滚蛋", "白痴", "智障"];
    private readonly DeepSeekService _deepSeek;

    public ChatModerationService(DeepSeekService deepSeek) => _deepSeek = deepSeek;

    public async Task<(int Score, string Reason)> ScoreAsync(string content)
    {
        if (BlockWords.Any(word => content.Contains(word, StringComparison.OrdinalIgnoreCase))) return (0, "命中敏感词");
        var prompt = $@"请为以下对局聊天消息评估友善程度，返回 0 到 100 的整数。只返回 JSON：{{""score"": 0, ""reason"": ""简短理由""}}。
消息：{content}";
        try
        {
            var raw = await _deepSeek.GetAIResponseAsync(prompt, "你是聊天消息审核器，只输出 JSON。", 120, 0.1);
            using var document = JsonDocument.Parse(ExtractJson(raw));
            var score = Math.Clamp(document.RootElement.GetProperty("score").GetInt32(), 0, 100);
            var reason = document.RootElement.TryGetProperty("reason", out var reasonElement) ? reasonElement.GetString() ?? "" : "";
            return (score, reason);
        }
        catch
        {
            return (80, "审核服务暂不可用");
        }
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : "{}";
    }
}
