using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Models.Werewolf;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MyPersonalWebsite.Hubs
{
    public class WerewolfHub : Hub
    {
        // 存储所有房间
        private static readonly ConcurrentDictionary<string, WerewolfGameState> _games = new();
        private static readonly ConcurrentDictionary<string, string> _playerToRoom = new();
        private static readonly ConcurrentDictionary<string, string> _connectionToPlayer = new();

        // 投票存储
        private static readonly ConcurrentDictionary<string, Dictionary<int, int>> _wolfVotes = new();
        private static readonly ConcurrentDictionary<string, Dictionary<int, int>> _dayVotes = new();
        private static readonly ConcurrentDictionary<string, Dictionary<int, int>> _sheriffVotes = new();

        // 自爆标记
        private static readonly ConcurrentDictionary<string, bool> _wolfExplode = new();

        // 猎人开枪标记
        private static readonly ConcurrentDictionary<string, int> _hunterShoot = new();

        // ============================================================
        // 1. 创建房间
        // ============================================================
        public async Task<object> CreateRoom(string hostName, int playerCount = 10, List<string>? selectedRoles = null)
        {
            var roomId = GenerateRoomCode();
            var playerId = $"host_{Guid.NewGuid():N}";

            var game = new WerewolfGameState
            {
                RoomId = roomId,
                Phase = GamePhase.Setup,
                StartedAt = DateTime.Now,
                Players = new List<WerewolfPlayer>()
            };

            var hostPlayer = new WerewolfPlayer
            {
                SeatNumber = 0,
                PlayerId = playerId,
                Nickname = hostName + " (主控)",
                AvatarEmoji = "👑",
                ConnectionId = Context.ConnectionId,
                IsAlive = true,
                IsSpectator = true,
                Role = RoleType.Villager
            };
            game.Players.Add(hostPlayer);

            _games[roomId] = game;
            _playerToRoom[playerId] = roomId;
            _connectionToPlayer[Context.ConnectionId] = playerId;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("RoomCreated", new { success = true, roomId });

            return new { success = true, roomId, playerId };
        }

        // ============================================================
        // 2. 加入游戏
        // ============================================================
        public async Task<object> JoinGame(string roomId, string nickname, string avatarEmoji = "🧑")
        {
            if (!_games.TryGetValue(roomId, out var game))
                return new { success = false, message = "房间不存在" };

            if (game.Phase != GamePhase.Setup && game.Phase != GamePhase.Seating)
                return new { success = false, message = "游戏已开始，无法加入" };

            var playerCount = game.Players.Count(p => !p.IsSpectator);
            var maxPlayers = 12;
            if (playerCount >= maxPlayers)
            {
                var spectatorId = $"spectator_{Guid.NewGuid():N}";
                var spectator = new WerewolfPlayer
                {
                    SeatNumber = 0,
                    PlayerId = spectatorId,
                    Nickname = nickname + " (观战)",
                    AvatarEmoji = avatarEmoji,
                    ConnectionId = Context.ConnectionId,
                    IsAlive = true,
                    IsSpectator = true,
                    Role = RoleType.Villager
                };
                game.Players.Add(spectator);
                _playerToRoom[spectatorId] = roomId;
                _connectionToPlayer[Context.ConnectionId] = spectatorId;
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);
                return new { success = true, isSpectator = true, message = "已进入观战模式" };
            }

            var usedSeats = game.Players.Where(p => !p.IsSpectator).Select(p => p.SeatNumber).ToHashSet();
            var seatNumber = 1;
            while (usedSeats.Contains(seatNumber) && seatNumber <= 12) seatNumber++;
            if (seatNumber > 12)
                return new { success = false, message = "座位已满" };

            var playerId = $"player_{Guid.NewGuid():N}";
            var player = new WerewolfPlayer
            {
                SeatNumber = seatNumber,
                PlayerId = playerId,
                Nickname = nickname,
                AvatarEmoji = avatarEmoji,
                ConnectionId = Context.ConnectionId,
                IsAlive = true,
                IsSpectator = false,
                IsReady = false,
                Role = RoleType.Villager
            };

            game.Players.Add(player);
            _playerToRoom[playerId] = roomId;
            _connectionToPlayer[Context.ConnectionId] = playerId;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);
            await Clients.Caller.SendAsync("JoinedGame", new { success = true, seatNumber, playerId });

            var occupied = game.Players.Count(p => !p.IsSpectator);
            var totalSeats = GetTotalSeats(game);
            if (occupied >= totalSeats)
            {
                game.Phase = GamePhase.Seating;
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "seating", game.Day, game.Night);
                await PlayVoiceAnnounce(roomId, "所有玩家已就坐，准备发牌", "high");
            }

            return new { success = true, seatNumber, playerId };
        }

        // ============================================================
        // 3. 准备状态
        // ============================================================
        public async Task ToggleReady(string playerId, bool isReady)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.IsSpectator) return;

            player.IsReady = isReady;
            await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

            var players = game.Players.Where(p => !p.IsSpectator && p.IsAlive).ToList();
            var allReady = players.All(p => p.IsReady) && players.Count > 0;

            if (allReady && game.Phase == GamePhase.Setup)
            {
                await StartDealing(roomId);
            }
        }

        // ============================================================
        // 4. 开始发牌
        // ============================================================
        public async Task StartDealing(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;
            if (game.Phase != GamePhase.Setup && game.Phase != GamePhase.Seating) return;

            game.Phase = GamePhase.Dealing;
            await Clients.Group(roomId).SendAsync("PhaseUpdate", "dealing", game.Day, game.Night);
            await PlayVoiceAnnounce(roomId, "开始发牌，请查看您的身份", "high");

            var players = game.Players.Where(p => !p.IsSpectator).ToList();
            var roles = GenerateRoles(players.Count, game);
            var shuffledRoles = ShuffleArray(roles);

            for (int i = 0; i < players.Count && i < shuffledRoles.Count; i++)
            {
                players[i].Role = shuffledRoles[i];
                players[i].HasRevealed = false;
            }

            // 通知玩家查看身份
            foreach (var p in players)
            {
                await Clients.Client(p.ConnectionId).SendAsync("ReceiveRole", new
                {
                    seatNumber = p.SeatNumber,
                    role = p.Role.ToString(),
                    isSpectator = false
                });
            }

            var spectators = game.Players.Where(p => p.IsSpectator);
            foreach (var s in spectators)
            {
                await Clients.Client(s.ConnectionId).SendAsync("ReceiveRole", new
                {
                    seatNumber = 0,
                    role = "观战",
                    isSpectator = true
                });
            }

            await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

            game.Phase = GamePhase.Revealing;
            await Clients.Group(roomId).SendAsync("PhaseUpdate", "revealing", game.Day, game.Night);
            await PlayVoiceAnnounce(roomId, "请所有玩家在手机上查看自己的身份", "high");

            // 10秒后自动进入第一夜
            _ = Task.Delay(10000).ContinueWith(async _ =>
            {
                // 检查是否10人以上 -> 警长竞选
                var playerCount = game.Players.Count(p => !p.IsSpectator);
                if (playerCount >= 10 && game.Day == 0)
                {
                    await StartSheriffElection(roomId);
                }
                else
                {
                    await StartNight(roomId);
                }
            });
        }
    }
}
