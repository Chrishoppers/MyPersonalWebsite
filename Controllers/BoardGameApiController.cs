using Microsoft.AspNetCore.Mvc;
using MyPersonalWebsite.Services.BoardGame;

namespace MyPersonalWebsite.Controllers;

[ApiController]
[Route("api/boardgame")]
public sealed class BoardGameApiController : Controller
{
    private const string PrivacyVersion = "v1";
    private static readonly HashSet<string> GameTypes = ["gomoku", "xiangqi", "chess"];
    private static readonly HashSet<string> GuestAvatars = ["🐱", "🐶", "🐼", "🦊", "🐸", "🐵", "🦁", "🐯"];
    private readonly BoardGameService _game;
    private readonly QrCodeService _qr;
    private readonly IConfiguration _configuration;

    public BoardGameApiController(BoardGameService game, QrCodeService qr, IConfiguration configuration)
    {
        _game = game;
        _qr = qr;
        _configuration = configuration;
    }

    private long? UserId => HttpContext.Session.GetInt32("UserId");
    private string Username => HttpContext.Session.GetString("Username") ?? "玩家";
    private string GuestKey
    {
        get
        {
            var key = HttpContext.Session.GetString("GuestKey");
            if (!string.IsNullOrWhiteSpace(key)) return key;
            key = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString("GuestKey", key);
            return key;
        }
    }

    [HttpGet("privacy-status")]
    public async Task<IActionResult> PrivacyStatus() => Ok(new { agreed = await _game.HasPrivacyAgreementAsync(UserId, GuestKey, PrivacyVersion) });

    [HttpPost("privacy-agree")]
    public async Task<IActionResult> AgreePrivacy()
    {
        // 隐私同意记录是尽力而为的授权留痕，写入失败也不应阻塞用户进入对局。
        try
        {
            if (await _game.HasPrivacyAgreementAsync(UserId, GuestKey, PrivacyVersion))
                return Ok(new { ok = true, saved = true });
            var saved = await _game.SavePrivacyAgreementAsync(UserId, GuestKey, PrivacyVersion);
            return Ok(new { ok = true, saved });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 隐私同意记录保存失败（不影响继续使用）: {ex.Message}");
            return Ok(new { ok = true, saved = false });
        }
    }

    public sealed class CreateRequest
    {
        public string GameType { get; set; } = "";
        public string Mode { get; set; } = "ai";
        public string? AiLevel { get; set; }
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateRequest request)
    {
        if (!await HasAgreement()) return BadRequest(new { error = "请先阅读并同意隐私声明。" });
        if (!GameTypes.Contains(request.GameType) || request.Mode is not ("ai" or "pvp")) return BadRequest(new { error = "棋类或对局模式无效。" });
        var level = request.Mode == "ai" ? request.AiLevel is "easy" or "medium" or "hard" ? request.AiLevel : "medium" : null;
        var aiSide = request.Mode == "ai" ? request.GameType == "gomoku" ? "white" : "black" : null;
        var guest = !UserId.HasValue;
        var displayName = guest ? EnsureGuestName() : Username;
        var avatar = guest ? EnsureGuestAvatar() : "😊";
        var created = await _game.CreateRoomAsync(request.GameType, request.Mode, level, aiSide, UserId, displayName, avatar, guest, guest ? GuestKey : null);
        return Ok(new { roomId = created.Id, roomCode = created.Code, joinUrl = BuildJoinUrl(created.Code), qrBase64 = _qr.GeneratePngBase64(BuildJoinUrl(created.Code)) });
    }

    public sealed class MatchRequest { public string GameType { get; set; } = ""; }

    [HttpPost("match")]
    public async Task<IActionResult> Match([FromBody] MatchRequest request)
    {
        if (!await HasAgreement()) return BadRequest(new { error = "请先阅读并同意隐私声明。" });
        if (!GameTypes.Contains(request.GameType)) return BadRequest(new { error = "棋类无效。" });
        var waiting = await _game.FindWaitingRoomForUserAsync(request.GameType, UserId, GuestKey);
        if (waiting is not null)
        {
            return Ok(new { roomCode = waiting.RoomCode, isCreator = true, gameType = waiting.GameType });
        }
        var waitingOpponent = await _game.FindWaitingRoomAsync(request.GameType);
        if (waitingOpponent is not null && !IsCurrentUser(waitingOpponent))
        {
            var joiningAsGuest = !UserId.HasValue;
            var guestName = joiningAsGuest ? EnsureGuestName() : Username;
            var guestAvatar = joiningAsGuest ? EnsureGuestAvatar() : "😊";
            var joined = await _game.JoinRoomAsync(waitingOpponent.RoomCode, UserId, guestName, guestAvatar, joiningAsGuest, joiningAsGuest ? GuestKey : null);
            if (!joined) return Conflict(new { error = "匹配房间刚刚被加入，请重试。" });
            return Ok(new { roomCode = waitingOpponent.RoomCode, isCreator = false, gameType = waitingOpponent.GameType });
        }
        var guest = !UserId.HasValue;
        var created = await _game.CreateRoomAsync(request.GameType, "pvp", null, null, UserId,
            guest ? EnsureGuestName() : Username, guest ? EnsureGuestAvatar() : "😊", guest, guest ? GuestKey : null);
        var url = BuildJoinUrl(created.Code);
        return Ok(new { roomCode = created.Code, isCreator = true, joinUrl = url, qrBase64 = _qr.GeneratePngBase64(url) });
    }

    public sealed class JoinRequest
    {
        public string Code { get; set; } = "";
        public string GuestName { get; set; } = "游客";
        public string GuestAvatar { get; set; } = "🐱";
        public bool AsGuest { get; set; } = true;
    }

    [HttpPost("join")]
    public async Task<IActionResult> JoinRoom([FromBody] JoinRequest request)
    {
        if (!await HasAgreement()) return BadRequest(new { error = "请先阅读并同意隐私声明。" });
        var code = request.Code.Trim().ToUpperInvariant();
        if (code.Length != 6 || code.Any(character => !char.IsAsciiLetterOrDigit(character))) return BadRequest(new { error = "房间码无效。" });
        var room = await _game.GetRoomAsync(code);
        if (room is null || room.Mode != "pvp" || room.Status != "waiting") return BadRequest(new { error = "房间不存在或已开始。" });
        var asGuest = request.AsGuest || !UserId.HasValue;
        var guestName = asGuest ? NormalizeGuestName(request.GuestName) : Username;
        var avatar = asGuest ? NormalizeAvatar(request.GuestAvatar) : "😊";
        if (asGuest)
        {
            HttpContext.Session.SetString("GuestName", guestName);
            HttpContext.Session.SetString("GuestAvatar", avatar);
        }
        var ok = await _game.JoinRoomAsync(code, asGuest ? null : UserId, guestName, avatar, asGuest, asGuest ? GuestKey : null);
        if (!ok) return BadRequest(new { error = "房间不存在或已开始。" });
        return Ok(new { ok = true, roomCode = code, gameType = room.GameType });
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        if (UserId.HasValue) return Ok(new { name = Username, avatar = "😊", isGuest = false });
        return Ok(new { name = EnsureGuestName(), avatar = EnsureGuestAvatar(), isGuest = true });
    }

    [HttpGet("guest-avatars")]
    public IActionResult Avatars() => Ok(GuestAvatars);

    [HttpGet("replays/{gameType}")]
    public async Task<IActionResult> Replays(string gameType)
    {
        if (!GameTypes.Contains(gameType)) return BadRequest(new { error = "棋类无效。" });
        var replays = await _game.GetReplaysAsync(gameType);
        return Ok(replays.Select(room => new
        {
            room.Id, room.RoomCode, room.GameType, room.HostName, room.GuestName, room.Winner,
            room.ResultReason, room.MoveCount, room.FinishedAt
        }));
    }

    [HttpGet("replay/{id:long}/moves")]
    public async Task<IActionResult> ReplayMoves(long id)
    {
        var room = await _game.GetRoomByIdAsync(id);
        if (room is null || room.Status != "finished" || room.Mode != "pvp" || room.HostIsGuest != 0 || room.GuestIsGuest != 0)
            return NotFound();
        return Ok(new { gameType = room.GameType, moves = await _game.GetMovesAsync(id) });
    }

    private Task<bool> HasAgreement() => _game.HasPrivacyAgreementAsync(UserId, GuestKey, PrivacyVersion);
    private string BuildJoinUrl(string code)
    {
        var configuredBaseUrl = (_configuration["BoardGame:PublicBaseUrl"] ?? "https://chris-hopper.org").TrimEnd('/');
        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
            baseUri = new Uri("https://chris-hopper.org");
        return new Uri(baseUri, $"/BoardGame/Join/{Uri.EscapeDataString(code)}").ToString();
    }

    private bool IsCurrentUser(RoomDto room) => UserId.HasValue ? room.HostUserId == UserId : room.HostGuestKey == GuestKey;
    private string EnsureGuestName()
    {
        var name = HttpContext.Session.GetString("GuestName");
        if (!string.IsNullOrWhiteSpace(name)) return name;
        HttpContext.Session.SetString("GuestName", "无名");
        return "无名";
    }
    private string EnsureGuestAvatar()
    {
        var avatar = HttpContext.Session.GetString("GuestAvatar");
        if (avatar is not null && GuestAvatars.Contains(avatar)) return avatar;
        HttpContext.Session.SetString("GuestAvatar", "🐱");
        return "🐱";
    }
    private static string NormalizeGuestName(string name)
    {
        var normalized = new string((name ?? "").Where(character => !char.IsControl(character)).Take(12).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "游客_无名" : $"游客_{normalized}";
    }
    private static string NormalizeAvatar(string avatar) => GuestAvatars.Contains(avatar) ? avatar : "🐱";
}
