using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ForestFireDetection.Data;
using ForestFireDetection.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ForestFireDetection.Services
{
    public class SensorMonitorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<MapHub> _mapHub;
        private readonly IHubContext<ChartHub> _chartHub;
        private readonly ILogger<SensorMonitorService> _logger;

        private const int CheckIntervalSeconds = 60;
        private const int OfflineThresholdMinutes = 3;

        public SensorMonitorService(
            IServiceScopeFactory scopeFactory,
            IHubContext<MapHub> mapHub,
            IHubContext<ChartHub> chartHub,
            ILogger<SensorMonitorService> logger)
        {
            _scopeFactory = scopeFactory;
            _mapHub = mapHub;
            _chartHub = chartHub;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ForestFireDetectionDbContext>();

                    var sensors = await context.Sensors
                        .Include(s => s.DataHistory.OrderByDescending(d => d.Timestamp).Take(1))
                        .ToListAsync(stoppingToken);

                    bool anyUpdated = false;

                    foreach (var sensor in sensors)
                    {
                        var latestData = sensor.DataHistory.FirstOrDefault();
                        if (latestData == null) continue;

                        var minutesSinceLastUpdate = (DateTime.UtcNow - sensor.SensorPositioningDate).TotalMinutes;
                        bool updated = false;
                        string state = sensor.SensorState;

                        if (minutesSinceLastUpdate > OfflineThresholdMinutes && sensor.SensorState != "offline")
                        {
                            sensor.SensorState = "offline";
                            sensor.SensorDangerSituation = false;
                            state = "offline";
                            updated = true;
                        }
                        else if (minutesSinceLastUpdate <= OfflineThresholdMinutes && sensor.SensorState == "offline")
                        {
                            sensor.SensorState = "green";
                            sensor.SensorDangerSituation = false;
                            state = "green";
                            updated = true;
                        }

                        if (updated)
                        {
                            anyUpdated = true;
                            await _mapHub.Clients.All.SendAsync("UpdateSensor", new
                            {
                                sensorId = sensor.SensorId,
                                temperature = latestData.Temperature,
                                humidity = latestData.Humidity,
                                smoke = latestData.Smoke,
                                latitude = latestData.Latitude,
                                longitude = latestData.Longitude,
                                timestamp = latestData.Timestamp,
                                sensorState = state,
                                fireScore = Math.Round(latestData.FireScore, 2)
                            }, stoppingToken);

                            var greenCount = await context.Sensors.CountAsync(s => s.SensorState == "green", stoppingToken);
                            var yellowCount = await context.Sensors.CountAsync(s => s.SensorState == "yellow", stoppingToken);
                            var redCount = await context.Sensors.CountAsync(s => s.SensorState == "red", stoppingToken);
                            var offlineCount = await context.Sensors.CountAsync(s => s.SensorState == "offline", stoppingToken);

                            await _chartHub.Clients.All.SendAsync("ReceiveSensorData", sensor.SensorId, new
                            {
                                timestamp = latestData.Timestamp,
                                temperature = latestData.Temperature,
                                humidity = latestData.Humidity,
                                smoke = latestData.Smoke
                            }, state, sensor.SensorDangerSituation,
                               greenCount, yellowCount, redCount, offlineCount,
                               sensor.SensorPositioningDate, stoppingToken);
                        }
                    }

                    if (anyUpdated)
                        await context.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SensorMonitorService");
                }

                await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), stoppingToken);
            }
        }
    }
}
