using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Hubs
{
    public class MessageHub : Hub
    {
        private static readonly ConcurrentDictionary<string, int> _onlineUsers = new();
        private static readonly ConcurrentDictionary<int, string> _userConnections = new();

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

        public async Task TriggerHorror(string message)
        {
            await Clients.All.SendAsync("HorrorInvasion", message);
        }

        public async Task TriggerGhost(string message)
        {
            await Clients.All.SendAsync("GhostEvent", message);
        }

        public async Task TriggerShake(string intensity)
        {
            await Clients.All.SendAsync("ShakeEvent", intensity);
        }

        public async Task SendNewMessage(string username, string content)
        {
            await Clients.All.SendAsync("ReceiveMessage", username, content);
        }
    }
}
