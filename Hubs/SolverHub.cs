using Microsoft.AspNetCore.SignalR;

namespace MyPersonalWebsite.Hubs
{
    public class SolverHub : Hub
    {
        // 服务器向指定客户端发送进度消息（SolverProcessingService 中可以直接调用）
        public async Task SendProgress(string connectionId, string message)
        {
            await Clients.Client(connectionId).SendAsync("SolverProgress", message);
        }

        // 前端调用以获取当前连接 id（用于上传时将 connectionId 一并提交）
        public Task<string> GetConnectionId()
        {
            return Task.FromResult(Context.ConnectionId);
        }
    }
}
