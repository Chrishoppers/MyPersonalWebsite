using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyPersonalWebsite.Services.BoardGame;

public sealed class ChessEngine : IBoardGameEngine
{
    public string GameType => "chess";
    public string InitialSide => "white";
    private const int Size = 8;
    private static readonly string[] InitialRows = ["rnbqkbnr", "pppppppp", "        ", "        ", "        ", "        ", "PPPPPPPP", "RNBQKBNR"];
    private static readonly (int X, int Y)[] RookDirections = [(1, 0), (-1, 0), (0, 1), (0, -1)];
    private static readonly (int X, int Y)[] BishopDirections = [(1, 1), (1, -1), (-1, 1), (-1, -1)];
    private static readonly (int X, int Y)[] KnightDirections = [(1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2)];
    private static readonly (int X, int Y)[] KingDirections = [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)];

    public string InitialStateJson()
    {
        var cells = InitialRows.Select(row => row.Select(piece => piece == ' ' ? "" : piece.ToString()).ToArray()).ToArray();
        return JsonSerializer.Serialize(new ChessState { Cells = cells, Castling = "KQkq", Halfmove = 0, Fullmove = 1 });
    }

    public string SideName(string side) => side == "white" ? "白方" : "黑方";

    public IReadOnlyList<BoardMove> GetLegalMoves(string stateJson, string side)
    {
        var state = ParseState(stateJson);
        var legalMoves = new List<BoardMove>();
        for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                var piece = state.Cells[y][x];
                if (piece.Length == 0 || SideOf(piece) != side) continue;
                foreach (var candidate in RawMoves(state, x, y, piece))
                    if (!LeavesInCheck(state, x, y, candidate.X, candidate.Y, side, candidate.IsEnPassant))
                        legalMoves.Add(new BoardMove($"{x},{y}", $"{candidate.X},{candidate.Y}", candidate.IsEnPassant ? "enpassant" : candidate.IsCastle ? "castle" : null));
            }
        return legalMoves;
    }

    public MoveResult TryApplyMove(string stateJson, string side, BoardMove move, out string newStateJson)
    {
        newStateJson = stateJson;
        if (move.From is null || !TryParseCoord(move.From, out var fx, out var fy) || !TryParseCoord(move.To, out var tx, out var ty))
            return new MoveResult(false, "坐标非法");
        var state = ParseState(stateJson);
        var piece = state.Cells[fy][fx];
        if (piece.Length == 0 || SideOf(piece) != side) return new MoveResult(false, "不是你的棋子");
        var candidate = RawMoves(state, fx, fy, piece).FirstOrDefault(candidate => candidate.X == tx && candidate.Y == ty);
        if (candidate is null) return new MoveResult(false, "走法非法");
        if (LeavesInCheck(state, fx, fy, tx, ty, side, candidate.IsEnPassant)) return new MoveResult(false, "走完被将军");

        var isPawnMove = char.ToLowerInvariant(piece[0]) == 'p';
        var promotionMove = isPawnMove && (ty == 0 || ty == Size - 1);
        var promotion = "Q";
        if (promotionMove && move.Extra is not null)
        {
            promotion = move.Extra.Trim().ToUpperInvariant();
            if (promotion.Length != 1 || !"QRBN".Contains(promotion[0])) return new MoveResult(false, "升变棋子无效");
        }

        var cells = state.Cells;
        var captured = candidate.IsEnPassant ? cells[fy][tx] : cells[ty][tx];
        var movingSide = SideOf(piece);
        var text = new List<string> { PieceName(piece), $"{Coord(fx, fy)}→{Coord(tx, ty)}" };
        cells[fy][fx] = "";
        if (candidate.IsEnPassant) cells[fy][tx] = "";
        cells[ty][tx] = promotionMove && movingSide == "black" ? promotion.ToLowerInvariant() : promotionMove ? promotion : piece;
        if (candidate.IsCastle)
        {
            var rookFromX = tx > fx ? 7 : 0;
            var rookToX = tx > fx ? 5 : 3;
            cells[ty][rookToX] = cells[ty][rookFromX];
            cells[ty][rookFromX] = "";
            text.Add(tx > fx ? "短易位 O-O" : "长易位 O-O-O");
        }
        if (candidate.IsEnPassant) text.Add("吃过路兵");
        if (promotionMove) text.Add($"={PieceName(cells[ty][tx])}");
        if (captured.Length != 0) text.Add($"吃{PieceName(captured)}");

        UpdateCastlingRights(state, piece, fx, fy, captured, tx, ty);
        state.EnPassant = isPawnMove && Math.Abs(fy - ty) == 2 ? $"{fx},{(fy + ty) / 2}" : null;
        state.Halfmove = isPawnMove || captured.Length != 0 ? 0 : state.Halfmove + 1;
        if (side == "black") state.Fullmove++;
        newStateJson = JsonSerializer.Serialize(state);
        return new MoveResult(true, MoveText: string.Join(" ", text));
    }

    public GameStatus Evaluate(string stateJson, string sideToMove)
    {
        var state = ParseState(stateJson);
        if (GetLegalMoves(stateJson, sideToMove).Count == 0)
            return IsInCheck(state.Cells, sideToMove)
                ? new GameStatus(GameOutcome.Win, sideToMove == "white" ? "black" : "white", "将死")
                : new GameStatus(GameOutcome.Draw, null, "逼和");
        return state.Halfmove >= 100
            ? new GameStatus(GameOutcome.Draw, null, "50 回合规则")
            : new GameStatus(GameOutcome.Ongoing, null, null);
    }

    private sealed record Candidate(int X, int Y, bool IsEnPassant = false, bool IsCastle = false);

    private static List<Candidate> RawMoves(ChessState state, int x, int y, string piece)
    {
        var cells = state.Cells;
        var side = SideOf(piece);
        var result = new List<Candidate>();
        void Add(int nx, int ny)
        {
            if (nx < 0 || nx >= Size || ny < 0 || ny >= Size) return;
            var target = cells[ny][nx];
            if (target.Length == 0 || (SideOf(target) != side && char.ToLowerInvariant(target[0]) != 'k')) result.Add(new Candidate(nx, ny));
        }
        void Slide(IEnumerable<(int X, int Y)> directions)
        {
            foreach (var (dx, dy) in directions)
                for (var step = 1; step < Size; step++)
                {
                    var nx = x + dx * step; var ny = y + dy * step;
                    if (nx < 0 || nx >= Size || ny < 0 || ny >= Size) break;
                    var target = cells[ny][nx];
                    if (target.Length == 0) result.Add(new Candidate(nx, ny));
                    else
                    {
                        if (SideOf(target) != side && char.ToLowerInvariant(target[0]) != 'k') result.Add(new Candidate(nx, ny));
                        break;
                    }
                }
        }
        switch (char.ToLowerInvariant(piece[0]))
        {
            case 'p':
                var direction = piece == "P" ? -1 : 1;
                var startRank = piece == "P" ? 6 : 1;
                var nextY = y + direction;
                if (nextY >= 0 && nextY < Size && cells[nextY][x].Length == 0)
                {
                    result.Add(new Candidate(x, nextY));
                    if (y == startRank && cells[y + 2 * direction][x].Length == 0) result.Add(new Candidate(x, y + 2 * direction));
                }
                foreach (var dx in new[] { -1, 1 })
                {
                    var nx = x + dx;
                    if (nx < 0 || nx >= Size || nextY < 0 || nextY >= Size) continue;
                    var target = cells[nextY][nx];
                    if (target.Length != 0 && SideOf(target) != side && char.ToLowerInvariant(target[0]) != 'k')
                        result.Add(new Candidate(nx, nextY));
                    else if (target.Length == 0 && state.EnPassant == $"{nx},{nextY}")
                    {
                        var adjacent = cells[y][nx];
                        if (adjacent.Length != 0 && char.ToLowerInvariant(adjacent[0]) == 'p' && SideOf(adjacent) != side)
                            result.Add(new Candidate(nx, nextY, IsEnPassant: true));
                    }
                }
                break;
            case 'n': foreach (var (dx, dy) in KnightDirections) Add(x + dx, y + dy); break;
            case 'b': Slide(BishopDirections); break;
            case 'r': Slide(RookDirections); break;
            case 'q': Slide(RookDirections.Concat(BishopDirections)); break;
            case 'k':
                foreach (var (dx, dy) in KingDirections) Add(x + dx, y + dy);
                AddCastlingMoves(state, x, y, piece, result);
                break;
        }
        return result;
    }

    private static void AddCastlingMoves(ChessState state, int x, int y, string king, List<Candidate> moves)
    {
        var white = king == "K" && x == 4 && y == 7;
        var black = king == "k" && x == 4 && y == 0;
        if (!white && !black) return;
        var side = white ? "white" : "black";
        var opponent = white ? "black" : "white";
        var row = white ? 7 : 0;
        if (IsInCheck(state.Cells, side)) return;
        var shortRight = white ? 'K' : 'k';
        var longRight = white ? 'Q' : 'q';
        var rook = white ? "R" : "r";
        if (state.Castling.Contains(shortRight) && state.Cells[row][7] == rook && state.Cells[row][5] == "" && state.Cells[row][6] == ""
            && !IsSquareAttacked(state.Cells, 5, row, opponent) && !IsSquareAttacked(state.Cells, 6, row, opponent))
            moves.Add(new Candidate(6, row, IsCastle: true));
        if (state.Castling.Contains(longRight) && state.Cells[row][0] == rook && state.Cells[row][1] == "" && state.Cells[row][2] == "" && state.Cells[row][3] == ""
            && !IsSquareAttacked(state.Cells, 3, row, opponent) && !IsSquareAttacked(state.Cells, 2, row, opponent))
            moves.Add(new Candidate(2, row, IsCastle: true));
    }

    private static bool LeavesInCheck(ChessState state, int fx, int fy, int tx, int ty, string side, bool enPassant)
    {
        var cells = state.Cells;
        var moving = cells[fy][fx];
        var captured = cells[ty][tx];
        var epCaptured = enPassant ? cells[fy][tx] : "";
        var rookFromX = -1;
        var rookToX = -1;
        var rookPiece = "";
        var castleRow = ty;
        var castlingRights = state.Castling;
        var castlingRightsWereValid = fx == 4 && (ty == 0 ? castlingRights.Contains('k') || castlingRights.Contains('q') : ty == 7 && (castlingRights.Contains('K') || castlingRights.Contains('Q')));
        if (char.ToLowerInvariant(moving[0]) == 'k' && Math.Abs(fx - tx) == 2
            && castlingRightsWereValid)
        {
            rookFromX = tx > fx ? 7 : 0;
            rookToX = tx > fx ? 5 : 3;
            rookPiece = cells[castleRow][rookFromX];
            cells[castleRow][rookToX] = rookPiece;
            cells[castleRow][rookFromX] = "";
        }
        cells[ty][tx] = moving;
        cells[fy][fx] = "";
        if (enPassant) cells[fy][tx] = "";
        if (rookFromX >= 0)
        {
            cells[castleRow][rookFromX] = "";
            cells[castleRow][rookToX] = rookPiece;
        }
        var inCheck = IsInCheck(cells, side);
        cells[fy][fx] = moving;
        cells[ty][tx] = captured;
        state.Castling = castlingRights;
        if (enPassant) cells[fy][tx] = epCaptured;
        if (rookFromX >= 0)
        {
            cells[castleRow][rookFromX] = rookPiece;
            cells[castleRow][rookToX] = "";
        }
        return inCheck;
    }

    private static bool IsInCheck(string[][] cells, string side)
    {
        var king = side == "white" ? "K" : "k";
        for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
                if (cells[y][x] == king) return IsSquareAttacked(cells, x, y, side == "white" ? "black" : "white");
        return true;
    }

    private static bool IsSquareAttacked(string[][] cells, int x, int y, string attackerSide)
    {
        for (var sy = 0; sy < Size; sy++)
            for (var sx = 0; sx < Size; sx++)
            {
                var piece = cells[sy][sx];
                if (piece.Length == 0 || SideOf(piece) != attackerSide) continue;
                var dx = x - sx; var dy = y - sy;
                switch (char.ToLowerInvariant(piece[0]))
                {
                    case 'p': if (Math.Abs(dx) == 1 && dy == (piece == "P" ? -1 : 1)) return true; break;
                    case 'n': if ((Math.Abs(dx), Math.Abs(dy)) is (1, 2) or (2, 1)) return true; break;
                    case 'k': if (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1 && (dx != 0 || dy != 0)) return true; break;
                    case 'b': if (Math.Abs(dx) == Math.Abs(dy) && ClearPath(cells, sx, sy, x, y)) return true; break;
                    case 'r': if ((dx == 0 || dy == 0) && ClearPath(cells, sx, sy, x, y)) return true; break;
                    case 'q': if ((dx == 0 || dy == 0 || Math.Abs(dx) == Math.Abs(dy)) && ClearPath(cells, sx, sy, x, y)) return true; break;
                }
            }
        return false;
    }

    private static bool ClearPath(string[][] cells, int fx, int fy, int tx, int ty)
    {
        var dx = Math.Sign(tx - fx); var dy = Math.Sign(ty - fy);
        int x = fx + dx, y = fy + dy;
        for (; x != tx || y != ty; x += dx, y += dy)
            if (cells[y][x].Length != 0) return false;
        return true;
    }

    private static void UpdateCastlingRights(ChessState state, string piece, int fx, int fy, string captured, int tx, int ty)
    {
        var rights = state.Castling;
        if (piece == "K") rights = rights.Replace("K", "").Replace("Q", "");
        if (piece == "k") rights = rights.Replace("k", "").Replace("q", "");
        if (piece == "R" && fx == 0 && fy == 7 || captured == "R" && tx == 0 && ty == 7) rights = rights.Replace("Q", "");
        if (piece == "R" && fx == 7 && fy == 7 || captured == "R" && tx == 7 && ty == 7) rights = rights.Replace("K", "");
        if (piece == "r" && fx == 0 && fy == 0 || captured == "r" && tx == 0 && ty == 0) rights = rights.Replace("q", "");
        if (piece == "r" && fx == 7 && fy == 0 || captured == "r" && tx == 7 && ty == 0) rights = rights.Replace("k", "");
        state.Castling = rights;
    }

    private static string SideOf(string piece) => piece.Length == 0 ? "" : char.IsUpper(piece[0]) ? "white" : "black";
    private static bool TryParseCoord(string value, out int x, out int y)
    {
        x = y = -1;
        var parts = value.Split(',');
        return parts.Length == 2 && int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y)
            && x >= 0 && x < Size && y >= 0 && y < Size;
    }
    private static string Coord(int x, int y) => $"{(char)('a' + x)}{8 - y}";
    private static string PieceName(string piece) => piece switch
    {
        "K" or "k" => "王", "Q" or "q" => "后", "R" or "r" => "车", "B" or "b" => "象", "N" or "n" => "马", "P" or "p" => "兵", _ => piece
    };
    private static ChessState ParseState(string json)
    {
        var state = JsonSerializer.Deserialize<ChessState>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new JsonException("棋盘状态无效");
        if (state.Cells is null)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var cells = root.EnumerateObject().FirstOrDefault(property => string.Equals(property.Name, "cells", StringComparison.OrdinalIgnoreCase));
            if (cells.Value.ValueKind != JsonValueKind.Array) throw new JsonException("棋盘状态缺少 cells");
            state.Cells = JsonSerializer.Deserialize<string[][]>(cells.Value.GetRawText()) ?? throw new JsonException("棋盘状态无效");
        }
        if (state.Cells.Length != Size || state.Cells.Any(row => row is null || row.Length != Size || row.Any(piece => piece is null)))
            throw new JsonException("棋盘状态尺寸无效");
        state.Castling ??= "";
        return state;
    }

    private sealed class ChessState
    {
        public ChessState() { }
        [JsonPropertyName("cells")] public string[][] Cells { get; set; } = [];
        [JsonPropertyName("castling")] public string Castling { get; set; } = "";
        [JsonPropertyName("enPassant")] public string? EnPassant { get; set; }
        [JsonPropertyName("halfmove")] public int Halfmove { get; set; }
        [JsonPropertyName("fullmove")] public int Fullmove { get; set; } = 1;
    }
}
