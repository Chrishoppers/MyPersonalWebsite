using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyPersonalWebsite.Services.BoardGame;

public sealed class GomokuEngine : IBoardGameEngine
{
    public string GameType => "gomoku";
    public string InitialSide => "black";
    private const int Size = 15;

    public string InitialStateJson() => JsonSerializer.Serialize(new
    {
        cells = Enumerable.Range(0, Size).Select(_ => Enumerable.Repeat("", Size).ToArray()).ToArray()
    });

    public string SideName(string side) => side == "black" ? "黑方" : "白方";

    public IReadOnlyList<BoardMove> GetLegalMoves(string stateJson, string side)
    {
        var cells = Parse(stateJson);
        var moves = new List<BoardMove>();
        for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
                if (cells[y][x].Length == 0) moves.Add(new BoardMove(null, $"{x},{y}"));
        return moves;
    }

    public MoveResult TryApplyMove(string stateJson, string side, BoardMove move, out string newStateJson)
    {
        newStateJson = stateJson;
        if (side is not ("black" or "white")) return new MoveResult(false, "执棋方无效");
        if (move.From is not null) return new MoveResult(false, "五子棋不支持起点");
        if (!TryParseCoord(move.To, out var x, out var y)) return new MoveResult(false, "坐标非法");
        var cells = Parse(stateJson);
        if (cells[y][x].Length != 0) return new MoveResult(false, "该点已有棋子");
        cells[y][x] = side == "black" ? "1" : "2";
        newStateJson = JsonSerializer.Serialize(new { cells });
        return new MoveResult(true, MoveText: $"{(side == "black" ? "●" : "○")} {Coord(x, y)}");
    }

    public GameStatus Evaluate(string stateJson, string sideToMove)
    {
        var cells = Parse(stateJson);
        int[] dx = [1, 0, 1, 1], dy = [0, 1, 1, -1];
        for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                var piece = cells[y][x];
                if (piece.Length == 0) continue;
                for (var d = 0; d < dx.Length; d++)
                {
                    var count = 1;
                    for (var k = 1; k < 5; k++)
                    {
                        var nx = x + dx[d] * k;
                        var ny = y + dy[d] * k;
                        if (nx < 0 || nx >= Size || ny < 0 || ny >= Size || cells[ny][nx] != piece) break;
                        count++;
                    }
                    if (count == 5)
                        return new GameStatus(GameOutcome.Win, piece == "1" ? "black" : "white", "五连");
                }
            }
        return cells.All(row => row.All(cell => cell.Length != 0))
            ? new GameStatus(GameOutcome.Draw, null, "满盘")
            : new GameStatus(GameOutcome.Ongoing, null, null);
    }

    private static bool TryParseCoord(string value, out int x, out int y)
    {
        x = y = -1;
        var parts = value.Split(',');
        return parts.Length == 2 && int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y)
            && x >= 0 && x < Size && y >= 0 && y < Size;
    }

    private static string Coord(int x, int y) => $"{(char)('A' + x)}{y + 1}";
    private static string[][] Parse(string json) => JsonSerializer.Deserialize<StateDto>(json)?.Cells
        ?? throw new JsonException("棋盘状态无效");

    private sealed class StateDto { [JsonPropertyName("cells")] public string[][] Cells { get; set; } = []; }
}
