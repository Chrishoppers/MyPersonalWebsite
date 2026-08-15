using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Models;
using System.Collections.Concurrent;

namespace MyPersonalWebsite.Hubs
{
    public class PartyHub : Hub
    {
        private static readonly ConcurrentDictionary<string, PartyRoom> _rooms = new();
        private static readonly ConcurrentDictionary<string, string> _playerToRoom = new();
        private static readonly ConcurrentDictionary<string, string> _connectionToPlayer = new();

        // ============================================================
        // 创建房间
        // ============================================================
        public async Task<object> CreateRoom(string hostName, int maxPlayers = 20, string? password = null)
        {
            var roomId = GenerateRoomCode();
            var playerId = $"host_{Guid.NewGuid():N}";

            var room = new PartyRoom
            {
                RoomId = roomId,
                HostUserId = playerId,
                HostName = hostName,
                MaxPlayers = maxPlayers,
                MinPlayers = 2,
                CreatedAt = DateTime.Now,
                Status = "waiting",
                Password = password,
                IsPublic = string.IsNullOrEmpty(password)
            };

            room.Players.Add(new PartyPlayer
            {
                PlayerId = playerId,
                Nickname = $"{hostName} 👑",
                AvatarEmoji = "👑",
                IsReady = true,
                IsAdmin = true,
                IsHost = true,
                JoinedAt = DateTime.Now,
                ConnectionId = Context.ConnectionId,
                Status = "online"
            });

            room.AdminUserIds.Add(playerId);

            _rooms[roomId] = room;
            _playerToRoom[playerId] = roomId;
            _connectionToPlayer[Context.ConnectionId] = playerId;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("RoomCreated", new { success = true, room });

            return new { success = true, roomId, playerId };
        }

        // ============================================================
        // 加入房间（玩家扫码）
        // ============================================================
        public async Task<object> JoinRoom(string roomId, string nickname, string avatarEmoji = "🧑")
        {
            if (!_rooms.TryGetValue(roomId, out var room))
                return new { success = false, message = "房间不存在" };

            if (room.Status != "waiting" && room.Status != "ready")
                return new { success = false, message = "游戏已开始或已结束" };

            if (room.Players.Count >= room.MaxPlayers)
                return new { success = false, message = "房间已满" };

            if (room.Players.Any(p => p.Nickname == nickname))
                return new { success = false, message = "昵称已被使用" };

            var playerId = $"player_{Guid.NewGuid():N}";

            var player = new PartyPlayer
            {
                PlayerId = playerId,
                Nickname = nickname,
                AvatarEmoji = avatarEmoji,
                IsReady = false,
                IsAdmin = false,
                IsHost = false,
                JoinedAt = DateTime.Now,
                ConnectionId = Context.ConnectionId,
                Status = "online",
                Score = 0,
                Combo = 0,
                PassedLevels = 0
            };

            room.Players.Add(player);
            _playerToRoom[playerId] = roomId;
            _connectionToPlayer[Context.ConnectionId] = playerId;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            await Clients.Group(roomId).SendAsync("PlayerJoined", player);
            await Clients.Caller.SendAsync("JoinedRoom", new { success = true, room, player });
            await Clients.Group(roomId).SendAsync("RoomUpdated", room);

            return new { success = true, room, player };
        }

        // ============================================================
        // 准备/取消准备
        // ============================================================
        public async Task ToggleReady(string roomId, string playerId)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return;

            var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null || player.IsHost) return;

            player.IsReady = !player.IsReady;
            await Clients.Group(roomId).SendAsync("PlayerReadyToggled", player);
            await Clients.Group(roomId).SendAsync("RoomUpdated", room);
        }

        // ============================================================
        // 开始游戏（仅主控）
        // ============================================================
       // Hubs/PartyHub.cs
public async Task StartGame(string roomId, string playerId)
{
    if (!_rooms.TryGetValue(roomId, out var room))
    {
        await Clients.Caller.SendAsync("GameStartFailed", "房间不存在");
        return;
    }

    var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
    if (player == null || !player.IsHost)
    {
        await Clients.Caller.SendAsync("GameStartFailed", "只有房主可以开始游戏");
        return;
    }

    var notReady = room.Players.Where(p => !p.IsReady && !p.IsHost).ToList();
    if (notReady.Any())
    {
        await Clients.Caller.SendAsync("GameStartFailed", $"{notReady.Count} 位玩家尚未准备");
        return;
    }

    if (room.Players.Count < room.MinPlayers)
    {
        await Clients.Caller.SendAsync("GameStartFailed", $"至少需要 {room.MinPlayers} 位玩家");
        return;
    }

    room.Status = "playing";
    
    // ⭐ 广播给所有玩家，让他们跳转
    await Clients.Group(roomId).SendAsync("GameStarted", room);
    
    // ⭐ 发送跳转指令给所有玩家
    await Clients.Group(roomId).SendAsync("RedirectToGame", new { 
        roomId = roomId, 
        game = "werewolf",
        url = "/Werewolf/Waiting?roomId=" + roomId
    });
}

        // ============================================================
        // 踢出玩家（仅管理员）
        // ============================================================
        public async Task KickPlayer(string roomId, string adminId, string targetPlayerId)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return;

            var admin = room.Players.FirstOrDefault(p => p.PlayerId == adminId);
            if (admin == null || !admin.IsAdmin) return;

            var target = room.Players.FirstOrDefault(p => p.PlayerId == targetPlayerId);
            if (target == null || target.IsHost) return;

            room.Players.Remove(target);
            _playerToRoom.TryRemove(targetPlayerId, out _);
            _connectionToPlayer.TryRemove(target.ConnectionId!, out _);

            await Clients.Client(target.ConnectionId).SendAsync("Kicked", "您已被管理员移出房间");
            await Clients.Group(roomId).SendAsync("PlayerLeft", targetPlayerId);
            await Clients.Group(roomId).SendAsync("RoomUpdated", room);
        }

        // ============================================================
        // 设置管理员（仅主控）
        // ============================================================
        public async Task ToggleAdmin(string roomId, string hostId, string targetPlayerId)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return;

            var host = room.Players.FirstOrDefault(p => p.PlayerId == hostId);
            if (host == null || !host.IsHost) return;

            var target = room.Players.FirstOrDefault(p => p.PlayerId == targetPlayerId);
            if (target == null || target.IsHost) return;

            target.IsAdmin = !target.IsAdmin;
            if (target.IsAdmin)
            {
                if (!room.AdminUserIds.Contains(target.PlayerId))
                    room.AdminUserIds.Add(target.PlayerId);
            }
            else
            {
                room.AdminUserIds.Remove(target.PlayerId);
            }

            await Clients.Group(roomId).SendAsync("AdminUpdated", target);
            await Clients.Group(roomId).SendAsync("RoomUpdated", room);
        }

        // ============================================================
        // 刷新房间
        // ============================================================
        public async Task RefreshRoom(string roomId)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("RoomUpdated", room);
            }
        }

        // ============================================================
        // 获取房间信息
        // ============================================================
        public async Task<PartyRoom?> GetRoomInfo(string roomId)
        {
            _rooms.TryGetValue(roomId, out var room);
            return room;
        }

        // ============================================================
        // 断开连接
        // ============================================================
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectionToPlayer.TryRemove(Context.ConnectionId, out var playerId))
            {
                if (_playerToRoom.TryRemove(playerId, out var roomId))
                {
                    if (_rooms.TryGetValue(roomId, out var room))
                    {
                        var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
                        if (player != null)
                        {
                            room.Players.Remove(player);
                            await Clients.Group(roomId).SendAsync("PlayerLeft", playerId);
                            await Clients.Group(roomId).SendAsync("RoomUpdated", room);
                        }

                        if (!room.Players.Any())
                        {
                            _rooms.TryRemove(roomId, out _);
                        }
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ============================================================
        // 获取房间（供 Controller 调用）
        // ============================================================
        public static PartyRoom? GetRoom(string roomId)
        {
            _rooms.TryGetValue(roomId, out var room);
            return room;
        }

        public static List<PartyRoom> GetAllRooms()
        {
            return _rooms.Values.ToList();
        }

        // ============================================================
        // 生成房间码
        // ============================================================
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
    }
}
