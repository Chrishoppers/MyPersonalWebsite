namespace MyPersonalWebsite.Services.BoardGame;

public interface IBoardGameEngine
{
    string GameType { get; }
    string InitialStateJson();
    string InitialSide { get; }
    IReadOnlyList<BoardMove> GetLegalMoves(string stateJson, string side);
    MoveResult TryApplyMove(string stateJson, string side, BoardMove move, out string newStateJson);
    GameStatus Evaluate(string stateJson, string sideToMove);
    string SideName(string side);
}
