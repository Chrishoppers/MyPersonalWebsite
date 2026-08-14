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

        // ============================================================
        // 1-4. 创建房间、加入、准备、发牌（已在第一批）
        // ============================================================
       

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

        // ============================================================
        // 5. 警长竞选
        // ============================================================
        public async Task StartSheriffElection(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;
            if (game.PlayerCount < 10) return;
            if (game.Day > 0 || game.IsSheriffElection) return;

            game.IsSheriffElection = true;
            game.Phase = GamePhase.SheriffElection;
            _sheriffVotes[roomId] = new Dictionary<int, int>();

            await Clients.Group(roomId).SendAsync("PhaseUpdate", "sheriff_election", game.Day, game.Night);
            await Clients.Group(roomId).SendAsync("VoiceAnnounce", "警长竞选开始，请参与竞选的玩家举手", "high");

            // 发送给所有玩家竞选信息
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
                await Clients.Group(roomId).SendAsync("VoiceAnnounce", "竞选发言结束，请警下玩家投票", "high");

                // 通知所有玩家投票
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

        // 玩家参与竞选
        public async Task JoinSheriffElection(string playerId)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || !player.IsAlive || player.IsSpectator) return;

            if (!_sheriffVotes.ContainsKey(roomId))
                _sheriffVotes[roomId] = new Dictionary<int, int>();

            if (!_sheriffVotes[roomId].ContainsKey(player.SeatNumber))
            {
                _sheriffVotes[roomId][player.SeatNumber] = 0;
                await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{player.SeatNumber}号玩家参与竞选", "normal");
                await Clients.Group(roomId).SendAsync("SheriffCandidateUpdate", _sheriffVotes[roomId].Keys.ToList());
            }
        }

        // 玩家投票给警长候选人
        public async Task SheriffVote(string playerId, int targetSeat)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var voter = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (voter == null || !voter.IsAlive || voter.IsSpectator) return;

            // 不能投自己
            if (voter.SeatNumber == targetSeat) return;

            // 不能投给非候选人
            if (!_sheriffVotes.ContainsKey(roomId) || !_sheriffVotes[roomId].ContainsKey(targetSeat)) return;

            // 记录投票
            _sheriffVotes[roomId][targetSeat] = _sheriffVotes[roomId].GetValueOrDefault(targetSeat) + 1;

            await Clients.Client(voter.ConnectionId).SendAsync("SheriffVoteConfirm", new { targetSeat });
            await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{voter.SeatNumber}号已投票", "normal");
        }

        private async Task ResolveSheriffElection(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            if (!_sheriffVotes.ContainsKey(roomId) || _sheriffVotes[roomId].Count == 0)
            {
                // 无人竞选
                await Clients.Group(roomId).SendAsync("VoiceAnnounce", "无人竞选警长，本局无警长", "high");
                game.IsSheriffElection = false;
                await StartNight(roomId);
                return;
            }

            // 统计票数
            var votes = _sheriffVotes[roomId];
            var maxVotes = votes.Values.Max();
            var winners = votes.Where(v => v.Value == maxVotes).Select(v => v.Key).ToList();

            if (winners.Count == 1)
            {
                // 当选
                var sheriffSeat = winners.First();
                var sheriff = game.GetPlayer(sheriffSeat);
                if (sheriff != null)
                {
                    sheriff.IsSheriff = true;
                    game.SheriffId = sheriffSeat;
                    await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{sheriffSeat}号当选警长", "high");
                    await Clients.Group(roomId).SendAsync("SheriffElected", new
                    {
                        seatNumber = sheriffSeat,
                        nickname = sheriff.Nickname
                    });
                }
            }
            else
            {
                // 平票，无人当选
                await Clients.Group(roomId).SendAsync("VoiceAnnounce", "平票，无人当选警长", "high");
            }

            game.IsSheriffElection = false;
            _sheriffVotes.Remove(roomId, out _);

            // 进入夜晚
            await StartNight(roomId);
        }

        // ============================================================
        // 6. 夜晚流程
        // ============================================================
        public async Task StartNight(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            game.Night++;
            _wolfVotes[roomId] = new Dictionary<int, int>();
            _wolfExplode[roomId] = false;

            // 守卫行动
            game.Phase = GamePhase.NightGuard;
            await Clients.Group(roomId).SendAsync("PhaseUpdate", "night_guard", game.Day, game.Night);
            await PlayVoiceAnnounce(roomId, $"第{game.Night}夜，守卫请睁眼", "high");

            var guard = game.Players.FirstOrDefault(p => p.Role == RoleType.Guard && p.IsAlive && !p.IsSpectator);
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

            // 30秒后自动进入下一步
            _ = Task.Delay(30000).ContinueWith(async _ =>
            {
                await NextPhase(roomId);
            });
        }

        // ============================================================
        // 7. 下一步（主控端点击）
        // ============================================================
        public async Task NextPhase(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            switch (game.Phase)
            {
                case GamePhase.NightGuard:
                    game.Phase = GamePhase.NightSeer;
                    await Clients.Group(roomId).SendAsync("PhaseUpdate", "night_seer", game.Day, game.Night);
                    await PlayVoiceAnnounce(roomId, "预言家请睁眼", "high");

                    var seer = game.Players.FirstOrDefault(p => p.Role == RoleType.Seer && p.IsAlive && !p.IsSpectator);
                    if (seer != null)
                    {
                        var targets = game.AlivePlayers.Select(p => p.SeatNumber).ToList();
                        await Clients.Client(seer.ConnectionId).SendAsync("SeerAction", new
                        {
                            night = game.Night,
                            targets = targets
                        });
                    }
                    _ = Task.Delay(20000).ContinueWith(async _ => { await NextPhase(roomId); });
                    break;

                case GamePhase.NightSeer:
                    game.Phase = GamePhase.NightWerewolf;
                    await Clients.Group(roomId).SendAsync("PhaseUpdate", "night_werewolf", game.Day, game.Night);
                    await PlayVoiceAnnounce(roomId, "狼人请睁眼", "high");

                    var wolves = game.Players.Where(p => p.Role == RoleType.Werewolf && p.IsAlive && !p.IsSpectator).ToList();
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
                    _ = Task.Delay(30000).ContinueWith(async _ => { await NextPhase(roomId); });
                    break;

                case GamePhase.NightWerewolf:
                    game.Phase = GamePhase.NightWitch;
                    await Clients.Group(roomId).SendAsync("PhaseUpdate", "night_witch", game.Day, game.Night);
                    await PlayVoiceAnnounce(roomId, "女巫请睁眼", "high");

                    var witch = game.Players.FirstOrDefault(p => p.Role == RoleType.Witch && p.IsAlive && !p.IsSpectator);
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
                    _ = Task.Delay(20000).ContinueWith(async _ => { await NextPhase(roomId); });
                    break;

                case GamePhase.NightWitch:
                    await ResolveNight(roomId);
                    break;

                case GamePhase.DayAnnounce:
                    game.Phase = GamePhase.DaySpeech;
                    game.Day++;
                    await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_speech", game.Day, game.Night);
                    await PlayVoiceAnnounce(roomId, $"第{game.Day}天，请开始发言", "high");

                    // 检查是否游戏结束
                    if (await CheckGameOver(roomId)) return;

                    // 30秒后进入投票
                    _ = Task.Delay(30000).ContinueWith(async _ =>
                    {
                        await NextPhase(roomId);
                    });
                    break;

                case GamePhase.DaySpeech:
                    game.Phase = GamePhase.DayVoting;
                    await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_voting", game.Day, game.Night);
                    await PlayVoiceAnnounce(roomId, "开始投票，请选择要放逐的玩家", "high");
                    await StartVoting(roomId);
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
                    await Clients.Caller.SendAsync("Toast", "当前阶段无法推进", "warning");
                    break;
            }
        }

        // ============================================================
        // 8. 玩家操作 - 守卫守护
        // ============================================================
        public async Task GuardProtect(string playerId, int targetSeat)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.Role != RoleType.Guard) return;

            var target = game.GetPlayer(targetSeat);
            if (target == null || !target.IsAlive) return;

            // 不能连续守护同一人
            if (target.GuardProtectedNight == game.Night - 1)
            {
                await Clients.Caller.SendAsync("GuardResult", new { success = false, message = "不能连续守护同一个人" });
                return;
            }

            // 第一天可以守护自己
            if (targetSeat == player.SeatNumber && game.Night != 1)
            {
                await Clients.Caller.SendAsync("GuardResult", new { success = false, message = "除了第一天外不能守护自己" });
                return;
            }

            target.IsGuardProtected = true;
            target.GuardProtectedNight = game.Night;

            await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{targetSeat}号已被守卫守护", "normal");
            await Clients.Caller.SendAsync("GuardResult", new { success = true, targetSeat });
        }

        // ============================================================
        // 9. 玩家操作 - 预言家查验
        // ============================================================
        public async Task SeerCheck(string playerId, int targetSeat)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.Role != RoleType.Seer) return;

            var target = game.GetPlayer(targetSeat);
            if (target == null || !target.IsAlive) return;

            var isWerewolf = target.Role == RoleType.Werewolf;
            await Clients.Client(player.ConnectionId).SendAsync("SeerResult", new
            {
                targetSeat = targetSeat,
                isWerewolf = isWerewolf,
                result = isWerewolf ? "🐺 狼人" : "⭐ 好人"
            });

            await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{targetSeat}号查验完成", "normal");
        }

        // ============================================================
        // 10. 玩家操作 - 狼人投票
        // ============================================================
        public async Task WerewolfVote(string playerId, int targetSeat)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.Role != RoleType.Werewolf || !player.IsAlive) return;

            // 检查是否已自爆
            if (_wolfExplode.TryGetValue(roomId, out var exploded) && exploded) return;

            if (!_wolfVotes.ContainsKey(roomId))
                _wolfVotes[roomId] = new Dictionary<int, int>();

            _wolfVotes[roomId][player.SeatNumber] = targetSeat;

            var wolves = game.Werewolves;
            var voted = _wolfVotes[roomId].Keys.Count;

            // 通知所有狼人投票进度
            var voteStatus = new { voted, total = wolves.Count };
            foreach (var w in wolves)
            {
                await Clients.Client(w.ConnectionId).SendAsync("WolfVoteStatus", voteStatus);
            }

            if (voted >= wolves.Count)
            {
                // 统计票数
                var voteCounts = _wolfVotes[roomId].Values.GroupBy(v => v)
                    .ToDictionary(g => g.Key, g => g.Count());

                var maxVotes = voteCounts.Values.Max();
                var targets = voteCounts.Where(v => v.Value == maxVotes).Select(v => v.Key).ToList();

                int selectedTarget;
                if (targets.Count == 1)
                {
                    selectedTarget = targets.First();
                }
                else
                {
                    selectedTarget = targets[new Random().Next(targets.Count)];
                }

                _wolfVotes[roomId]["_result"] = selectedTarget;

                await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"狼人选择了 {selectedTarget} 号", "normal");

                // 自动进入女巫阶段
                await NextPhase(roomId);
            }
        }

        // ============================================================
        // 11. 玩家操作 - 女巫行动
        // ============================================================
        public async Task WitchAction(string playerId, bool useAntidote, bool usePoison, int targetSeat = -1)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.Role != RoleType.Witch) return;

            var wolfTargets = GetWerewolfTargets(roomId);

            if (useAntidote && wolfTargets.Any())
            {
                var wolfTarget = wolfTargets.First();
                var saved = game.GetPlayer(wolfTarget);
                if (saved != null && saved.IsAlive)
                {
                    saved.IsWitchSaved = true;
                    await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{wolfTarget}号被女巫救活", "normal");
                }
            }

            if (usePoison && targetSeat > 0)
            {
                var poisoned = game.GetPlayer(targetSeat);
                if (poisoned != null && poisoned.IsAlive)
                {
                    poisoned.IsPoisoned = true;
                    await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{targetSeat}号被女巫毒杀", "normal");
                }
            }

            await Clients.Caller.SendAsync("WitchResult", new { success = true });
        }

        // ============================================================
        // 12. 狼人自爆
        // ============================================================
        public async Task WerewolfExplode(string playerId)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.Role != RoleType.Werewolf || !player.IsAlive) return;

            _wolfExplode[roomId] = true;

            // 如果是警长竞选阶段，吞警徽
            if (game.Phase == GamePhase.SheriffElection)
            {
                await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{player.SeatNumber}号狼人自爆，警徽被吞，本局无警长", "high");
                game.IsSheriffElection = false;
                game.SheriffId = -1;
                _sheriffVotes.Remove(roomId, out _);
            }
            else
            {
                await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{player.SeatNumber}号狼人自爆", "high");
            }

            // 狼人死亡
            player.IsAlive = false;
            await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

            // 狼人自爆后直接进入黑夜
            if (await CheckGameOver(roomId)) return;

            _ = Task.Delay(2000).ContinueWith(async _ =>
            {
                await StartNight(roomId);
            });
        }

        // ============================================================
        // 13. 玩家操作 - 投票放逐
        // ============================================================
        public async Task DayVote(string playerId, int targetSeat)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || !player.IsAlive || player.IsSpectator) return;

            if (!_dayVotes.ContainsKey(roomId))
                _dayVotes[roomId] = new Dictionary<int, int>();

            _dayVotes[roomId][player.SeatNumber] = targetSeat;

            var alivePlayers = game.AlivePlayers;
            var voted = _dayVotes[roomId].Keys.Count;

            if (voted >= alivePlayers.Count)
            {
                await ResolveDay(roomId);
            }
        }

        // ============================================================
        // 14. 结算夜晚
        // ============================================================
        private async Task ResolveNight(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            game.Phase = GamePhase.NightResolve;
            await Clients.Group(roomId).SendAsync("PhaseUpdate", "night_resolve", game.Day, game.Night);

            var deaths = new List<int>();

            // 1. 狼人刀人
            var wolfTargets = GetWerewolfTargets(roomId);
            foreach (var targetSeat in wolfTargets)
            {
                var target = game.GetPlayer(targetSeat);
                if (target != null && target.IsAlive)
                {
                    // 检查守卫守护
                    if (target.IsGuardProtected && target.GuardProtectedNight == game.Night)
                    {
                        await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{targetSeat}号被守卫守护，狼人袭击无效", "normal");
                        continue;
                    }
                    // 检查女巫解救
                    if (target.IsWitchSaved)
                    {
                        await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{targetSeat}号被女巫救活", "normal");
                        continue;
                    }
                    deaths.Add(targetSeat);
                }
            }

            // 2. 女巫毒药
            var poisoned = game.Players.FirstOrDefault(p => p.IsPoisoned && p.IsAlive);
            if (poisoned != null && !deaths.Contains(poisoned.SeatNumber))
            {
                deaths.Add(poisoned.SeatNumber);
            }

            // 3. 同守同救（奶穿）
            var guardAndSave = game.Players.Where(p => p.IsGuardProtected && p.IsWitchSaved && p.IsAlive).ToList();
            foreach (var p in guardAndSave)
            {
                if (!deaths.Contains(p.SeatNumber))
                {
                    deaths.Add(p.SeatNumber);
                    await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{p.SeatNumber}号同守同救（奶穿）死亡", "high");
                }
            }

            // 4. 执行死亡
            var deathList = new List<string>();
            foreach (var seat in deaths)
            {
                var player = game.GetPlayer(seat);
                if (player != null && player.IsAlive)
                {
                    player.IsAlive = false;
                    deathList.Add($"{seat}号 {player.Nickname}");
                    await Clients.Group(roomId).SendAsync("PlayerDeath", new { seatNumber = seat, nickname = player.Nickname });
                }
            }

            // 清理状态
            foreach (var p in game.Players)
            {
                p.IsGuardProtected = false;
                p.IsWitchSaved = false;
                p.IsPoisoned = false;
            }

            _wolfVotes.Remove(roomId, out _);
            await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

            // 进入白天
            game.Phase = GamePhase.DayAnnounce;
            game.Day++;
            await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_announce", game.Day, game.Night);

            if (deathList.Any())
            {
                var msg = $"昨晚 {string.Join("、", deathList)} 死亡";
                await PlayVoiceAnnounce(roomId, msg, "high");
                await Clients.Group(roomId).SendAsync("DeathAnnounce", new
                {
                    deaths = deathList,
                    message = msg
                });

                // 第一天有遗言
                if (game.Night == 1)
                {
                    await PlayVoiceAnnounce(roomId, "第一天死亡玩家有遗言", "normal");
                }
            }
            else
            {
                await PlayVoiceAnnounce(roomId, "昨晚是平安夜", "high");
                await Clients.Group(roomId).SendAsync("DeathAnnounce", new
                {
                    deaths = new List<string>(),
                    message = "昨晚是平安夜，没有人死亡"
                });
            }

            // 检查游戏结束
            if (await CheckGameOver(roomId)) return;

            // 进入白天发言
            _ = Task.Delay(3000).ContinueWith(async _ =>
            {
                await NextPhase(roomId);
            });
        }

        // ============================================================
        // 15. 开始投票
        // ============================================================
        private async Task StartVoting(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            _dayVotes[roomId] = new Dictionary<int, int>();

            var alivePlayers = game.AlivePlayers;
            var targets = alivePlayers.Select(p => p.SeatNumber).ToList();

            foreach (var p in alivePlayers)
            {
                var targetList = targets.Where(t => t != p.SeatNumber).ToList();
                await Clients.Client(p.ConnectionId).SendAsync("VotingAction", new
                {
                    day = game.Day,
                    targets = targetList,
                    isSheriff = p.IsSheriff
                });
            }

            // 60秒后自动结算
            _ = Task.Delay(60000).ContinueWith(async _ =>
            {
                await ResolveDay(roomId);
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
                await Clients.Group(roomId).SendAsync("VoiceAnnounce", "没有人投票，平安日", "high");
                game.Phase = GamePhase.DayResolve;
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_resolve", game.Day, game.Night);

                if (await CheckGameOver(roomId)) return;

                _ = Task.Delay(3000).ContinueWith(async _ =>
                {
                    await StartNight(roomId);
                });
                return;
            }

            var voteCounts = _dayVotes[roomId].Values.GroupBy(v => v)
                .ToDictionary(g => g.Key, g => g.Count());

            var maxVotes = voteCounts.Values.Max();
            var targets = voteCounts.Where(v => v.Value == maxVotes).Select(v => v.Key).ToList();

            _dayVotes.Remove(roomId, out _);

            if (targets.Count == 1)
            {
                // 一人得票最高，被放逐
                var eliminated = targets.First();
                var player = game.GetPlayer(eliminated);
                if (player != null && player.IsAlive)
                {
                    // 检查是否是白痴
                    if (player.Role == RoleType.Fool && !player.IsFoolSkillUsed)
                    {
                        await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{eliminated}号白痴发动技能，翻牌免死", "high");
                        player.IsFoolSkillUsed = true;
                        player.IsAlive = true;
                        await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

                        // 白痴翻牌后继续游戏
                        await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_resolve", game.Day, game.Night);
                        if (await CheckGameOver(roomId)) return;

                        _ = Task.Delay(3000).ContinueWith(async _ =>
                        {
                            await StartNight(roomId);
                        });
                        return;
                    }

                    player.IsAlive = false;
                    await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{eliminated}号被放逐出局", "high");
                    await Clients.Group(roomId).SendAsync("PlayerDeath", new { seatNumber = eliminated, nickname = player.Nickname });

                    // 检查猎人
                    if (player.Role == RoleType.Hunter && player.IsHunterCanShoot)
                    {
                        await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{eliminated}号猎人发动技能", "high");
                        game.Phase = GamePhase.HunterShoot;
                        await Clients.Group(roomId).SendAsync("PhaseUpdate", "hunter_shoot", game.Day, game.Night);

                        // 通知猎人选择目标
                        var targets2 = game.AlivePlayers.Select(p => p.SeatNumber).ToList();
                        await Clients.Client(player.ConnectionId).SendAsync("HunterShootAction", new
                        {
                            targets = targets2
                        });

                        // 30秒后自动结算猎人开枪
                        _ = Task.Delay(30000).ContinueWith(async _ =>
                        {
                            await ResolveHunterShoot(roomId);
                        });
                        return;
                    }

                    await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

                    if (await CheckGameOver(roomId)) return;

                    _ = Task.Delay(3000).ContinueWith(async _ =>
                    {
                        await StartNight(roomId);
                    });
                }
            }
            else
            {
                // 平票，进入PK
                await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"平票，{string.Join("、", targets)}号进行PK发言", "high");
                game.Phase = GamePhase.DayPK;
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_pk", game.Day, game.Night);

                // 通知PK玩家发言
                foreach (var seat in targets)
                {
                    var p = game.GetPlayer(seat);
                    if (p != null)
                    {
                        await Clients.Client(p.ConnectionId).SendAsync("PKAction", new { seatNumber = seat });
                    }
                }

                // 30秒后重新投票
                _ = Task.Delay(30000).ContinueWith(async _ =>
                {
                    await StartPKVoting(roomId, targets);
                });
            }
        }

        // ============================================================
        // 17. PK投票
        // ============================================================
        private async Task StartPKVoting(string roomId, List<int> pkTargets)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            _dayVotes[roomId] = new Dictionary<int, int>();

            var alivePlayers = game.AlivePlayers;

            foreach (var p in alivePlayers)
            {
                var targetList = pkTargets.Where(t => t != p.SeatNumber).ToList();
                if (targetList.Any())
                {
                    await Clients.Client(p.ConnectionId).SendAsync("VotingAction", new
                    {
                        day = game.Day,
                        targets = targetList,
                        isPK = true,
                        isSheriff = p.IsSheriff
                    });
                }
            }

            // 30秒后结算PK
            _ = Task.Delay(30000).ContinueWith(async _ =>
            {
                await ResolvePK(roomId);
            });
        }

        private async Task ResolvePK(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            if (!_dayVotes.ContainsKey(roomId) || _dayVotes[roomId].Count == 0)
            {
                await Clients.Group(roomId).SendAsync("VoiceAnnounce", "PK无人投票，平安日", "high");
                _dayVotes.Remove(roomId, out _);
                game.Phase = GamePhase.DayResolve;
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_resolve", game.Day, game.Night);

                if (await CheckGameOver(roomId)) return;

                _ = Task.Delay(3000).ContinueWith(async _ =>
                {
                    await StartNight(roomId);
                });
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
                var player = game.GetPlayer(eliminated);
                if (player != null && player.IsAlive)
                {
                    player.IsAlive = false;
                    await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{eliminated}号被放逐出局", "high");
                    await Clients.Group(roomId).SendAsync("PlayerDeath", new { seatNumber = eliminated, nickname = player.Nickname });
                    await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

                    if (await CheckGameOver(roomId)) return;

                    _ = Task.Delay(3000).ContinueWith(async _ =>
                    {
                        await StartNight(roomId);
                    });
                }
            }
            else
            {
                // PK后再平票，平安日
                await Clients.Group(roomId).SendAsync("VoiceAnnounce", "PK再平票，平安日", "high");
                game.Phase = GamePhase.DayResolve;
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_resolve", game.Day, game.Night);

                if (await CheckGameOver(roomId)) return;

                _ = Task.Delay(3000).ContinueWith(async _ =>
                {
                    await StartNight(roomId);
                });
            }
        }

        // ============================================================
        // 18. 猎人开枪
        // ============================================================
        public async Task HunterShoot(string playerId, int targetSeat)
        {
            if (!_playerToRoom.TryGetValue(playerId, out var roomId)) return;
            if (!_games.TryGetValue(roomId, out var game)) return;

            var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.Role != RoleType.Hunter) return;

            var target = game.GetPlayer(targetSeat);
            if (target == null || !target.IsAlive) return;

            target.IsAlive = false;
            await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"猎人开枪，{targetSeat}号死亡", "high");
            await Clients.Group(roomId).SendAsync("PlayerDeath", new { seatNumber = targetSeat, nickname = target.Nickname });
            await Clients.Group(roomId).SendAsync("PlayerListUpdate", game.Players);

            _hunterShoot[roomId] = targetSeat;

            if (await CheckGameOver(roomId)) return;

            _ = Task.Delay(3000).ContinueWith(async _ =>
            {
                await StartNight(roomId);
            });
        }

        private async Task ResolveHunterShoot(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            // 如果猎人没有开枪，自动进入夜晚
            if (!_hunterShoot.ContainsKey(roomId))
            {
                await Clients.Group(roomId).SendAsync("VoiceAnnounce", "猎人未开枪", "normal");
            }

            _hunterShoot.Remove(roomId, out _);
            game.Phase = GamePhase.DayResolve;
            await Clients.Group(roomId).SendAsync("PhaseUpdate", "day_resolve", game.Day, game.Night);

            if (await CheckGameOver(roomId)) return;

            _ = Task.Delay(3000).ContinueWith(async _ =>
            {
                await StartNight(roomId);
            });
        }

        // ============================================================
        // 19. 检查游戏结束
        // ============================================================
        private async Task<bool> CheckGameOver(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return true;

            var alivePlayers = game.AlivePlayers;
            var wolves = game.Werewolves;
            var goods = game.GoodPlayers;

            // 狼人全部出局 → 好人胜利
            if (!wolves.Any())
            {
                game.Phase = GamePhase.GameOver;
                game.IsGameOver = true;
                game.Winner = "好人";
                await Clients.Group(roomId).SendAsync("GameOver", "好人");
                await PlayVoiceAnnounce(roomId, "好人阵营获胜！", "high");
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "gameover", game.Day, game.Night);
                return true;
            }

            // 好人全部出局 → 狼人胜利
            if (!goods.Any())
            {
                game.Phase = GamePhase.GameOver;
                game.IsGameOver = true;
                game.Winner = "狼人";
                await Clients.Group(roomId).SendAsync("GameOver", "狼人");
                await PlayVoiceAnnounce(roomId, "狼人阵营获胜！", "high");
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "gameover", game.Day, game.Night);
                return true;
            }

            // 神职全灭 → 狼人胜利（屠神）
            var gods = game.Gods;
            if (!gods.Any())
            {
                game.Phase = GamePhase.GameOver;
                game.IsGameOver = true;
                game.Winner = "狼人";
                await Clients.Group(roomId).SendAsync("GameOver", "狼人");
                await PlayVoiceAnnounce(roomId, "所有神职已出局，狼人获胜！", "high");
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "gameover", game.Day, game.Night);
                return true;
            }

            // 平民全灭 → 狼人胜利（屠民）
            var villagers = game.Villagers;
            if (!villagers.Any())
            {
                game.Phase = GamePhase.GameOver;
                game.IsGameOver = true;
                game.Winner = "狼人";
                await Clients.Group(roomId).SendAsync("GameOver", "狼人");
                await PlayVoiceAnnounce(roomId, "所有平民已出局，狼人获胜！", "high");
                await Clients.Group(roomId).SendAsync("PhaseUpdate", "gameover", game.Day, game.Night);
                return true;
            }

            return false;
        }

        // ============================================================
        // 20. 辅助方法
        // ============================================================

        private int GetTotalSeats(WerewolfGameState game)
        {
            var count = game.Players.Count(p => !p.IsSpectator);
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

        private List<RoleType> GenerateRoles(int playerCount, WerewolfGameState game)
        {
            var roles = new List<RoleType>();

            // 狼人数量
            var wolfCount = playerCount <= 8 ? 2 : (playerCount <= 10 ? 3 : 4);
            for (int i = 0; i < wolfCount; i++) roles.Add(RoleType.Werewolf);

            // 神职
            var godRoles = new List<RoleType> { RoleType.Seer, RoleType.Witch, RoleType.Guard };
            if (playerCount >= 10) godRoles.Add(RoleType.Hunter);
            if (playerCount >= 12) godRoles.Add(RoleType.Fool);
            // 骑士可选
            if (playerCount >= 12) godRoles.Add(RoleType.Knight);

            foreach (var r in godRoles) roles.Add(r);

            // 平民
            while (roles.Count < playerCount) roles.Add(RoleType.Villager);

            return roles;
        }

        private List<T> ShuffleArray<T>(List<T> array)
        {
            var arr = array.ToList();
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

        private async Task PlayVoiceAnnounce(string roomId, string message, string importance)
        {
            // 只发送到大屏（主控端）
            await Clients.Group(roomId).SendAsync("VoiceAnnounce", message, importance);
            // 同时在大屏上显示文字
            await Clients.Group(roomId).SendAsync("DisplayMessage", message);
        }

        // ============================================================
        // 21. 结束游戏
        // ============================================================
        public async Task EndGame(string roomId)
        {
            if (!_games.TryGetValue(roomId, out var game)) return;

            game.Phase = GamePhase.GameOver;
            game.IsGameOver = true;
            await Clients.Group(roomId).SendAsync("GameOver", "游戏已结束");
            await Clients.Group(roomId).SendAsync("PhaseUpdate", "gameover", game.Day, game.Night);
            _games.TryRemove(roomId, out _);
        }

        // ============================================================
        // 22. 断开连接
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
                            await Clients.Group(roomId).SendAsync("VoiceAnnounce", $"{player.Nickname} 已断开连接", "normal");
                        }
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ============================================================
        // 23. 获取房间信息
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
