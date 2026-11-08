using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Services.BoardGame;

namespace MyPersonalWebsite.Hubs;

public sealed class BoardGameHub : Hub
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> RoomLocks = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, int> Violations = new(StringComparer.Ordinal);
    private readonly BoardGameService _game;
    private readonly BoardGameAiService _ai;
    private readonly ChatModerationService _moderation;
    private readonly IConfiguration _configuration;

    public BoardGameHub(BoardGameService game, BoardGameAiService ai, ChatModerationService moderation, IConfiguration configuration)
    {
        _game = game;
        _ai = ai;
        _moderation = moderation;
        _configuration = configuration;
    }

    public async Task<object?> JoinRoom(string roomCode, string? token = null)
    {
        var room = await _game.GetRoomAsync(roomCode);
        if (room is null) throw new HubException("无权加入该棋局。");
        var side = GetSide(room, token);
        // AI 对局只有一名真人玩家（房主）；令牌/session 匹配失败时也允许以房主身份进入
        if (side is null && room.Mode == "ai")
            side = _game.GetEngine(room.GameType).InitialSide;
        if (side is null) throw new HubException("无权加入该棋局。");
        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        await Clients.Group(roomCode).SendAsync("RoomUpdated", new
        {
            status = room.Status, guestName = room.GuestName, guestAvatar = room.GuestAvatar,
            guestIsGuest = room.GuestIsGuest, currentTurn = room.CurrentTurn, chatEnabled = room.ChatEnabled
        });
        return new
        {
            room.Id, room.RoomCode, room.GameType, room.Mode, room.Status,
            room.HostName, room.HostAvatar, room.HostIsGuest, room.GuestName, room.GuestAvatar, room.GuestIsGuest,
            room.AiLevel, room.AiSide, room.CurrentTurn, room.BoardState, room.MoveCount, room.ChatEnabled,
            room.HostReady, room.GuestReady,
            mySide = side
        };
    }

    public async Task MakeMove(string roomCode, string? from, string to, string? extra = null, string? token = null)
    {
        var gate = RoomLocks.GetOrAdd(roomCode, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            var room = await _game.GetRoomAsync(roomCode);
            var side = GetSide(room, token);
            if (room is null) { await Clients.Caller.SendAsync("MoveRejected", "房间不存在"); return; }
            if (side is null) { await Clients.Caller.SendAsync("MoveRejected", "无权操作该棋局"); return; }
            if (room.Status != "playing") { await Clients.Caller.SendAsync("MoveRejected", "对局尚未开始"); return; }
            if (room.CurrentTurn != side) { await Clients.Caller.SendAsync("MoveRejected", "还没轮到您"); return; }
            var engine = _game.GetEngine(room.GameType);
            var move = new BoardMove(from, to, extra);
            var result = engine.TryApplyMove(room.BoardState, side, move, out var newState);
            if (!result.Ok) { await Clients.Caller.SendAsync("MoveRejected", result.Error); return; }
            var next = NextSide(room.GameType, side);
            var status = engine.Evaluate(newState, next);
            await _game.UpdateRoomStateAsync(room.Id, newState, room.MoveCount + 1, next);
            await _game.AppendMoveAsync(room.Id, room.MoveCount, side, move, result.MoveText ?? "", newState);
            await Clients.Group(roomCode).SendAsync("MoveMade", new
            {
                from, to, side, extra, moveText = result.MoveText, state = newState, turn = next,
                status = status.Outcome.ToString(), winner = status.WinnerSide
            });
            if (status.Outcome != GameOutcome.Ongoing)
            {
                await FinishAndNotify(room, status);
                return;
            }
            if (room.Mode == "ai" && next == room.AiSide)
                await MakeAiMoveAsync(room, engine, newState);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MakeMove 异常: {ex}");
            await Clients.Caller.SendAsync("MoveRejected", "走子处理失败：" + ex.Message);
        }
        finally { gate.Release(); }
    }

    public async Task SendChat(string roomCode, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 200) return;
        var room = await _game.GetRoomAsync(roomCode);
        var side = GetSide(room, null);
        if (room is null || side is null || room.ChatEnabled == 0 || room.Status != "playing") return;
        var (name, isGuest) = GetSender(room, side);
        var threshold = _configuration.GetValue("BoardGame:ChatHarmonyThreshold", 50);
        var maxViolations = _configuration.GetValue("BoardGame:ChatMaxViolations", 3);
        var (score, reason) = await _moderation.ScoreAsync(content.Trim());
        var flagged = score < threshold;
        await _game.SaveChatAsync(room.Id, name, isGuest, side, content.Trim(), score, flagged);
        if (!flagged)
        {
            await Clients.Group(roomCode).SendAsync("ChatMessage", new { senderName = name, isGuest, senderSide = side, content = content.Trim(), at = DateTime.Now.ToString("HH:mm:ss") });
            return;
        }
        var count = Violations.AddOrUpdate(roomCode, 1, (_, value) => value + 1);
        await Clients.Group(roomCode).SendAsync("ChatWarning", new { message = $"消息未通过审核（{score}分：{reason}），已拦截；累计 {count}/{maxViolations} 次将关闭聊天。" });
        if (count < maxViolations) return;
        await _game.SetChatEnabledAsync(room.Id, false);
        await Clients.Group(roomCode).SendAsync("ChatDisabled", new { message = "聊天已关闭（多次违规）。" });
        if (_configuration.GetValue("BoardGame:CancelOnChatAbuse", true))
        {
            var punishment = _configuration.GetValue<string>("BoardGame:AbusePunishment") ?? "draw";
            var status = punishment == "lose"
                ? new GameStatus(GameOutcome.Win, NextSide(room.GameType, side), "聊天多次违规，判负")
                : new GameStatus(GameOutcome.Draw, null, "聊天多次违规，判平局");
            await FinishAndNotify(room, status);
        }
        Violations.TryRemove(roomCode, out _);
    }

    public async Task Resign(string roomCode)
    {
        var room = await _game.GetRoomAsync(roomCode);
        var side = GetSide(room, null);
        if (room is null || side is null || room.Status != "playing") return;
        await FinishAndNotify(room, new GameStatus(GameOutcome.Win, NextSide(room.GameType, side), $"{_game.GetEngine(room.GameType).SideName(side)}认输"));
    }

    // 准备：双方都准备后开始对局
    public async Task SetReady(string roomCode, string? token = null)
    {
        var room = await _game.GetRoomAsync(roomCode);
        if (room is null) return;
        var isHost = !string.IsNullOrEmpty(token) && token == room.HostToken;
        var isGuest = !string.IsNullOrEmpty(token) && token == room.GuestToken;
        if (!isHost && !isGuest) return;

        await _game.SetPlayerReadyAsync(room.Id, isHost, true);
        var updated = await _game.GetRoomAsync(roomCode);
        if (updated is null) return;
        await Clients.Group(roomCode).SendAsync("RoomUpdated", new
        {
            status = updated.Status, guestName = updated.GuestName, guestAvatar = updated.GuestAvatar,
            guestIsGuest = updated.GuestIsGuest, currentTurn = updated.CurrentTurn, chatEnabled = updated.ChatEnabled,
            hostReady = updated.HostReady, guestReady = updated.GuestReady
        });
        if (updated.HostReady == 1 && updated.GuestReady == 1 && updated.Status == "waiting")
        {
            await _game.StartRoomAsync(room.Id);
            await Clients.Group(roomCode).SendAsync("GameStarted", new { });
        }
    }

    // 房主踢出玩家
    public async Task KickPlayer(string roomCode, string? token = null)
    {
        var room = await _game.GetRoomAsync(roomCode);
        if (room is null) return;
        var isHost = !string.IsNullOrEmpty(token) && token == room.HostToken;
        if (!isHost) return;
        await _game.KickGuestAsync(roomCode);
        await Clients.Group(roomCode).SendAsync("GuestKicked", new { });
    }

    private async Task MakeAiMoveAsync(RoomDto room, IBoardGameEngine engine, string state)
    {
        await Clients.Group(room.RoomCode).SendAsync("AiThinking", true);
        try
        {
            await Task.Delay(400);
            var move = await _ai.ChooseMoveAsync(engine, state, room.AiSide!, room.AiLevel ?? "medium");
            if (move is null) return;
            var result = engine.TryApplyMove(state, room.AiSide!, move, out var newState);
            if (!result.Ok) return;
            var next = NextSide(room.GameType, room.AiSide!);
            var status = engine.Evaluate(newState, next);
            await _game.UpdateRoomStateAsync(room.Id, newState, room.MoveCount + 2, next);
            await _game.AppendMoveAsync(room.Id, room.MoveCount + 1, room.AiSide!, move, result.MoveText ?? "", newState);
            await Clients.Group(room.RoomCode).SendAsync("MoveMade", new
            {
                from = move.From, to = move.To, side = room.AiSide, moveText = result.MoveText,
                state = newState, turn = next, status = status.Outcome.ToString(), winner = status.WinnerSide
            });
            if (status.Outcome != GameOutcome.Ongoing) await FinishAndNotify(room, status);
        }
        finally { await Clients.Group(room.RoomCode).SendAsync("AiThinking", false); }
    }

    private async Task FinishAndNotify(RoomDto room, GameStatus status)
    {
        await _game.FinishAsync(room.Id, status);
        await Clients.Group(room.RoomCode).SendAsync("GameOver", new
        {
            outcome = status.Outcome.ToString(), winner = status.WinnerSide, reason = status.Reason
        });
    }

    private string? GetSide(RoomDto? room, string? token)
    {
        if (room is null) return null;
        // 令牌鉴权（优先，不依赖 session）
        if (!string.IsNullOrEmpty(token) && token == room.HostToken)
            return _game.GetEngine(room.GameType).InitialSide;
        if (!string.IsNullOrEmpty(token) && token == room.GuestToken)
            return NextSide(room.GameType, _game.GetEngine(room.GameType).InitialSide);
        // session 鉴权（兜底）
        var session = Context.GetHttpContext()?.Session;
        var userId = session?.GetInt32("UserId");
        var guestKey = session?.GetString("GuestKey");
        if (room.HostUserId.HasValue && userId == room.HostUserId) return _game.GetEngine(room.GameType).InitialSide;
        if (room.HostIsGuest == 1 && !string.IsNullOrEmpty(guestKey) && room.HostGuestKey == guestKey)
            return _game.GetEngine(room.GameType).InitialSide;
        if (room.GuestUserId.HasValue && userId == room.GuestUserId) return NextSide(room.GameType, _game.GetEngine(room.GameType).InitialSide);
        if (room.GuestIsGuest == 1 && !string.IsNullOrEmpty(guestKey) && room.GuestGuestKey == guestKey)
            return NextSide(room.GameType, _game.GetEngine(room.GameType).InitialSide);
        return null;
    }

    private (string Name, bool IsGuest) GetSender(RoomDto room, string side)
    {
        var hostSide = _game.GetEngine(room.GameType).InitialSide;
        return side == hostSide
            ? (room.HostName ?? "玩家", room.HostIsGuest == 1)
            : (room.GuestName ?? "玩家", room.GuestIsGuest == 1);
    }

    private static string NextSide(string gameType, string side) => gameType switch
    {
        "gomoku" => side == "black" ? "white" : "black",
        "xiangqi" => side == "red" ? "black" : "red",
        "chess" => side == "white" ? "black" : "white",
        _ => side
    };
}
