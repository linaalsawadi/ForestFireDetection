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

        private const int CHECK_INTERVAL_SECONDS = 60;
        private const int OFFLINE_THRESHOLD_MINUTES = 3;

        public SensorMonitorService(IServiceScopeFactory scopeFactory, IHubContext<MapHub> mapHub, IHubContext<ChartHub> chartHub)
        {
            _scopeFactory = scopeFactory;
            _mapHub = mapHub;
            _chartHub = chartHub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    bool Updated = false;
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ForestFireDetectionDbContext>();

                    var sensors = await context.Sensors
                        .Include(s => s.DataHistory.OrderByDescending(d => d.Timestamp))
                        .ToListAsync();

                    foreach (var sensor in sensors)
                    {
                        var latestData = sensor.DataHistory.FirstOrDefault();
                        if (latestData == null) continue;
                        Updated = false;

                        var minutesSinceLastUpdate = (DateTime.UtcNow - sensor.SensorPositioningDate).TotalMinutes;

                        string state = sensor.SensorState;

                        if (minutesSinceLastUpdate > OFFLINE_THRESHOLD_MINUTES)
                        {
                            if (sensor.SensorState != "offline")
                            {
                                sensor.SensorState = "offline";
                                sensor.SensorDangerSituation = false;
                                state = "offline";
                                Updated = true;
                            }
                        }
                        else if (sensor.SensorState == "offline")
                        {
                            sensor.SensorState = "green";
                            sensor.SensorDangerSituation = false;
                            state = "green";
                            Updated = true;
                        }
                        await context.SaveChangesAsync();

                        if (Updated)
                        {
                            // ✅ إرسال التحديث إلى الخريطة
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
                                fireScore = Math.Round((latestData.Temperature * 0.4) + (latestData.Smoke * 0.5) - (latestData.Humidity * 0.2), 2)
                            });

                            // ✅ تحديث العدادات بعد كل حساس (حتى لو تكرار بسيط)
                            var greenCount = await context.Sensors.CountAsync(s => s.SensorState == "green");
                            var yellowCount = await context.Sensors.CountAsync(s => s.SensorState == "yellow");
                            var redCount = await context.Sensors.CountAsync(s => s.SensorState == "red");
                            var offlineCount = await context.Sensors.CountAsync(s => s.SensorState == "offline");

                            // ✅ إرسال نفس البيانات إلى المخططات والعدادات
                            await _chartHub.Clients.All.SendAsync("ReceiveSensorData", sensor.SensorId, new
                            {
                                timestamp = latestData.Timestamp,
                                temperature = latestData.Temperature,
                                humidity = latestData.Humidity,
                                smoke = latestData.Smoke
                            }, state, sensor.SensorDangerSituation, greenCount, yellowCount, redCount, offlineCount, sensor.SensorPositioningDate);

                        }
                    }

                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[SensorMonitorService] Error: " + ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(CHECK_INTERVAL_SECONDS), stoppingToken);
            }
        }
    }
}
