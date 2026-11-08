using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyPersonalWebsite.Services.BoardGame;

public sealed class XiangqiEngine : IBoardGameEngine
{
    public string GameType => "xiangqi";
    public string InitialSide => "red";
    private const int Width = 9, Height = 10;
    private static readonly string[] InitialRows =
    [
        "rnbakabnr", "         ", " c     c ", "p p p p p", "         ",
        "         ", "P P P P P", " C     C ", "         ", "RNBAKABNR"
    ];
    private static readonly (int X, int Y)[] RookDirections = [(1, 0), (-1, 0), (0, 1), (0, -1)];
    private static readonly (int X, int Y)[] HorseDirections = [(1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2)];
    private static readonly (int X, int Y)[] ElephantDirections = [(2, 2), (2, -2), (-2, 2), (-2, -2)];
    private static readonly (int X, int Y)[] AdvisorDirections = [(1, 1), (1, -1), (-1, 1), (-1, -1)];

    public string InitialStateJson()
    {
        var cells = InitialRows.Select(row => row.Select(piece => piece == ' ' ? "" : piece.ToString()).ToArray()).ToArray();
        return JsonSerializer.Serialize(new { cells });
    }

    public string SideName(string side) => side == "red" ? "红方" : "黑方";

    public IReadOnlyList<BoardMove> GetLegalMoves(string stateJson, string side)
    {
        var cells = Parse(stateJson);
        var result = new List<BoardMove>();
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                var piece = cells[y][x];
                if (piece.Length == 0 || SideOf(piece) != side) continue;
                foreach (var (tx, ty) in RawMoves(cells, x, y, piece))
                    if (!LeavesInCheck(cells, x, y, tx, ty, side)) result.Add(new BoardMove($"{x},{y}", $"{tx},{ty}"));
            }
        return result;
    }

    public MoveResult TryApplyMove(string stateJson, string side, BoardMove move, out string newStateJson)
    {
        newStateJson = stateJson;
        if (move.From is null || !TryParseCoord(move.From, out var fx, out var fy) || !TryParseCoord(move.To, out var tx, out var ty))
            return new MoveResult(false, "坐标非法");
        var cells = Parse(stateJson);
        var piece = cells[fy][fx];
        if (piece.Length == 0 || SideOf(piece) != side) return new MoveResult(false, "不是你的棋子");
        if (!RawMoves(cells, fx, fy, piece).Contains((tx, ty))) return new MoveResult(false, "走法非法");
        if (LeavesInCheck(cells, fx, fy, tx, ty, side)) return new MoveResult(false, "走完被将军");
        var captured = cells[ty][tx];
        cells[ty][tx] = piece;
        cells[fy][fx] = "";
        newStateJson = JsonSerializer.Serialize(new { cells });
        return new MoveResult(true, MoveText: $"{PieceName(piece)} {Coord(fx, fy)}→{Coord(tx, ty)}{(captured.Length == 0 ? "" : $" 吃{PieceName(captured)}")}");
    }

    public GameStatus Evaluate(string stateJson, string sideToMove)
    {
        var cells = Parse(stateJson);
        if (GetLegalMoves(stateJson, sideToMove).Count != 0) return new GameStatus(GameOutcome.Ongoing, null, null);
        return new GameStatus(GameOutcome.Win, sideToMove == "red" ? "black" : "red", IsInCheck(cells, sideToMove) ? "将死" : "困毙");
    }

    private static List<(int X, int Y)> RawMoves(string[][] cells, int x, int y, string piece)
    {
        var result = new List<(int, int)>();
        var red = char.IsUpper(piece[0]);
        var side = red ? "red" : "black";
        var kind = char.ToLowerInvariant(piece[0]);
        void Add(int nx, int ny)
        {
            if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) return;
            if (cells[ny][nx].Length == 0 || SideOf(cells[ny][nx]) != side) result.Add((nx, ny));
        }
        switch (kind)
        {
            case 'r':
                foreach (var (dx, dy) in RookDirections)
                    for (var k = 1; k < 10; k++)
                    {
                        var nx = x + dx * k; var ny = y + dy * k;
                        if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) break;
                        if (cells[ny][nx].Length == 0) result.Add((nx, ny));
                        else { if (SideOf(cells[ny][nx]) != side) result.Add((nx, ny)); break; }
                    }
                break;
            case 'c':
                foreach (var (dx, dy) in RookDirections)
                {
                    var jumped = false;
                    for (var k = 1; k < 10; k++)
                    {
                        var nx = x + dx * k; var ny = y + dy * k;
                        if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) break;
                        if (!jumped)
                        {
                            if (cells[ny][nx].Length == 0) result.Add((nx, ny));
                            else jumped = true;
                        }
                        else if (cells[ny][nx].Length != 0)
                        {
                            if (SideOf(cells[ny][nx]) != side) result.Add((nx, ny));
                            break;
                        }
                    }
                }
                break;
            case 'n':
                foreach (var (dx, dy) in HorseDirections)
                {
                    var legX = x + (Math.Abs(dx) == 2 ? dx / 2 : 0);
                    var legY = y + (Math.Abs(dy) == 2 ? dy / 2 : 0);
                    if (legX < 0 || legX >= Width || legY < 0 || legY >= Height) continue; // 边界保护，避免马在边线越界
                    if (cells[legY][legX].Length == 0) Add(x + dx, y + dy);
                }
                break;
            case 'b':
                foreach (var (dx, dy) in ElephantDirections)
                {
                    var nx = x + dx; var ny = y + dy;
                    if ((red && ny < 5) || (!red && ny > 4) || cells[y + dy / 2][x + dx / 2].Length != 0) continue;
                    Add(nx, ny);
                }
                break;
            case 'a':
                foreach (var (dx, dy) in AdvisorDirections)
                {
                    var nx = x + dx; var ny = y + dy;
                    if (nx is < 3 or > 5 || (red ? ny is < 7 or > 9 : ny is < 0 or > 2)) continue;
                    Add(nx, ny);
                }
                break;
            case 'k':
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    var nx = x + dx; var ny = y + dy;
                    if (nx is < 3 or > 5 || (red ? ny is < 7 or > 9 : ny is < 0 or > 2)) continue;
                    Add(nx, ny);
                }
                break;
            case 'p':
                var forward = red ? -1 : 1;
                Add(x, y + forward);
                if (red ? y <= 4 : y >= 5) { Add(x - 1, y); Add(x + 1, y); }
                break;
        }
        return result;
    }

    private static bool LeavesInCheck(string[][] cells, int fx, int fy, int tx, int ty, string side)
    {
        var captured = cells[ty][tx]; var moving = cells[fy][fx];
        cells[ty][tx] = moving; cells[fy][fx] = "";
        var inCheck = IsInCheck(cells, side) || KingsFacing(cells);
        cells[fy][fx] = moving; cells[ty][tx] = captured;
        return inCheck;
    }

    private static bool IsInCheck(string[][] cells, string side)
    {
        var king = side == "red" ? "K" : "k";
        var kingX = -1; var kingY = -1;
        for (var y = 0; y < Height; y++) for (var x = 0; x < Width; x++) if (cells[y][x] == king) { kingX = x; kingY = y; }
        if (kingX < 0) return true;
        var opponent = side == "red" ? "black" : "red";
        for (var y = 0; y < Height; y++) for (var x = 0; x < Width; x++)
        {
            var piece = cells[y][x];
            if (piece.Length != 0 && SideOf(piece) == opponent && RawMoves(cells, x, y, piece).Contains((kingX, kingY))) return true;
        }
        return false;
    }

    private static bool KingsFacing(string[][] cells)
    {
        var redX = -1; var blackX = -2; var redY = -1; var blackY = -2;
        for (var y = 0; y < Height; y++) for (var x = 0; x < Width; x++)
        {
            if (cells[y][x] == "K") { redX = x; redY = y; }
            if (cells[y][x] == "k") { blackX = x; blackY = y; }
        }
        if (redX < 0 || redX != blackX) return false;
        for (var y = Math.Min(redY, blackY) + 1; y < Math.Max(redY, blackY); y++) if (cells[y][redX].Length != 0) return false;
        return true;
    }

    private static string SideOf(string piece) => piece.Length == 0 ? "" : char.IsUpper(piece[0]) ? "red" : "black";
    private static bool TryParseCoord(string value, out int x, out int y)
    {
        x = y = -1; var parts = value.Split(',');
        return parts.Length == 2 && int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y) && x >= 0 && x < Width && y >= 0 && y < Height;
    }
    private static string Coord(int x, int y) => $"{(char)('a' + x)}{10 - y}";
    private static string PieceName(string piece) => piece switch
    {
        "K" => "帅", "A" => "仕", "B" => "相", "N" => "馬", "R" => "車", "C" => "炮", "P" => "兵",
        "k" => "将", "a" => "士", "b" => "象", "n" => "馬", "r" => "車", "c" => "砲", "p" => "卒", _ => piece
    };
    private static string[][] Parse(string json) => JsonSerializer.Deserialize<StateDto>(json)?.Cells ?? throw new JsonException("棋盘状态无效");
    private sealed class StateDto { [JsonPropertyName("cells")] public string[][] Cells { get; set; } = []; }
}
