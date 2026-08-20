using Microsoft.AspNetCore.SignalR;

namespace MyPersonalWebsite.Hubs
{
    public class SolverHub : Hub
    {
        public async Task SendProgress(string connectionId, string message)
        {
            await Clients.Client(connectionId).SendAsync("SolverProgress", message);
        }
    }
}
