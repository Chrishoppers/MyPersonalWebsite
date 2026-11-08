using System.Text.Json;
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Services.BoardGame;

public sealed class BoardGameAiService
{
    private const int GomokuSize = 15;
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
        if (legalMoves.Count == 1) return legalMoves[0];

        // 困难：先尝试 DeepSeek，失败再退回本地启发式（不再纯随机）
        if (level == "hard")
        {
            var deepMove = await TryDeepSeekMoveAsync(engine, stateJson, aiSide, legalMoves);
            if (deepMove is not null) return deepMove;
        }

        // easy：启发式但带一定随机性；medium：强启发式
        return ChooseHeuristicMove(engine, stateJson, aiSide, weak: level == "easy");
    }

    private async Task<BoardMove?> TryDeepSeekMoveAsync(IBoardGameEngine engine, string stateJson, string aiSide, IReadOnlyList<BoardMove> legalMoves)
    {
        var moveList = string.Join("\n", legalMoves.Select((move, index) => $"{index}. from={move.From ?? "-"} to={move.To}"));
        var prompt = $@"你是职业棋手，请从给定合法走法中选择对当前执棋方最有利的一步。
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
            _logger.LogWarning(exception, "棋类 AI 返回内容无法解析，将退回本地启发式");
        }
        return null;
    }

    private BoardMove ChooseHeuristicMove(IBoardGameEngine engine, string stateJson, string aiSide, bool weak)
    {
        var legalMoves = engine.GetLegalMoves(stateJson, aiSide);
        var opponent = NextSide(engine.GameType, aiSide);

        // 1. 立即取胜
        foreach (var move in legalMoves)
            if (engine.TryApplyMove(stateJson, aiSide, move, out var winState).Ok
                && engine.Evaluate(winState, opponent).Outcome == GameOutcome.Win)
                return move;

        // 2. 阻止对手立即取胜（占住对手的必胜点）
        var threats = new HashSet<string>(StringComparer.Ordinal);
        foreach (var oppMove in engine.GetLegalMoves(stateJson, opponent))
            if (engine.TryApplyMove(stateJson, opponent, oppMove, out var oppState).Ok
                && engine.Evaluate(oppState, aiSide).Outcome == GameOutcome.Win)
                threats.Add(oppMove.To);

        foreach (var move in legalMoves)
            if (threats.Contains(move.To))
                return move;

        // 3. 按评分排序
        var scored = legalMoves
            .Select(move => (Move: move, Score: ScoreMove(engine, stateJson, aiSide, move)))
            .OrderByDescending(x => x.Score)
            .ToList();

        if (scored.Count == 0) return legalMoves[Random.Shared.Next(legalMoves.Count)];

        // easy：在前几名里随机（带一点不确定性，但不至于乱下）
        if (weak && scored.Count > 1)
        {
            var top = scored.Take(Math.Max(2, scored.Count / 4)).ToList();
            return top[Random.Shared.Next(top.Count)].Move;
        }

        return scored[0].Move;
    }

    private int ScoreMove(IBoardGameEngine engine, string stateJson, string aiSide, BoardMove move)
    {
        if (engine.GameType == "gomoku")
            return ScoreGomoku(stateJson, aiSide, move.To);

        // 中国象棋 / 国际象棋：中心偏好 + 吃子偏好
        var score = CenterBonus(move.To);
        if (engine.TryApplyMove(stateJson, aiSide, move, out var next).Ok)
        {
            var captured = CountPieces(stateJson) - CountPieces(next);
            if (captured > 0) score += captured * 1000;
        }
        return score;
    }

    // 五子棋：评估在 (x,y) 落子的价值
    private int ScoreGomoku(string stateJson, string aiSide, string to)
    {
        if (!TryParseCoord(to, out var x, out var y)) return 0;
        var cells = ParseGomokuCells(stateJson);
        var self = aiSide == "black" ? "1" : "2";
        var opp = aiSide == "black" ? "2" : "1";

        int[] dx = [1, 0, 1, 1];
        int[] dy = [0, 1, 1, -1];
        var score = CenterBonus(to) * 2;
        for (var d = 0; d < 4; d++)
        {
            score += LineScore(cells, x, y, dx[d], dy[d], self) * 2;
            score += LineScore(cells, x, y, dx[d], dy[d], opp);
        }
        return score;
    }

    private static int LineScore(string[][] cells, int x, int y, int dx, int dy, string piece)
    {
        var count = 1;
        for (var k = 1; k < 5; k++)
        {
            var nx = x + dx * k;
            var ny = y + dy * k;
            if (nx < 0 || nx >= GomokuSize || ny < 0 || ny >= GomokuSize || cells[ny][nx] != piece) break;
            count++;
        }
        for (var k = 1; k < 5; k++)
        {
            var nx = x - dx * k;
            var ny = y - dy * k;
            if (nx < 0 || nx >= GomokuSize || ny < 0 || ny >= GomokuSize || cells[ny][nx] != piece) break;
            count++;
        }
        return count >= 5 ? 1000000 : count == 4 ? 100000 : count == 3 ? 10000 : count == 2 ? 1000 : count == 1 ? 100 : 0;
    }

    private static int CenterBonus(string to)
    {
        if (!TryParseCoord(to, out var x, out var y)) return 0;
        var center = 7;
        return 14 - Math.Abs(x - center) - Math.Abs(y - center);
    }

    private static int CountPieces(string stateJson)
    {
        var count = 0;
        try
        {
            using var doc = JsonDocument.Parse(stateJson);
            foreach (var row in doc.RootElement.GetProperty("cells").EnumerateArray())
                foreach (var cell in row.EnumerateArray())
                    if (cell.ValueKind == JsonValueKind.String && cell.GetString()?.Length > 0)
                        count++;
        }
        catch { }
        return count;
    }

    private static string[][] ParseGomokuCells(string stateJson)
    {
        using var doc = JsonDocument.Parse(stateJson);
        return doc.RootElement.GetProperty("cells").EnumerateArray()
            .Select(row => row.EnumerateArray().Select(cell => cell.GetString() ?? "").ToArray())
            .ToArray();
    }

    private static bool TryParseCoord(string value, out int x, out int y)
    {
        x = y = -1;
        var parts = value?.Split(',') ?? [];
        return parts.Length == 2 && int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y);
    }

    private static string NextSide(string gameType, string side) => gameType switch
    {
        "gomoku" => side == "black" ? "white" : "black",
        "xiangqi" => side == "red" ? "black" : "red",
        "chess" => side == "white" ? "black" : "white",
        _ => side
    };

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : "{}";
    }
}
