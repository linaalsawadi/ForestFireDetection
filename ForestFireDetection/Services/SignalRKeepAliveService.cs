using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using ForestFireDetection.Hubs;
using Microsoft.Extensions.Logging;

namespace ForestFireDetection.Services
{
    public class SignalRKeepAliveService : BackgroundService
    {
        private readonly IHubContext<AlertHub> _alertHub;
        private readonly IHubContext<ChartHub> _chartHub;
        private readonly IHubContext<MapHub> _mapHub;
        private readonly ILogger<SignalRKeepAliveService> _logger;

        public SignalRKeepAliveService(
            IHubContext<AlertHub> alertHub,
            IHubContext<ChartHub> chartHub,
            IHubContext<MapHub> mapHub,
            ILogger<SignalRKeepAliveService> logger)
        {
            _alertHub = alertHub;
            _chartHub = chartHub;
            _mapHub = mapHub;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    await Task.WhenAll(
                        _alertHub.Clients.All.SendAsync("KeepAlive", now, stoppingToken),
                        _chartHub.Clients.All.SendAsync("KeepAlive", now, stoppingToken),
                        _mapHub.Clients.All.SendAsync("KeepAlive", now, stoppingToken)
                    );
                    _logger.LogDebug("KeepAlive sent at {Time:HH:mm:ss}", now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "KeepAlive error");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
