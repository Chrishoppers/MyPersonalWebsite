using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Models.Werewolf;
using System.Collections.Concurrent;

namespace MyPersonalWebsite.Hubs
{
    public class WerewolfHub : Hub
    {
        // ============================================================
        // 存储所有房间
        // ============================================================
        private static readonly ConcurrentDictionary<string, WerewolfGameState> _games = new();
        private static readonly ConcurrentDictionary<string, string> _playerToRoom = new();
        private static readonly ConcurrentDictionary<string, string> _connectionToPlayer = new();

        // ============================================================
        // 投票存储
        // ============================================================
        private static readonly ConcurrentDictionary<string, Dictionary<int, int>> _wolfVotes = new();
        private static readonly ConcurrentDictionary<string, Dictionary<int, int>> _dayVotes = new();
        private static readonly ConcurrentDictionary<string, Dictionary<int, int>> _sheriffVotes = new();
        private static readonly ConcurrentDictionary<string, bool> _wolfExplode = new();

        // ============================================================
        // AI 自动计时控制
        // ============================================================
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _autoTimers = new();
        private static readonly ConcurrentDictionary<string, bool> _isPaused = new();
        private static readonly ConcurrentDictionary<string, double> _speedMultiplier = new();

        // ============================================================
        // 行动记录
        // ============================================================
        private static readonly ConcurrentDictionary<string, bool> _guardActions = new();
        private static readonly ConcurrentDictionary<string, bool> _seerActions = new();
        private static readonly ConcurrentDictionary<string, bool> _witchActions = new();

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
                Players = new List<WerewolfPlayer>(),
                SpeedMultiplier = 1.0
            };

            game.Players.Add(new WerewolfPlayer
            {
                SeatNumber = 0,
                PlayerId = playerId,
                Nickname = hostName + " (主控)",
                AvatarEmoji = "👑",
                ConnectionId = Context.ConnectionId,
                IsAlive = true,
                IsSpectator = true,
                Role = RoleType.Villager
            });

            _games[roomId] = game;
            _playerToRoom[playerId] = roomId;
            _connectionToPlayer[Context.ConnectionId] = playerId;
            _speedMultiplier[roomId] = 1.0;

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
                return new { success = false, message = "游戏已开始" };

            var playerCount = game.PlayerCount;
            if (playerCount >= 12)
            {
                var spectatorId = $"spectator_{Guid.NewGuid():N}";
                game.Players.Add(new WerewolfPlayer
                {
                    SeatNumber = 0,
                    PlayerId = spectatorId,
                    Nickname = nickname + " (观战)",
                    AvatarEmoji = avatarEmoji,
                    ConnectionId = Context.ConnectionId,
                    IsAlive = true,
                    IsSpectator = true,
                    Role = RoleType.Villager
                });
                _playerToRoom[spectatorId] = roomId;
                _connectionToPlayer[Context.ConnectionId] = spectatorId;
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);
                return new { success = true, isSpectator = true };
            }

            var usedSeats = game.Players.Where(p => !p.IsSpectator).Select(p => p.SeatNumber).ToHashSet();
            var seatNumber = 1;
            while (usedSeats.Contains(seatNumber) && seatNumber <= 12) seatNumber++;
            if (seatNumber > 12) return new { success = false, message = "座位已满" };

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

            if (game.PlayerCount >= GetTotalSeats(game))
            {
                game.Phase = GamePhase.Seating;
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "seating", game.Day, game.Night);
                await PlayVoiceAnnounce(roomId, "所有玩家已就坐，准备发牌");
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

            var players = game.AlivePlayers;
            if (players.All(p => p.IsReady) && players.Count > 0 && game.Phase == GamePhase.Setup)
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
            await PlayVoiceAnnounce(roomId, "开始发牌，请查看您的身份");

            var players = game.Players.Where(p => !p.IsSpectator).ToList();
            var roles = GenerateRoles(players.Count);
            var shuffledRoles = ShuffleList(roles);

            for (int i = 0; i < players.Count && i < shuffledRoles.Count; i++)
            {
                players[i].Role = shuffledRoles[i];
                players[i].HasRevealed = false;
            }

            foreach (var p in players)
            {
                await Clients.Client(p.ConnectionId).SendAsync("ReceiveRole", new
                {
                    seatNumber = p.SeatNumber,
                    role = p.Role.ToString(),
                    isSpectator = false
                });
            }

            foreach (var s in game.Players.Where(p => p.IsSpectator))
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
            await PlayVoiceAnnounce(roomId, "请所有玩家在手机上查看自己的身份");

            // 10秒后自动进入下一步（警长竞选或直接夜晚）
            _ = Task.Delay(10000).ContinueWith(async _ =>
            {
                if (game.PlayerCount >= 10)
                {
                    await StartSheriffElection(roomId);
                }
                else
                {
                    await StartNight(roomId);
                }
            });
        }

        // ============================================================
        // 5. 警长竞选
        // ============================================================
        public async Task StartSheriffElection(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;
            if (game.PlayerCount < 10 || game.Day > 0 || game.IsSheriffElection) return;

            game.IsSheriffElection = true;
            game.Phase = GamePhase.SheriffElection;
            _sheriffVotes[roomId] = new Dictionary<int, int>();

            await Clients.Group(roomId).SendAsync("PhaseUpdate", "sheriff_election", game.Day, game.Night);
            await PlayVoiceAnnounce(roomId, "警长竞选开始，请参与竞选的玩家在手机上举手");

            var alivePlayers = game.AlivePlayers;
            foreach (var p in alivePlayers)
            {
                await Clients.Client(p.ConnectionId).SendAsync("SheriffElectionStart", new
                {
                    seatNumber = p.SeatNumber,
                    candidates = alivePlayers.Select(ap => ap.SeatNumber).ToList()
                });
            }

            // 30秒后开始投票
            _ = Task.Delay(30000).ContinueWith(async _ =>
            {
                await Clients.Group(roomId).SendAsync("DisplayMessage", "竞选发言结束，请警下玩家投票");
                await PlayVoiceAnnounce(roomId, "请警下玩家在手机上投票");

                var alive = game.AlivePlayers;
                foreach (var p in alive)
                {
                    var candidates = alive.Where(ap => ap.SeatNumber != p.SeatNumber).Select(ap => ap.SeatNumber).ToList();
                    await Clients.Client(p.ConnectionId).SendAsync("SheriffVote", new
                    {
                        candidates = candidates,
                        isCandidate = _sheriffVotes[roomId].ContainsKey(p.SeatNumber)
                    });
                }

                // 20秒后结算
                _ = Task.Delay(20000).ContinueWith(async _ =>
                {
                    await ResolveSheriffElection(roomId);
                });
            });
        }

        public async Task JoinSheriffElection(string playerId)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || !player.IsAlive || player.IsSpectator) return;

            if (!_sheriffVotes.ContainsKey(roomId)) _sheriffVotes[roomId] = new Dictionary<int, int>();
            if (!_sheriffVotes[roomId].ContainsKey(player.SeatNumber))
            {
                _sheriffVotes[roomId][player.SeatNumber] = 0;
                await Clients.Group(roomId).SendAsync("SheriffCandidateUpdate", _sheriffVotes[roomId].Keys.ToList());
            }
        }

        public async Task SheriffVote(string playerId, int targetSeat)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var voter = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (voter == null || !voter.IsAlive || voter.IsSpectator) return;
            if (voter.SeatNumber == targetSeat) return;
            if (!_sheriffVotes.ContainsKey(roomId) || !_sheriffVotes[roomId].ContainsKey(targetSeat)) return;

            _sheriffVotes[roomId][targetSeat] = _sheriffVotes[roomId].GetValueOrDefault(targetSeat) + 1;
        }

        private async Task ResolveSheriffElection(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            if (!_sheriffVotes.ContainsKey(roomId) || _sheriffVotes[roomId].Count == 0)
            {
                await PlayVoiceAnnounce(roomId, "无人竞选警长，本局无警长");
                game.IsSheriffElection = false;
                await StartNight(roomId);
                return;
            }

            var votes = _sheriffVotes[roomId];
            var maxVotes = votes.Values.Max();
            var winners = votes.Where(v => v.Value == maxVotes).Select(v => v.Key).ToList();

            if (winners.Count == 1)
            {
                var sheriff = game.Players.FirstOrDefault(p => p.SeatNumber == winners.First());
                if (sheriff != null)
                {
                    sheriff.IsSheriff = true;
                    game.SheriffId = sheriff.SeatNumber;
                    await Clients.Group(roomId).SendAsync("SheriffElected", new
                    {
                        seatNumber = sheriff.SeatNumber,
                        nickname = sheriff.Nickname
                    });
                    await PlayVoiceAnnounce(roomId, $"{sheriff.SeatNumber}号当选警长");
                }
            }
            else
            {
                await PlayVoiceAnnounce(roomId, "平票，无人当选警长");
            }

            game.IsSheriffElection = false;
            _sheriffVotes.Remove(roomId, out _);
            await StartNight(roomId);
        }

        // ============================================================
        // 6. 夜晚流程（AI自动推进）
        // ============================================================
        public async Task StartNight(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            game.Night++;
            _wolfVotes[roomId] = new Dictionary<int, int>();
            _wolfExplode[roomId] = false;
            _guardActions[roomId] = false;
            _seerActions[roomId] = false;
            _witchActions[roomId] = false;

            // 守卫行动
            game.Phase = GamePhase.NightGuard;
            await Clients.Group(roomId).SendAsync("PhaseUpdate", "night_guard", game.Day, game.Night);
            await PlayVoiceAnnounce(roomId, $"第{game.Night}夜，守卫请睁眼");

            var guard = game.AlivePlayers.FirstOrDefault(p => p.Role == RoleType.Guard);
            if (guard != null)
            {
                var targets = game.AlivePlayers.Select(p => p.SeatNumber).ToList();
                await Clients.Client(guard.ConnectionId).SendAsync("GuardAction", new
                {
                    night = game.Night,
                    targets = targets,
                    canProtectSelf = game.Night == 1,
                    lastProtected = guard.GuardProtectedNight
                });
            }

            // AI自动计时（20秒）
            _ = StartAutoTimer(roomId, 20, "守卫行动");
        }

        // ============================================================
        // 7. AI自动计时器（核心）
        // ============================================================
        private async Task StartAutoTimer(string roomId, int seconds, string phaseName)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;
            if (game.Phase == GamePhase.GameOver) return;

            // 获取速度倍数
            var speed = _speedMultiplier.TryGetValue(roomId, out var s) ? s : 1.0;
            var actualSeconds = (int)(seconds / speed);

            await Clients.Group(roomId).SendAsync("DisplayMessage", $"{phaseName} - {actualSeconds}秒");

            for (int i = actualSeconds; i > 0; i--)
            {
                if (i <= 5)
                {
                    await Clients.Group(roomId).SendAsync("DisplayMessage", $"⏱ {i}");
                }

                // 检查暂停
                while (_isPaused.TryGetValue(roomId, out var paused) && paused)
                {
                    await Task.Delay(500);
                }

                // 检查是否所有人已行动
                if (CheckAllPlayersActed(roomId))
                {
                    await Clients.Group(roomId).SendAsync("DisplayMessage", "✅ 所有人已行动");
                    await NextPhase(roomId);
                    return;
                }

                await Task.Delay(1000);
            }

            // 超时自动推进
            await Clients.Group(roomId).SendAsync("DisplayMessage", "⏱ 时间到");
            await NextPhase(roomId);
        }

        private bool CheckAllPlayersActed(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return false;

            var phase = game.Phase;
            var alivePlayers = game.AlivePlayers;

            switch (phase)
            {
                case GamePhase.NightGuard:
                    var guard = alivePlayers.FirstOrDefault(p => p.Role == RoleType.Guard);
                    return guard == null || _guardActions.ContainsKey(roomId);
                case GamePhase.NightSeer:
                    var seer = alivePlayers.FirstOrDefault(p => p.Role == RoleType.Seer);
                    return seer == null || _seerActions.ContainsKey(roomId);
                case GamePhase.NightWerewolf:
                    var wolves = alivePlayers.Where(p => p.Role == RoleType.Werewolf).ToList();
                    if (!wolves.Any()) return true;
                    return _wolfVotes.ContainsKey(roomId) && _wolfVotes[roomId].Keys.Count >= wolves.Count;
                case GamePhase.NightWitch:
                    var witch = alivePlayers.FirstOrDefault(p => p.Role == RoleType.Witch);
                    return witch == null || _witchActions.ContainsKey(roomId);
                default:
                    return false;
            }
        }

        // ============================================================
        // 8. 下一阶段
        // ============================================================
        public async Task NextPhase(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            switch (game.Phase)
            {
                case GamePhase.NightGuard:
                    game.Phase = GamePhase.NightSeer;
                    await Clients.Group(roomId).SendAsync("PhaseUpdate", "night_seer", game.Day, game.Night);
                    await PlayVoiceAnnounce(roomId, "预言家请睁眼");

                    var seer = game.AlivePlayers.FirstOrDefault(p => p.Role == RoleType.Seer);
                    if (seer != null)
                    {
                        var targets = game.AlivePlayers.Select(p => p.SeatNumber).ToList();
                        await Clients.Client(seer.ConnectionId).SendAsync("SeerAction", new { night = game.Night, targets = targets });
                    }
                    _ = StartAutoTimer(roomId, 20, "预言家行动");
                    break;

                case GamePhase.NightSeer:
                    game.Phase = GamePhase.NightWerewolf;
                    await Clients.Group(roomId).SendAsync("PhaseUpdate", "night_werewolf", game.Day, game.Night);
                    await PlayVoiceAnnounce(roomId, "狼人请睁眼");

                    var wolves = game.Werewolves;
                    if (wolves.Any())
                    {
                        var targets = game.AlivePlayers.Select(p => p.SeatNumber).ToList();
                        foreach (var w in wolves)
                        {
                            await Clients.Client(w.ConnectionId).SendAsync("WerewolfAction", new
                            {
                                night = game.Night,
                                targets = targets,
                                wolfSeats = wolves.Select(w2 => w2.SeatNumber).ToList()
                            });
                        }
                    }
                    _ = StartAutoTimer(roomId, 30, "狼人行动");
                    break;

                case GamePhase.NightWerewolf:
                    game.Phase = GamePhase.NightWitch;
                    await Clients.Group(roomId).SendAsync("PhaseUpdate", "night_witch", game.Day, game.Night);
                    await PlayVoiceAnnounce(roomId, "女巫请睁眼");

                    var witch = game.AlivePlayers.FirstOrDefault(p => p.Role == RoleType.Witch);
                    if (witch != null)
                    {
                        var deathSeats = GetWerewolfTargets(roomId);
                        await Clients.Client(witch.ConnectionId).SendAsync("WitchAction", new
                        {
                            night = game.Night,
                            deathSeats = deathSeats,
                            canSaveFirstNight = game.Night == 1
                        });
                    }
                    _ = StartAutoTimer(roomId, 20, "女巫行动");
                    break;

                case GamePhase.NightWitch:
                    await ResolveNight(roomId);
                    break;

                case GamePhase.DayAnnounce:
                    game.Phase = GamePhase.DaySpeech;
                    game.Day++;
                    await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_speech", game.Day, game.Night);
                    await PlayVoiceAnnounce(roomId, $"第{game.Day}天，请开始发言");

                    if (await CheckGameOver(roomId)) return;

                    // 白天发言阶段没有手机操作，等待60秒
                    _ = Task.Delay(60000).ContinueWith(async _ =>
                    {
                        await NextPhase(roomId);
                    });
                    break;

                case GamePhase.DaySpeech:
                    game.Phase = GamePhase.DayVoting;
                    await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_voting", game.Day, game.Night);
                    await PlayVoiceAnnounce(roomId, "开始投票，请选择要放逐的玩家");

                    var alive = game.AlivePlayers;
                    foreach (var p in alive)
                    {
                        var targets = alive.Where(t => t.SeatNumber != p.SeatNumber).Select(t => t.SeatNumber).ToList();
                        await Clients.Client(p.ConnectionId).SendAsync("VotingAction", new
                        {
                            day = game.Day,
                            targets = targets,
                            isSheriff = p.IsSheriff
                        });
                    }
                    _ = StartAutoTimer(roomId, 45, "投票");
                    break;

                case GamePhase.DayVoting:
                    await ResolveDay(roomId);
                    break;

                case GamePhase.DayPK:
                    await ResolvePK(roomId);
                    break;

                case GamePhase.HunterShoot:
                    await ResolveHunterShoot(roomId);
                    break;

                default:
                    break;
            }
        }

        // ============================================================
        // 9. 玩家操作 - 守卫
        // ============================================================
        public async Task GuardProtect(string playerId, int targetSeat)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.Role != RoleType.Guard) return;

            var target = game.Players.FirstOrDefault(p => p.SeatNumber == targetSeat);
            if (target == null || !target.IsAlive) return;

            if (target.GuardProtectedNight == game.Night - 1)
            {
                await Clients.Caller.SendAsync("GuardResult", new { success = false, message = "不能连续守护同一个人" });
                return;
            }

            if (targetSeat == player.SeatNumber && game.Night != 1)
            {
                await Clients.Caller.SendAsync("GuardResult", new { success = false, message = "除了第一天外不能守护自己" });
                return;
            }

            target.IsGuardProtected = true;
            target.GuardProtectedNight = game.Night;
            _guardActions[roomId] = true;

            await Clients.Group(roomId).SendAsync("DisplayMessage", $"{targetSeat}号已被守卫守护");
            await Clients.Caller.SendAsync("GuardResult", new { success = true, targetSeat });
        }

        // ============================================================
        // 10. 玩家操作 - 预言家
        // ============================================================
        public async Task SeerCheck(string playerId, int targetSeat)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.Role != RoleType.Seer) return;

            var target = game.Players.FirstOrDefault(p => p.SeatNumber == targetSeat);
            if (target == null || !target.IsAlive) return;

            _seerActions[roomId] = true;

            var isWerewolf = target.Role == RoleType.Werewolf;
            await Clients.Client(player.ConnectionId).SendAsync("SeerResult", new
            {
                targetSeat = targetSeat,
                isWerewolf = isWerewolf,
                result = isWerewolf ? "🐺 狼人" : "⭐ 好人"
            });
        }

        // ============================================================
        // 11. 玩家操作 - 狼人投票
        // ============================================================
        public async Task WerewolfVote(string playerId, int targetSeat)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.Role != RoleType.Werewolf || !player.IsAlive) return;

            if (_wolfExplode.TryGetValue(roomId, out var exploded) && exploded) return;

            if (!_wolfVotes.ContainsKey(roomId)) _wolfVotes[roomId] = new Dictionary<int, int>();
            _wolfVotes[roomId][player.SeatNumber] = targetSeat;

            var wolves = game.Werewolves;
            var voted = _wolfVotes[roomId].Keys.Count;

            foreach (var w in wolves)
            {
                await Clients.Client(w.ConnectionId).SendAsync("WolfVoteStatus", new { voted, total = wolves.Count });
            }

            if (voted >= wolves.Count)
            {
                var voteCounts = _wolfVotes[roomId].Values.GroupBy(v => v)
                    .ToDictionary(g => g.Key, g => g.Count());

                var maxVotes = voteCounts.Values.Max();
                var targets = voteCounts.Where(v => v.Value == maxVotes).Select(v => v.Key).ToList();

                int selectedTarget = targets.Count == 1 ? targets.First() : targets[new Random().Next(targets.Count)];
                _wolfVotes[roomId]["_result"] = selectedTarget;

                await Clients.Group(roomId).SendAsync("DisplayMessage", $"狼人选择了 {selectedTarget} 号");
            }
        }

        // ============================================================
        // 12. 玩家操作 - 女巫
        // ============================================================
        public async Task WitchAction(string playerId, bool useAntidote, bool usePoison, int targetSeat = -1)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.Role != RoleType.Witch) return;

            _witchActions[roomId] = true;

            var wolfTargets = GetWerewolfTargets(roomId);

            if (useAntidote && wolfTargets.Any())
            {
                var saved = game.Players.FirstOrDefault(p => p.SeatNumber == wolfTargets.First());
                if (saved != null && saved.IsAlive)
                {
                    saved.IsWitchSaved = true;
                    await Clients.Group(roomId).SendAsync("DisplayMessage", $"{saved.SeatNumber}号被女巫救活");
                }
            }

            if (usePoison && targetSeat > 0)
            {
                var poisoned = game.Players.FirstOrDefault(p => p.SeatNumber == targetSeat);
                if (poisoned != null && poisoned.IsAlive)
                {
                    poisoned.IsPoisoned = true;
                    await Clients.Group(roomId).SendAsync("DisplayMessage", $"{targetSeat}号被女巫毒杀");
                }
            }

            await Clients.Caller.SendAsync("WitchResult", new { success = true });
        }

        // ============================================================
        // 13. 狼人自爆
        // ============================================================
        public async Task WerewolfExplode(string playerId)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.Role != RoleType.Werewolf || !player.IsAlive) return;

            _wolfExplode[roomId] = true;

            if (game.Phase == GamePhase.SheriffElection)
            {
                await Clients.Group(roomId).SendAsync("DisplayMessage", $"{player.SeatNumber}号狼人自爆，警徽被吞");
                game.IsSheriffElection = false;
                game.SheriffId = -1;
                _sheriffVotes.Remove(roomId, out _);
            }
            else
            {
                await Clients.Group(roomId).SendAsync("DisplayMessage", $"{player.SeatNumber}号狼人自爆");
            }

            player.IsAlive = false;
            await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

            if (await CheckGameOver(roomId)) return;

            _ = Task.Delay(2000).ContinueWith(async _ =>
            {
                await StartNight(roomId);
            });
        }

        // ============================================================
        // 14. 投票
        // ============================================================
        public async Task DayVote(string playerId, int targetSeat)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || !player.IsAlive || player.IsSpectator) return;

            if (!_dayVotes.ContainsKey(roomId)) _dayVotes[roomId] = new Dictionary<int, int>();
            _dayVotes[roomId][player.SeatNumber] = targetSeat;
        }

        // ============================================================
        // 15. 结算夜晚
        // ============================================================
        private async Task ResolveNight(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            game.Phase = GamePhase.NightResolve;
            await Clients.Group(roomId).SendAsync("PhaseUpdate", "night_resolve", game.Day, game.Night);

            var deaths = new List<int>();
            var wolfTargets = GetWerewolfTargets(roomId);

            foreach (var targetSeat in wolfTargets)
            {
                var target = game.Players.FirstOrDefault(p => p.SeatNumber == targetSeat);
                if (target != null && target.IsAlive)
                {
                    if (target.IsGuardProtected && target.GuardProtectedNight == game.Night) continue;
                    if (target.IsWitchSaved) continue;
                    deaths.Add(targetSeat);
                }
            }

            var poisoned = game.Players.FirstOrDefault(p => p.IsPoisoned && p.IsAlive);
            if (poisoned != null && !deaths.Contains(poisoned.SeatNumber)) deaths.Add(poisoned.SeatNumber);

            // 同守同救（奶穿）
            foreach (var p in game.Players.Where(p => p.IsGuardProtected && p.IsWitchSaved && p.IsAlive))
            {
                if (!deaths.Contains(p.SeatNumber)) deaths.Add(p.SeatNumber);
            }

            // 执行死亡
            var deathList = new List<string>();
            foreach (var seat in deaths)
            {
                var player = game.Players.FirstOrDefault(p => p.SeatNumber == seat);
                if (player != null && player.IsAlive)
                {
                    player.IsAlive = false;
                    deathList.Add($"{seat}号 {player.Nickname}");
                    await Clients.Group(roomId).SendAsync("PlayerDeath", new { seatNumber = seat, nickname = player.Nickname });
                }
            }

            foreach (var p in game.Players)
            {
                p.IsGuardProtected = false;
                p.IsWitchSaved = false;
                p.IsPoisoned = false;
            }

            _wolfVotes.Remove(roomId, out _);
            _guardActions.Remove(roomId, out _);
            _seerActions.Remove(roomId, out _);
            _witchActions.Remove(roomId, out _);

            await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

            game.Phase = GamePhase.DayAnnounce;
            game.Day++;
            await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_announce", game.Day, game.Night);

            if (deathList.Any())
            {
                await PlayVoiceAnnounce(roomId, $"昨晚 {string.Join("、", deathList)} 死亡");
                await Clients.Group(roomId).SendAsync("DeathAnnounce", new { deaths = deathList, message = $"昨晚 {string.Join("、", deathList)} 死亡" });
            }
            else
            {
                await PlayVoiceAnnounce(roomId, "昨晚是平安夜");
                await Clients.Group(roomId).SendAsync("DeathAnnounce", new { deaths = new List<string>(), message = "昨晚是平安夜" });
            }

            if (await CheckGameOver(roomId)) return;

            _ = Task.Delay(3000).ContinueWith(async _ =>
            {
                await NextPhase(roomId);
            });
        }

        // ============================================================
        // 16. 结算白天
        // ============================================================
        private async Task ResolveDay(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            if (!_dayVotes.ContainsKey(roomId) || _dayVotes[roomId].Count == 0)
            {
                await PlayVoiceAnnounce(roomId, "没有人投票，平安日");
                game.Phase = GamePhase.DayResolve;
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_resolve", game.Day, game.Night);

                if (await CheckGameOver(roomId)) return;
                _ = Task.Delay(3000).ContinueWith(async _ => { await StartNight(roomId); });
                return;
            }

            var voteCounts = _dayVotes[roomId].Values.GroupBy(v => v)
                .ToDictionary(g => g.Key, g => g.Count());

            var maxVotes = voteCounts.Values.Max();
            var targets = voteCounts.Where(v => v.Value == maxVotes).Select(v => v.Key).ToList();

            _dayVotes.Remove(roomId, out _);

            if (targets.Count == 1)
            {
                var eliminated = targets.First();
                var player = game.Players.FirstOrDefault(p => p.SeatNumber == eliminated);

                if (player != null && player.IsAlive)
                {
                    // 检查白痴
                    if (player.Role == RoleType.Fool && !player.IsFoolSkillUsed)
                    {
                        await Clients.Group(roomId).SendAsync("DisplayMessage", $"{eliminated}号白痴发动技能，翻牌免死");
                        player.IsFoolSkillUsed = true;
                        await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

                        if (await CheckGameOver(roomId)) return;
                        _ = Task.Delay(3000).ContinueWith(async _ => { await StartNight(roomId); });
                        return;
                    }

                    player.IsAlive = false;
                    await Clients.Group(roomId).SendAsync("DisplayMessage", $"{eliminated}号被放逐出局");
                    await Clients.Group(roomId).SendAsync("PlayerDeath", new { seatNumber = eliminated, nickname = player.Nickname });

                    // 检查猎人
                    if (player.Role == RoleType.Hunter && player.IsHunterCanShoot)
                    {
                        game.Phase = GamePhase.HunterShoot;
                        await Clients.Group(roomId).SendAsync("PhaseUpdate", "hunter_shoot", game.Day, game.Night);
                        await PlayVoiceAnnounce(roomId, "猎人发动技能，请选择开枪目标");

                        var targets2 = game.AlivePlayers.Select(p => p.SeatNumber).ToList();
                        await Clients.Client(player.ConnectionId).SendAsync("HunterShootAction", new { targets = targets2 });
                        _ = StartAutoTimer(roomId, 20, "猎人开枪");
                        return;
                    }

                    await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

                    if (await CheckGameOver(roomId)) return;
                    _ = Task.Delay(3000).ContinueWith(async _ => { await StartNight(roomId); });
                }
            }
            else
            {
                // 平票进入PK
                await Clients.Group(roomId).SendAsync("DisplayMessage", $"平票，{string.Join("、", targets)}号进行PK发言");
                game.Phase = GamePhase.DayPK;
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_pk", game.Day, game.Night);

                foreach (var seat in targets)
                {
                    var p = game.Players.FirstOrDefault(p => p.SeatNumber == seat);
                    if (p != null)
                    {
                        await Clients.Client(p.ConnectionId).SendAsync("PKAction", new { seatNumber = seat });
                    }
                }

                _ = Task.Delay(20000).ContinueWith(async _ =>
                {
                    await StartPKVoting(roomId, targets);
                });
            }
        }

        private async Task StartPKVoting(string roomId, List<int> pkTargets)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            _dayVotes[roomId] = new Dictionary<int, int>();

            foreach (var p in game.AlivePlayers)
            {
                var targets = pkTargets.Where(t => t != p.SeatNumber).ToList();
                if (targets.Any())
                {
                    await Clients.Client(p.ConnectionId).SendAsync("VotingAction", new
                    {
                        day = game.Day,
                        targets = targets,
                        isPK = true,
                        isSheriff = p.IsSheriff
                    });
                }
            }

            _ = StartAutoTimer(roomId, 20, "PK投票");
        }

        private async Task ResolvePK(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            if (!_dayVotes.ContainsKey(roomId) || _dayVotes[roomId].Count == 0)
            {
                await PlayVoiceAnnounce(roomId, "PK无人投票，平安日");
                _dayVotes.Remove(roomId, out _);
                game.Phase = GamePhase.DayResolve;
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_resolve", game.Day, game.Night);

                if (await CheckGameOver(roomId)) return;
                _ = Task.Delay(3000).ContinueWith(async _ => { await StartNight(roomId); });
                return;
            }

            var voteCounts = _dayVotes[roomId].Values.GroupBy(v => v)
                .ToDictionary(g => g.Key, g => g.Count());

            var maxVotes = voteCounts.Values.Max();
            var targets = voteCounts.Where(v => v.Value == maxVotes).Select(v => v.Key).ToList();

            _dayVotes.Remove(roomId, out _);

            if (targets.Count == 1)
            {
                var eliminated = targets.First();
                var player = game.Players.FirstOrDefault(p => p.SeatNumber == eliminated);
                if (player != null && player.IsAlive)
                {
                    player.IsAlive = false;
                    await Clients.Group(roomId).SendAsync("DisplayMessage", $"{eliminated}号被放逐出局");
                    await Clients.Group(roomId).SendAsync("PlayerDeath", new { seatNumber = eliminated, nickname = player.Nickname });
                    await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

                    if (await CheckGameOver(roomId)) return;
                    _ = Task.Delay(3000).ContinueWith(async _ => { await StartNight(roomId); });
                }
            }
            else
            {
                await PlayVoiceAnnounce(roomId, "PK再平票，平安日");
                game.Phase = GamePhase.DayResolve;
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_resolve", game.Day, game.Night);

                if (await CheckGameOver(roomId)) return;
                _ = Task.Delay(3000).ContinueWith(async _ => { await StartNight(roomId); });
            }
        }

        // ============================================================
        // 17. 猎人开枪
        // ============================================================
        public async Task HunterShoot(string playerId, int targetSeat)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.Role != RoleType.Hunter) return;

            var target = game.Players.FirstOrDefault(p => p.SeatNumber == targetSeat);
            if (target == null || !target.IsAlive) return;

            target.IsAlive = false;
            await Clients.Group(roomId).SendAsync("DisplayMessage", $"猎人开枪，{targetSeat}号死亡");
            await Clients.Group(roomId).SendAsync("PlayerDeath", new { seatNumber = targetSeat, nickname = target.Nickname });
            await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

            if (await CheckGameOver(roomId)) return;

            _ = Task.Delay(3000).ContinueWith(async _ =>
            {
                await StartNight(roomId);
            });
        }

        private async Task ResolveHunterShoot(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;
            game.Phase = GamePhase.DayResolve;

            if (await CheckGameOver(roomId)) return;
            _ = Task.Delay(3000).ContinueWith(async _ =>
            {
                await StartNight(roomId);
            });
        }

        // ============================================================
        // 18. 检查游戏结束
        // ============================================================
        private async Task<bool> CheckGameOver(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return true;

            var alivePlayers = game.AlivePlayers;
            var wolves = game.Werewolves;

            // 狼人全部出局
            if (!wolves.Any())
            {
                game.Phase = GamePhase.GameOver;
                game.IsGameOver = true;
                game.Winner = "好人";
                await Clients.Group(roomId).SendAsync("GameOver", "好人");
                await PlayVoiceAnnounce(roomId, "好人阵营获胜！");
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "gameover", game.Day, game.Night);
                return true;
            }

            // 好人全部出局
            if (!game.GoodPlayers.Any())
            {
                game.Phase = GamePhase.GameOver;
                game.IsGameOver = true;
                game.Winner = "狼人";
                await Clients.Group(roomId).SendAsync("GameOver", "狼人");
                await PlayVoiceAnnounce(roomId, "狼人阵营获胜！");
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "gameover", game.Day, game.Night);
                return true;
            }

            // 神职全灭（屠神）
            if (!game.Gods.Any())
            {
                game.Phase = GamePhase.GameOver;
                game.IsGameOver = true;
                game.Winner = "狼人";
                await Clients.Group(roomId).SendAsync("GameOver", "狼人");
                await PlayVoiceAnnounce(roomId, "所有神职已出局，狼人获胜！");
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "gameover", game.Day, game.Night);
                return true;
            }

            // 平民全灭（屠民）
            if (!game.Villagers.Any())
            {
                game.Phase = GamePhase.GameOver;
                game.IsGameOver = true;
                game.Winner = "狼人";
                await Clients.Group(roomId).SendAsync("GameOver", "狼人");
                await PlayVoiceAnnounce(roomId, "所有平民已出局，狼人获胜！");
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "gameover", game.Day, game.Night);
                return true;
            }

            return false;
        }

        // ============================================================
        // 19. 房主控制
        // ============================================================
        public async Task JoinHostControl(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId + "_host");
        }

        public async Task PauseGame(string roomId)
        {
            _isPaused[roomId] = true;
            await Clients.Group(roomId).SendAsync("DisplayMessage", "⏸️ 游戏已暂停");
        }

        public async Task ResumeGame(string roomId)
        {
            _isPaused[roomId] = false;
            await Clients.Group(roomId).SendAsync("DisplayMessage", "▶️ 游戏继续");
        }

        public async Task SetSpeed(string roomId, double speed)
        {
            _speedMultiplier[roomId] = Math.Max(0.5, Math.Min(3.0, speed));
            await Clients.Group(roomId).SendAsync("DisplayMessage", $"⚡ 速度 x{_speedMultiplier[roomId]:F1}");
        }

        public async Task SkipPhase(string roomId)
        {
            await NextPhase(roomId);
        }

        public async Task<List<object>> GetAllRoles(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return new List<object>();

            return game.Players.Where(p => !p.IsSpectator).Select(p => new
            {
                p.SeatNumber,
                p.Nickname,
                p.Role
            }).Cast<object>().ToList();
        }

        public async Task EndGame(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            game.Phase = GamePhase.GameOver;
            game.IsGameOver = true;
            await Clients.Group(roomId).SendAsync("GameOver", "游戏已结束");
            await Clients.Group(roomId).SendAsync("PhaseUpdate", "gameover", game.Day, game.Night);
            await PlayVoiceAnnounce(roomId, "游戏已结束");
            _games.TryRemove(roomId, out _);
        }

        // ============================================================
        // 20. 断开连接
        // ============================================================
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectionToPlayer.TryRemove(Context.ConnectionId, out var playerId))
            {
                if (_playerToRoom.TryRemove(playerId, out var roomId))
                {
                    if (_games.TryGetValue(roomId, out var game))
                    {
                        var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
                        if (player != null)
                        {
                            player.IsOnline = false;
                            await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);
                        }
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ============================================================
        // 21. 辅助方法
        // ============================================================
        private int GetTotalSeats(WerewolfGameState game)
        {
            var count = game.PlayerCount;
            return count >= 8 ? count : 8;
        }

        private List<int> GetWerewolfTargets(string roomId)
        {
            if (_wolfVotes.TryGetValue(roomId, out var votes))
            {
                if (votes.TryGetValue("_result", out var target))
                {
                    return new List<int> { target };
                }
            }
            return new List<int>();
        }

        private List<RoleType> GenerateRoles(int playerCount)
        {
            var roles = new List<RoleType>();

            var wolfCount = playerCount <= 8 ? 2 : (playerCount <= 10 ? 3 : 4);
            for (int i = 0; i < wolfCount; i++) roles.Add(RoleType.Werewolf);

            var godRoles = new List<RoleType> { RoleType.Seer, RoleType.Witch, RoleType.Guard };
            if (playerCount >= 10) godRoles.Add(RoleType.Hunter);
            if (playerCount >= 12) godRoles.Add(RoleType.Fool);
            if (playerCount >= 12) godRoles.Add(RoleType.Knight);

            foreach (var r in godRoles) roles.Add(r);

            while (roles.Count < playerCount) roles.Add(RoleType.Villager);

            return roles;
        }

        private List<T> ShuffleList<T>(List<T> list)
        {
            var arr = list.ToList();
            var rand = new Random();
            for (int i = arr.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            return arr;
        }

        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            var parts = new string[2];
            for (int i = 0; i < 2; i++)
            {
                char[] part = new char[4];
                for (int j = 0; j < 4; j++)
                {
                    part[j] = chars[random.Next(chars.Length)];
                }
                parts[i] = new string(part);
            }
            return $"{parts[0]}-{parts[1]}";
        }

        private async Task PlayVoiceAnnounce(string roomId, string message)
        {
            await Clients.Group(roomId).SendAsync("VoiceAnnounce", message, "normal");
            await Clients.Group(roomId).SendAsync("DisplayMessage", message);
        }

        // ============================================================
        // 22. 获取房间信息（供Controller调用）
        // ============================================================
        public static WerewolfGameState? GetGame(string roomId)
        {
            _games.TryGetValue(roomId, out var game);
            return game;
        }

        public static List<WerewolfGameState> GetAllGames()
        {
            return _games.Values.ToList();
        }
    }
}
