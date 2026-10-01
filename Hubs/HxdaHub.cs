using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace MyPersonalWebsite.Hubs
{
    public class HxdaHub : Hub
    {
        public async Task JoinPlayer(string playerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "player_" + playerId);
        }

        public async Task JoinAdmin()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        }

        public async Task Report(object data)
        {
            await Clients.Group("admins").SendAsync("PlayerReport", data);
        }

        public async Task Heartbeat(string playerId)
        {
            await Clients.Group("admins").SendAsync("PlayerHeartbeat", playerId);
        }
    }
}