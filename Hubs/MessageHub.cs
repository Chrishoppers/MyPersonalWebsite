using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Hubs
{
    public class MessageHub : Hub
    {
        private static readonly ConcurrentDictionary<string, int> _onlineUsers = new();
        private static readonly ConcurrentDictionary<int, string> _userConnections = new();

        // ===== 在线用户跟踪 =====
        public async Task UserOnline(int userId)
        {
            var connectionId = Context.ConnectionId;

            if (_userConnections.TryGetValue(userId, out var oldConnectionId))
            {
                _onlineUsers.TryRemove(oldConnectionId, out _);
                _userConnections.TryRemove(userId, out _);
            }

            _onlineUsers[connectionId] = userId;
            _userConnections[userId] = connectionId;

            await BroadcastOnlineUsers();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            if (_onlineUsers.TryRemove(connectionId, out var userId))
            {
                _userConnections.TryRemove(userId, out _);
                await BroadcastOnlineUsers();
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<List<int>> GetOnlineUsers()
        {
            return _userConnections.Keys.ToList();
        }

        private async Task BroadcastOnlineUsers()
        {
            var onlineUserIds = _userConnections.Keys.ToList();
            await Clients.All.SendAsync("OnlineUsersUpdated", onlineUserIds);
        }

        // ============================================================
        // ⭐ 恐怖控制方法
        // ============================================================

        // 全部用户
        public async Task TriggerHorror(string message)
        {
            await Clients.All.SendAsync("HorrorInvasion", message);
        }

        // ⭐ 指定单个用户
        public async Task TriggerHorrorToUser(int userId, string message)
        {
            if (_userConnections.TryGetValue(userId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("HorrorInvasion", message);
            }
        }

        // 指定多个用户
        public async Task TriggerHorrorToUsers(List<int> userIds, string message)
        {
            var connectionIds = new List<string>();
            foreach (var id in userIds)
            {
                if (_userConnections.TryGetValue(id, out var connId))
                {
                    connectionIds.Add(connId);
                }
            }
            if (connectionIds.Count > 0)
            {
                await Clients.Clients(connectionIds).SendAsync("HorrorInvasion", message);
            }
        }

        public async Task TriggerGhost(string message)
        {
            await Clients.All.SendAsync("GhostEvent", message);
        }

        public async Task TriggerShake(string intensity)
        {
            await Clients.All.SendAsync("ShakeEvent", intensity);
        }

        // ============================================================
        // 原有方法
        // ============================================================
        public async Task SendNewMessage(string username, string content)
        {
            await Clients.All.SendAsync("ReceiveMessage", username, content);
        }
    }
}
