using System.Text.Json;
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Services.BoardGame;

public sealed class BoardGameService
{
    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private readonly DataSyncService _db;
    private readonly IServiceProvider _services;

    public BoardGameService(DataSyncService db, IServiceProvider services)
    {
        _db = db;
        _services = services;
    }

    public IBoardGameEngine GetEngine(string gameType) => gameType switch
    {
        "gomoku" => _services.GetRequiredService<GomokuEngine>(),
        "xiangqi" => _services.GetRequiredService<XiangqiEngine>(),
        "chess" => _services.GetRequiredService<ChessEngine>(),
        _ => throw new ArgumentException("未知棋类", nameof(gameType))
    };

    public async Task<(long Id, string Code)> CreateRoomAsync(string gameType, string mode, string? aiLevel, string? aiSide,
        long? hostUserId, string hostName, string hostAvatar, bool hostIsGuest, string? hostGuestKey = null)
    {
        var engine = GetEngine(gameType);
        var code = NewRoomCode();
        var now = DateTime.UtcNow.ToString("O");
        var status = mode == "ai" ? "playing" : "waiting";
        var sql = $@"INSERT INTO BoardGameRooms
            (RoomCode,GameType,Mode,Status,HostUserId,HostName,HostAvatar,HostIsGuest,HostGuestKey,
             AiLevel,AiSide,CurrentTurn,BoardState,MoveCount,ChatEnabled,CreatedAt,StartedAt)
            VALUES ('{E(code)}','{E(gameType)}','{E(mode)}','{status}',{NullableId(hostUserId)},
             '{E(hostName)}','{E(hostAvatar)}',{(hostIsGuest ? 1 : 0)},'{E(hostGuestKey)}',
             '{E(aiLevel)}','{E(aiSide)}','{engine.InitialSide}','{E(engine.InitialStateJson())}',0,1,
             '{now}','{(mode == "ai" ? now : "")}')";
        if (!await _db.ExecuteSqlAsync(sql)) throw new InvalidOperationException("创建棋局房间失败，请检查数据库连接。");
        return (await GetIdByCodeAsync(code), code);
    }

    public async Task<long> GetIdByCodeAsync(string code)
    {
        var rows = await QueryRowsAsync($"SELECT Id FROM BoardGameRooms WHERE RoomCode='{E(code)}' LIMIT 1");
        return rows.Count == 0 ? 0 : Long(rows[0][0]);
    }

    public async Task<string> GetCodeByIdAsync(long id)
    {
        var rows = await QueryRowsAsync($"SELECT RoomCode FROM BoardGameRooms WHERE Id={id} LIMIT 1");
        return rows.Count == 0 ? "" : Text(rows[0][0]);
    }

    public async Task<RoomDto?> GetRoomAsync(string code)
    {
        var rows = await QueryRowsAsync($@"SELECT Id,RoomCode,GameType,Mode,Status,HostUserId,HostName,HostAvatar,HostIsGuest,HostGuestKey,
            GuestUserId,GuestName,GuestAvatar,GuestIsGuest,GuestGuestKey,GuestConnectionId,AiLevel,AiSide,CurrentTurn,BoardState,
            MoveCount,Winner,ResultReason,ChatEnabled,CreatedAt,FinishedAt FROM BoardGameRooms WHERE RoomCode='{E(code)}' LIMIT 1");
        return rows.Count == 0 ? null : MapRoom(rows[0]);
    }

    public async Task<RoomDto?> GetRoomByIdAsync(long id)
    {
        var rows = await QueryRowsAsync($@"SELECT Id,RoomCode,GameType,Mode,Status,HostUserId,HostName,HostAvatar,HostIsGuest,HostGuestKey,
            GuestUserId,GuestName,GuestAvatar,GuestIsGuest,GuestGuestKey,GuestConnectionId,AiLevel,AiSide,CurrentTurn,BoardState,
            MoveCount,Winner,ResultReason,ChatEnabled,CreatedAt,FinishedAt FROM BoardGameRooms WHERE Id={id} LIMIT 1");
        return rows.Count == 0 ? null : MapRoom(rows[0]);
    }

    public async Task<RoomDto?> FindWaitingRoomAsync(string gameType)
    {
        var rows = await QueryRowsAsync($"SELECT RoomCode FROM BoardGameRooms WHERE GameType='{E(gameType)}' AND Mode='pvp' AND Status='waiting' AND GuestName IS NULL ORDER BY Id LIMIT 1");
        return rows.Count == 0 ? null : await GetRoomAsync(Text(rows[0][0]));
    }

    public async Task<RoomDto?> FindWaitingRoomForUserAsync(string gameType, long? userId, string guestKey)
    {
        var hostIdentity = userId.HasValue ? $"HostUserId={userId.Value}" : $"HostGuestKey='{E(guestKey)}'";
        var rows = await QueryRowsAsync($"SELECT RoomCode FROM BoardGameRooms WHERE GameType='{E(gameType)}' AND Mode='pvp' AND Status='waiting' AND GuestName IS NULL AND {hostIdentity} ORDER BY Id DESC LIMIT 1");
        return rows.Count == 0 ? null : await GetRoomAsync(Text(rows[0][0]));
    }

    public async Task<bool> JoinRoomAsync(string code, long? guestUserId, string guestName, string guestAvatar,
        bool guestIsGuest, string? guestKey, string connectionId = "")
    {
        var sql = $@"UPDATE BoardGameRooms SET Status='playing',GuestUserId={NullableId(guestUserId)},
            GuestName='{E(guestName)}',GuestAvatar='{E(guestAvatar)}',GuestIsGuest={(guestIsGuest ? 1 : 0)},
            GuestGuestKey='{E(guestKey)}',GuestConnectionId='{E(connectionId)}',StartedAt='{DateTime.UtcNow:O}'
            WHERE RoomCode='{E(code)}' AND Status='waiting' AND GuestName IS NULL AND (HostUserId IS NULL OR HostUserId != {NullableId(guestUserId)})
            AND (HostGuestKey IS NULL OR HostGuestKey = '' OR HostGuestKey != '{E(guestKey)}')";
        return await _db.ExecuteSqlAsync(sql);
    }

    public async Task<bool> SetGuestConnectionAsync(string code, string guestKey, string connectionId)
    {
        return await _db.ExecuteSqlAsync($"UPDATE BoardGameRooms SET GuestConnectionId='{E(connectionId)}' WHERE RoomCode='{E(code)}' AND GuestGuestKey='{E(guestKey)}' AND Status='playing'");
    }

    public Task UpdateRoomStateAsync(long roomId, string state, int moveCount, string turn) =>
        _db.ExecuteSqlAsync($"UPDATE BoardGameRooms SET BoardState='{E(state)}',MoveCount={moveCount},CurrentTurn='{E(turn)}' WHERE Id={roomId} AND Status='playing'");

    public Task AppendMoveAsync(long roomId, int index, string side, BoardMove move, string moveText, string stateAfter) =>
        _db.ExecuteSqlAsync($@"INSERT INTO BoardGameMoves (RoomId,MoveIndex,Side,FromPos,ToPos,MoveText,BoardStateAfter,CreatedAt)
            VALUES ({roomId},{index},'{E(side)}','{E(move.From)}','{E(move.To)}','{E(moveText)}','{E(stateAfter)}','{DateTime.UtcNow:O}')");

    public Task FinishAsync(long roomId, GameStatus status) =>
        _db.ExecuteSqlAsync($"UPDATE BoardGameRooms SET Status='finished',Winner='{E(status.WinnerSide)}',ResultReason='{E(status.Reason)}',FinishedAt='{DateTime.UtcNow:O}' WHERE Id={roomId} AND Status='playing'");

    public Task SetChatEnabledAsync(long roomId, bool enabled) =>
        _db.ExecuteSqlAsync($"UPDATE BoardGameRooms SET ChatEnabled={(enabled ? 1 : 0)} WHERE Id={roomId}");

    public Task SaveChatAsync(long roomId, string name, bool isGuest, string side, string content, int score, bool flagged) =>
        _db.ExecuteSqlAsync($@"INSERT INTO BoardGameChats (RoomId,SenderName,SenderIsGuest,SenderSide,Content,HarmonyScore,IsFlagged,CreatedAt)
            VALUES ({roomId},'{E(name)}',{(isGuest ? 1 : 0)},'{E(side)}','{E(content)}',{score},{(flagged ? 1 : 0)},'{DateTime.UtcNow:O}')");

    public async Task<List<RoomDto>> GetReplaysAsync(string gameType)
    {
        var rows = await QueryRowsAsync($@"SELECT Id,RoomCode,GameType,Mode,Status,HostUserId,HostName,HostAvatar,HostIsGuest,HostGuestKey,
            GuestUserId,GuestName,GuestAvatar,GuestIsGuest,GuestGuestKey,GuestConnectionId,AiLevel,AiSide,CurrentTurn,BoardState,
            MoveCount,Winner,ResultReason,ChatEnabled,CreatedAt,FinishedAt FROM BoardGameRooms
            WHERE GameType='{E(gameType)}' AND Mode='pvp' AND Status='finished' AND HostIsGuest=0 AND GuestIsGuest=0 ORDER BY FinishedAt DESC LIMIT 50");
        return rows.Select(MapRoom).ToList();
    }

    public async Task<List<MoveDto>> GetMovesAsync(long roomId)
    {
        var rows = await QueryRowsAsync($"SELECT MoveIndex,Side,FromPos,ToPos,MoveText,BoardStateAfter,CreatedAt FROM BoardGameMoves WHERE RoomId={roomId} ORDER BY MoveIndex");
        return rows.Select(row => new MoveDto
        {
            MoveIndex = Int(row[0]), Side = Text(row[1]), From = NullableText(row[2]), To = NullableText(row[3]),
            MoveText = NullableText(row[4]), BoardStateAfter = NullableText(row[5]), CreatedAt = NullableText(row[6])
        }).ToList();
    }

    public Task<bool> SavePrivacyAgreementAsync(long? userId, string guestKey, string version) =>
        _db.ExecuteSqlAsync($"INSERT INTO BoardGamePrivacyAgreements (UserId,GuestKey,Version,AgreedAt) VALUES ({NullableId(userId)},'{E(guestKey)}','{E(version)}','{DateTime.UtcNow:O}')");

    public async Task<bool> HasPrivacyAgreementAsync(long? userId, string guestKey, string version)
    {
        if (userId is null && string.IsNullOrWhiteSpace(guestKey)) return false;
        var where = userId.HasValue ? $"UserId={userId.Value}" : $"GuestKey='{E(guestKey)}'";
        var rows = await QueryRowsAsync($"SELECT COUNT(*) FROM BoardGamePrivacyAgreements WHERE {where} AND Version='{E(version)}'");
        return rows.Count > 0 && Int(rows[0][0]) > 0;
    }

    private async Task<List<JsonElement>> QueryRowsAsync(string sql)
    {
        using var document = JsonDocument.Parse(await _db.QueryAsync(sql));
        if (!document.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0) return [];
        var result = results[0];
        if (!result.TryGetProperty("response", out var response) || !response.TryGetProperty("result", out var data)
            || !data.TryGetProperty("rows", out var rows)) return [];
        return rows.EnumerateArray().Select(row => row.Clone()).ToList();
    }

    private static RoomDto MapRoom(JsonElement row) => new()
    {
        Id = Long(row[0]), RoomCode = Text(row[1]), GameType = Text(row[2]), Mode = Text(row[3]), Status = Text(row[4]),
        HostUserId = NullableLong(row[5]), HostName = NullableText(row[6]), HostAvatar = NullableText(row[7]), HostIsGuest = Int(row[8]),
        HostGuestKey = NullableText(row[9]), GuestUserId = NullableLong(row[10]), GuestName = NullableText(row[11]),
        GuestAvatar = NullableText(row[12]), GuestIsGuest = Int(row[13]), GuestGuestKey = NullableText(row[14]),
        GuestConnectionId = NullableText(row[15]), AiLevel = NullableText(row[16]), AiSide = NullableText(row[17]),
        CurrentTurn = Text(row[18]), BoardState = Text(row[19]), MoveCount = Int(row[20]), Winner = NullableText(row[21]),
        ResultReason = NullableText(row[22]), ChatEnabled = Int(row[23]), CreatedAt = NullableText(row[24]), FinishedAt = NullableText(row[25])
    };

    private static string NewRoomCode() => new(Enumerable.Range(0, 6).Select(_ => CodeChars[Random.Shared.Next(CodeChars.Length)]).ToArray());
    public static string E(string? value) => (value ?? "").Replace("'", "''", StringComparison.Ordinal);
    private static string NullableId(long? value) => value?.ToString() ?? "NULL";
    private static string Text(JsonElement value) => value.ValueKind == JsonValueKind.Null ? "" : value.ToString();
    private static string? NullableText(JsonElement value) => value.ValueKind == JsonValueKind.Null ? null : value.ToString();
    private static int Int(JsonElement value) => value.ValueKind == JsonValueKind.Number ? value.GetInt32() : int.TryParse(value.ToString(), out var parsed) ? parsed : 0;
    private static long Long(JsonElement value) => value.ValueKind == JsonValueKind.Number ? value.GetInt64() : long.TryParse(value.ToString(), out var parsed) ? parsed : 0;
    private static long? NullableLong(JsonElement value) => value.ValueKind == JsonValueKind.Null ? null : Long(value);
}
