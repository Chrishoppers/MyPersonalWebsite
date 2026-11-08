namespace MyPersonalWebsite.Services.BoardGame;

public sealed record BoardMove(string? From, string To, string? Extra = null);
public sealed record MoveResult(bool Ok, string? Error = null, string? MoveText = null);
public enum GameOutcome { Ongoing, Win, Draw }
public sealed record GameStatus(GameOutcome Outcome, string? WinnerSide, string? Reason);

public sealed class RoomDto
{
    public long Id { get; set; }
    public string RoomCode { get; set; } = "";
    public string GameType { get; set; } = "";
    public string Mode { get; set; } = "";
    public string Status { get; set; } = "waiting";
    public long? HostUserId { get; set; }
    public string? HostName { get; set; }
    public string? HostAvatar { get; set; }
    public int HostIsGuest { get; set; }
    public string? HostGuestKey { get; set; }
    public long? GuestUserId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestAvatar { get; set; }
    public int GuestIsGuest { get; set; }
    public string? GuestGuestKey { get; set; }
    public string? GuestConnectionId { get; set; }
    public string? AiLevel { get; set; }
    public string? AiSide { get; set; }
    public string CurrentTurn { get; set; } = "black";
    public string BoardState { get; set; } = "";
    public int MoveCount { get; set; }
    public string? Winner { get; set; }
    public string? ResultReason { get; set; }
    public int ChatEnabled { get; set; } = 1;
    public string? CreatedAt { get; set; }
    public string? FinishedAt { get; set; }
    public string? HostToken { get; set; }
    public string? GuestToken { get; set; }
}

public sealed class MoveDto
{
    public int MoveIndex { get; set; }
    public string Side { get; set; } = "";
    public string? From { get; set; }
    public string? To { get; set; }
    public string? MoveText { get; set; }
    public string? BoardStateAfter { get; set; }
    public string? CreatedAt { get; set; }
}

public sealed class ChatDto
{
    public string SenderName { get; set; } = "";
    public bool SenderIsGuest { get; set; }
    public string SenderSide { get; set; } = "";
    public string Content { get; set; } = "";
    public int HarmonyScore { get; set; }
    public bool IsFlagged { get; set; }
    public string? CreatedAt { get; set; }
}
