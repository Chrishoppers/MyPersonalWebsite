using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Hubs
{
    public class MessageHub : Hub
    {
        // 存储在线用户：ConnectionId -> UserId
        private static readonly ConcurrentDictionary<string, int> _onlineUsers = new();
        private static readonly ConcurrentDictionary<int, string> _userConnections = new();

        // 用户加入
        public async Task UserOnline(int userId)
        {
            var connectionId = Context.ConnectionId;

            // 如果用户已有其他连接，移除旧的
            if (_userConnections.TryGetValue(userId, out var oldConnectionId))
            {
                _onlineUsers.TryRemove(oldConnectionId, out _);
                _userConnections.TryRemove(userId, out _);
            }

            _onlineUsers[connectionId] = userId;
            _userConnections[userId] = connectionId;

            // 广播在线用户列表给管理员
            await BroadcastOnlineUsers();
        }

        // 用户离线
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

        // 获取在线用户列表
        public async Task<List<int>> GetOnlineUsers()
        {
            return _userConnections.Keys.ToList();
        }

        // 广播在线用户给所有管理员
        private async Task BroadcastOnlineUsers()
        {
            var onlineUserIds = _userConnections.Keys.ToList();
            await Clients.All.SendAsync("OnlineUsersUpdated", onlineUserIds);
        }

        // 原有发送新留言的方法
        public async Task SendNewMessage(string username, string content)
        {
            await Clients.All.SendAsync("ReceiveMessage", username, content);
        }
    }
}
