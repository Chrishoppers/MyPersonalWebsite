using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Hubs
{
    // 这个类负责向所有连接的客户端发送新留言
    public class MessageHub : Hub
    {
        // 这个方法可以被客户端调用，用来广播新留言
        public async Task SendNewMessage(string username, string content)
        {
            // 向所有客户端（除了自己）发送 "ReceiveMessage" 事件
            await Clients.All.SendAsync("ReceiveMessage", username, content);
        }
    }
}