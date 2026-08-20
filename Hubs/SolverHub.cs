using Microsoft.AspNetCore.SignalR;

namespace MyPersonalWebsite.Hubs
{
    public class SolverHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // send the connection id back to the caller so the client can include it in uploads
            await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public async Task SendProgress(string connectionId, string message)
        {
            await Clients.Client(connectionId).SendAsync("SolverProgress", message);
        }
    }
}
