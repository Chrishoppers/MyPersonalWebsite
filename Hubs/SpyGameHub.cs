using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace MyPersonalWebsite.Hubs
{
    public class SpyGameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, SpyRoom> _rooms = new();
        private static readonly ConcurrentDictionary<string, (string playerId, string roomCode)> _connMap = new();

        // 词对库（平民词 / 卧底词）
        private static readonly (string c, string s)[] WordPairs =
        {
            ("苹果", "梨"), ("猫", "狗"), ("足球", "篮球"), ("奶茶", "咖啡"),
            ("手机", "电脑"), ("面条", "米饭"), ("冰箱", "空调"), ("眼镜", "墨镜"),
            ("医生", "护士"), ("老师", "教授"), ("地铁", "公交"), ("可乐", "雪碧")
        };

        // ============ 创建房间 ============
        public async Task<object> CreateRoom(string name)
        {
            var code = NewCode();
            var player = new SpyPlayer { PlayerId = NewId(), Name = Sanitize(name), IsHost = true, ConnectionId = Context.ConnectionId };
            var room = new SpyRoom { Code = code, HostPlayerId = player.PlayerId };
            room.Players.Add(player);
            _rooms[code] = room;
            _connMap[Context.ConnectionId] = (player.PlayerId, code);
            await Groups.AddToGroupAsync(Context.ConnectionId, code);
            await Clients.Caller.SendAsync("RoomCreated", new { success = true, room = RoomDto(room), playerId = player.PlayerId });
            return new { success = true, roomCode = code, playerId = player.PlayerId };
        }

        // ============ 加入房间 ============
        public async Task<object> JoinRoom(string code, string name)
        {
            code = (code ?? "").Trim().ToUpper();
            if (!_rooms.TryGetValue(code, out var room))
                return new { success = false, message = "房间不存在" };
            if (room.Status != "waiting")
                return new { success = false, message = "游戏已开始" };
            if (room.Players.Count >= 8)
                return new { success = false, message = "房间已满" };
            if (room.Players.Any(p => p.Name == Sanitize(name)))
                return new { success = false, message = "昵称已被使用" };

            var player = new SpyPlayer { PlayerId = NewId(), Name = Sanitize(name), ConnectionId = Context.ConnectionId };
            room.Players.Add(player);
            _connMap[Context.ConnectionId] = (player.PlayerId, code);
            await Groups.AddToGroupAsync(Context.ConnectionId, code);
            await Clients.Caller.SendAsync("JoinedRoom", new { success = true, room = RoomDto(room), playerId = player.PlayerId });
            await Clients.Group(code).SendAsync("RoomUpdated", RoomDto(room));
            return new { success = true, playerId = player.PlayerId };
        }

        // ============ 开始游戏（房主） ============
        public async Task StartGame(string code, string playerId)
        {
            if (!_rooms.TryGetValue(code, out var room)) return;
            if (room.HostPlayerId != playerId) { await Clients.Caller.SendAsync("Error", "只有房主可以开始"); return; }
            if (room.Players.Count < 4) { await Clients.Caller.SendAsync("Error", "至少需要 4 位玩家"); return; }

            var pair = WordPairs[Random.Shared.Next(WordPairs.Length)];
            room.CivilianWord = pair.c;
            room.SpyWord = pair.s;
            room.SpyIndex = Random.Shared.Next(room.Players.Count);

            for (int i = 0; i < room.Players.Count; i++)
            {
                var p = room.Players[i];
                p.Alive = true;
                p.Role = i == room.SpyIndex ? "spy" : "civilian";
                p.Word = i == room.SpyIndex ? room.SpyWord : room.CivilianWord;
            }

            room.Status = "playing";
            room.Round = 1;
            room.Phase = "speaking";
            room.SpeakOrder = room.Players.Select(p => p.PlayerId).ToList();
            room.PendingSpeakers = room.SpeakOrder.Where(id => room.Players.First(p => p.PlayerId == id).Alive).ToList();
            room.Clues.Clear();
            room.Votes.Clear();

            // 私发身份
            foreach (var p in room.Players)
                await Clients.Client(p.ConnectionId).SendAsync("YourRole", new { word = p.Word, role = p.Role });

            await Clients.Group(code).SendAsync("GameStarted", new
            {
                room = RoomDto(room),
                order = room.SpeakOrder,
                currentSpeaker = room.PendingSpeakers[0]
            });
        }

        // ============ 发言 ============
        public async Task Speak(string code, string playerId, string clue)
        {
            if (!_rooms.TryGetValue(code, out var room)) return;
            if (room.Status != "playing" || room.Phase != "speaking") return;
            if (room.PendingSpeakers.Count == 0 || room.PendingSpeakers[0] != playerId) return;

            var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || !player.Alive) return;

            var text = Sanitize(clue);
            if (string.IsNullOrWhiteSpace(text)) text = "……";
            room.Clues[playerId] = text;
            room.PendingSpeakers.RemoveAt(0);
            await Clients.Group(code).SendAsync("ClueSpoken", new { playerId, name = player.Name, clue = text });

            if (room.PendingSpeakers.Count == 0)
            {
                room.Phase = "voting";
                await Clients.Group(code).SendAsync("PhaseChanged", new { phase = "voting", message = "投票阶段开始，请投票" });
            }
            else
            {
                await Clients.Group(code).SendAsync("TurnChanged", new { currentSpeaker = room.PendingSpeakers[0] });
            }
        }

        // ============ 投票 ============
        public async Task Vote(string code, string playerId, string targetPlayerId)
        {
            if (!_rooms.TryGetValue(code, out var room)) return;
            if (room.Status != "playing" || room.Phase != "voting") return;
            var voter = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (voter == null || !voter.Alive) return;
            var target = room.Players.FirstOrDefault(p => p.PlayerId == targetPlayerId);
            if (target == null || !target.Alive) return;

            room.Votes[playerId] = targetPlayerId;
            await Clients.Group(code).SendAsync("VoteCast", new { name = voter.Name });

            var aliveIds = room.Players.Where(p => p.Alive).Select(p => p.PlayerId).ToList();
            if (aliveIds.All(id => room.Votes.ContainsKey(id)))
                await ResolveVotes(room);
        }

        private async Task ResolveVotes(SpyRoom room)
        {
            var alive = room.Players.Where(p => p.Alive).ToList();
            var tally = new Dictionary<string, int>();
            foreach (var kv in room.Votes)
            {
                var t = kv.Value;
                if (!alive.Any(p => p.PlayerId == t)) continue;
                tally[t] = tally.GetValueOrDefault(t) + 1;
            }
            int max = tally.Count == 0 ? 0 : tally.Values.Max();
            var tops = alive.Where(p => tally.GetValueOrDefault(p.PlayerId) == max).ToList();
            var eliminated = tops[Random.Shared.Next(tops.Count)];
            eliminated.Alive = false;
            var wasSpy = room.Players.IndexOf(eliminated) == room.SpyIndex;

            await Clients.Group(room.Code).SendAsync("Eliminated", new
            {
                playerId = eliminated.PlayerId,
                name = eliminated.Name,
                wasSpy
            });

            // 判定胜负
            if (wasSpy)
            {
                await EndGame(room, "civilian");
                return;
            }
            var aliveCount = room.Players.Count(p => p.Alive);
            if (aliveCount <= 2)
            {
                await EndGame(room, "spy");
                return;
            }

            room.Round++;
            room.Phase = "speaking";
            room.Clues.Clear();
            room.Votes.Clear();
            room.PendingSpeakers = room.SpeakOrder.Where(id => room.Players.First(p => p.PlayerId == id).Alive).ToList();
            await Clients.Group(room.Code).SendAsync("NewRound", new
            {
                round = room.Round,
                room = RoomDto(room),
                currentSpeaker = room.PendingSpeakers[0]
            });
        }

        private async Task EndGame(SpyRoom room, string winner)
        {
            room.Status = "ended";
            var spy = room.Players[room.SpyIndex];
            await Clients.Group(room.Code).SendAsync("GameOver", new
            {
                winner,               // civilian | spy
                spyName = spy.Name,
                spyWord = room.SpyWord,
                civilianWord = room.CivilianWord,
                room = RoomDto(room)
            });
        }

        // ============ 离开 / 断开 ============
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connMap.TryRemove(Context.ConnectionId, out var map))
            {
                if (_rooms.TryGetValue(map.roomCode, out var room))
                {
                    var player = room.Players.FirstOrDefault(p => p.PlayerId == map.playerId);
                    if (player != null)
                    {
                        room.Players.Remove(player);
                        if (room.Status == "playing") { /* 简单处理：有人离开则判定剩余玩家继续或结束 */ }
                        await Clients.Group(room.Code).SendAsync("RoomUpdated", RoomDto(room));
                    }
                    if (room.Players.Count == 0) _rooms.TryRemove(room.Code, out _);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public static SpyRoom? GetRoom(string code) { _rooms.TryGetValue(code, out var r); return r; }

        // ============ 工具 ============
        private static object RoomDto(SpyRoom room) => new
        {
            code = room.Code,
            status = room.Status,
            round = room.Round,
            phase = room.Phase,
            hostPlayerId = room.HostPlayerId,
            players = room.Players.Select(p => new
            {
                p.PlayerId, p.Name, p.IsHost, p.Alive,
                role = room.Status == "playing" || room.Status == "ended" ? p.Role : (string?)null
            }).ToList()
        };

        private static string NewId() => Guid.NewGuid().ToString("N")[..10];
        private static string NewCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var a = new string(Enumerable.Range(0, 4).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
            var b = new string(Enumerable.Range(0, 4).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
            var code = $"{a}-{b}";
            return _rooms.ContainsKey(code) ? NewCode() : code;
        }
        private static string Sanitize(string? s) => (s ?? "").Trim().Length > 12 ? (s ?? "").Trim()[..12] : (s ?? "").Trim();
    }

    public class SpyRoom
    {
        public string Code { get; set; } = "";
        public string HostPlayerId { get; set; } = "";
        public string Status { get; set; } = "waiting";   // waiting | playing | ended
        public string Phase { get; set; } = "waiting";    // speaking | voting
        public int Round { get; set; }
        public int SpyIndex { get; set; } = -1;
        public string CivilianWord { get; set; } = "";
        public string SpyWord { get; set; } = "";
        public int SpeakerIndex { get; set; } = -1;
        public List<string> SpeakOrder { get; set; } = new();
        public List<string> PendingSpeakers { get; set; } = new();
        public Dictionary<string, string> Clues { get; set; } = new();
        public Dictionary<string, string> Votes { get; set; } = new();
        public List<SpyPlayer> Players { get; set; } = new();
    }

    public class SpyPlayer
    {
        public string PlayerId { get; set; } = "";
        public string Name { get; set; } = "";
        public string ConnectionId { get; set; } = "";
        public bool IsHost { get; set; }
        public bool Alive { get; set; } = true;
        public string Role { get; set; } = "";   // civilian | spy
        public string Word { get; set; } = "";
    }
}
