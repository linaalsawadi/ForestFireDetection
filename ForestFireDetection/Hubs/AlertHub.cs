using Microsoft.AspNetCore.SignalR;

namespace ForestFireDetection.Hubs
{
    public class AlertHub : Hub
    {
        public async Task SendAlert(string siteName, string sensorId, double latitude, double longitude)
        {
            await Clients.All.SendAsync("NewAlarm", new
            {
                siteName = siteName,
                sensorId = sensorId,
                latitude = latitude,
                longitude = longitude
            });
        }
    }

}
