using System.Text.Json;
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Services.BoardGame;

public sealed class BoardGameAiService
{
    private readonly DeepSeekService _deepSeek;
    private readonly ILogger<BoardGameAiService> _logger;

    public BoardGameAiService(DeepSeekService deepSeek, ILogger<BoardGameAiService> logger)
    {
        _deepSeek = deepSeek;
        _logger = logger;
    }

    public async Task<BoardMove?> ChooseMoveAsync(IBoardGameEngine engine, string stateJson, string aiSide, string level)
    {
        var legalMoves = engine.GetLegalMoves(stateJson, aiSide);
        if (legalMoves.Count == 0) return null;
        if (level == "easy") return legalMoves[Random.Shared.Next(legalMoves.Count)];

        var moveList = string.Join("\n", legalMoves.Select((move, index) => $"{index}. from={move.From ?? "-"} to={move.To}"));
        var hint = level == "hard"
            ? "你是职业棋手，请从合法走法中选择对当前执棋方最有利的一步。"
            : "你是业余棋手，请从合法走法中选择一个合理走法。";
        var prompt = $@"{hint}
棋种：{engine.GameType}
执棋方：{aiSide}（{engine.SideName(aiSide)}）
当前局面：{stateJson}
合法走法（只能选择下列编号）：
{moveList}
只返回 JSON：{{""index"": <编号>}}，不要输出其他文字。";

        try
        {
            var raw = await _deepSeek.GetAIResponseAsync(prompt, "你是棋类走法选择器，只能从给定合法走法中选择。", 300, 0.2);
            using var document = JsonDocument.Parse(ExtractJson(raw));
            if (document.RootElement.TryGetProperty("index", out var indexElement)
                && indexElement.TryGetInt32(out var index) && index >= 0 && index < legalMoves.Count)
                return legalMoves[index];
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "棋类 AI 返回内容无法解析，将退回随机合法走法");
        }
        return legalMoves[Random.Shared.Next(legalMoves.Count)];
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : "{}";
    }
}
