using Microsoft.AspNetCore.SignalR;

namespace ForestFireDetection.Hubs
{
    public class AlertHub : Hub
    {
        public async Task SimulateAlert(object alert)
        {
            await Clients.All.SendAsync("NewAlert", alert);
        }
    }

}
