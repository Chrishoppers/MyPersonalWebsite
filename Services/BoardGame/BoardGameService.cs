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
        long? hostUserId, string hostName, string hostAvatar, bool hostIsGuest, string? hostGuestKey = null, string? hostToken = null)
    {
        var engine = GetEngine(gameType);
        var code = NewRoomCode();
        var now = DateTime.UtcNow.ToString("O");
        var status = mode == "ai" ? "playing" : "waiting";
        var sql = $@"INSERT INTO BoardGameRooms
            (RoomCode,GameType,Mode,Status,HostUserId,HostName,HostAvatar,HostIsGuest,HostGuestKey,
             AiLevel,AiSide,CurrentTurn,BoardState,MoveCount,ChatEnabled,CreatedAt,StartedAt,HostToken)
            VALUES ('{E(code)}','{E(gameType)}','{E(mode)}','{status}',{NullableId(hostUserId)},
             '{E(hostName)}','{E(hostAvatar)}',{(hostIsGuest ? 1 : 0)},'{E(hostGuestKey)}',
             '{E(aiLevel)}','{E(aiSide)}','{engine.InitialSide}','{E(engine.InitialStateJson())}',0,1,
             '{now}','{(mode == "ai" ? now : "")}','{E(hostToken)}')";
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
            MoveCount,Winner,ResultReason,ChatEnabled,CreatedAt,FinishedAt,HostToken,GuestToken FROM BoardGameRooms WHERE RoomCode='{E(code)}' LIMIT 1");
        return rows.Count == 0 ? null : MapRoom(rows[0]);
    }

    public async Task<RoomDto?> GetRoomByIdAsync(long id)
    {
        var rows = await QueryRowsAsync($@"SELECT Id,RoomCode,GameType,Mode,Status,HostUserId,HostName,HostAvatar,HostIsGuest,HostGuestKey,
            GuestUserId,GuestName,GuestAvatar,GuestIsGuest,GuestGuestKey,GuestConnectionId,AiLevel,AiSide,CurrentTurn,BoardState,
            MoveCount,Winner,ResultReason,ChatEnabled,CreatedAt,FinishedAt,HostToken,GuestToken FROM BoardGameRooms WHERE Id={id} LIMIT 1");
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
        bool guestIsGuest, string? guestKey, string connectionId = "", string? guestToken = null)
    {
        // 防自加入：只有加入者是登录用户时才按 UserId 判断；游客加入时跳过，
        // 否则 "HostUserId != NULL" 会得到 NULL（假），导致 UPDATE 匹配 0 行。
        var userGuard = guestUserId.HasValue
            ? $"AND (HostUserId IS NULL OR HostUserId != {guestUserId.Value})"
            : "AND 1=1";
        var guestGuard = !string.IsNullOrEmpty(guestKey)
            ? $"AND (HostGuestKey IS NULL OR HostGuestKey = '' OR HostGuestKey != '{E(guestKey)}')"
            : "AND 1=1";

        var sql = $@"UPDATE BoardGameRooms SET Status='playing',GuestUserId={NullableId(guestUserId)},
            GuestName='{E(guestName)}',GuestAvatar='{E(guestAvatar)}',GuestIsGuest={(guestIsGuest ? 1 : 0)},
            GuestGuestKey='{E(guestKey)}',GuestConnectionId='{E(connectionId)}',GuestToken='{E(guestToken)}',StartedAt='{DateTime.UtcNow:O}'
            WHERE RoomCode='{E(code)}' AND Status='waiting' AND GuestName IS NULL {userGuard} {guestGuard}";
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
            MoveCount,Winner,ResultReason,ChatEnabled,CreatedAt,FinishedAt,HostToken,GuestToken FROM BoardGameRooms
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
        ResultReason = NullableText(row[22]), ChatEnabled = Int(row[23]), CreatedAt = NullableText(row[24]), FinishedAt = NullableText(row[25]),
        HostToken = NullableText(row[26]), GuestToken = NullableText(row[27])
    };

    private static string NewRoomCode() => new(Enumerable.Range(0, 6).Select(_ => CodeChars[Random.Shared.Next(CodeChars.Length)]).ToArray());
    public static string E(string? value) => (value ?? "").Replace("'", "''", StringComparison.Ordinal);
    private static string NullableId(long? value) => value?.ToString() ?? "NULL";

    // Turso Hrana 协议返回值是 {"type":"text","value":"..."} 或 {"type":"integer","value":"123"} 对象，
    // 需要先取出 "value" 字段，否则 ToString() 得到的是 JSON 字符串而非真实值。
    private static bool IsNullValue(JsonElement value) =>
        value.ValueKind == JsonValueKind.Null ||
        (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String && t.GetString() == "null");

    private static JsonElement RawValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("value", out var inner))
            return inner;
        return value;
    }

    private static string Text(JsonElement value)
    {
        if (IsNullValue(value)) return "";
        var raw = RawValue(value);
        if (raw.ValueKind == JsonValueKind.Null) return "";
        return raw.ValueKind == JsonValueKind.String ? raw.GetString() ?? "" : raw.ToString();
    }

    private static string? NullableText(JsonElement value)
    {
        if (IsNullValue(value)) return null;
        var raw = RawValue(value);
        if (raw.ValueKind == JsonValueKind.Null) return null;
        return raw.ValueKind == JsonValueKind.String ? raw.GetString() : raw.ToString();
    }

    private static int Int(JsonElement value)
    {
        if (IsNullValue(value)) return 0;
        var raw = RawValue(value);
        if (raw.ValueKind == JsonValueKind.Number) return raw.GetInt32();
        if (raw.ValueKind == JsonValueKind.String) return int.TryParse(raw.GetString(), out var p) ? p : 0;
        return int.TryParse(raw.ToString(), out var p2) ? p2 : 0;
    }

    private static long Long(JsonElement value)
    {
        if (IsNullValue(value)) return 0;
        var raw = RawValue(value);
        if (raw.ValueKind == JsonValueKind.Number) return raw.GetInt64();
        if (raw.ValueKind == JsonValueKind.String) return long.TryParse(raw.GetString(), out var p) ? p : 0;
        return long.TryParse(raw.ToString(), out var p2) ? p2 : 0;
    }

    private static long? NullableLong(JsonElement value) => IsNullValue(value) ? null : Long(value);
}
