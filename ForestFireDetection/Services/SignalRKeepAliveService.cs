using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using ForestFireDetection.Hubs;

namespace ForestFireDetection.Services
{
    public class SignalRKeepAliveService : BackgroundService
    {
        private readonly IHubContext<AlertHub> _alertHub;
        private readonly IHubContext<ChartHub> _chartHub;
        private readonly IHubContext<MapHub> _mapHub;

        public SignalRKeepAliveService(
            IHubContext<AlertHub> alertHub,
            IHubContext<ChartHub> chartHub,
            IHubContext<MapHub> mapHub)
        {
            _alertHub = alertHub;
            _chartHub = chartHub;
            _mapHub = mapHub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;

                    await _alertHub.Clients.All.SendAsync("KeepAlive", now, cancellationToken: stoppingToken);
                    await _chartHub.Clients.All.SendAsync("KeepAlive", now, cancellationToken: stoppingToken);
                    await _mapHub.Clients.All.SendAsync("KeepAlive", now, cancellationToken: stoppingToken);

                    Console.WriteLine($"[KeepAlive] Sent to all hubs at {now:HH:mm:ss}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[KeepAlive] Error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
