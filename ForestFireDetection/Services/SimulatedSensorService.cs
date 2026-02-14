using ForestFireDetection.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ForestFireDetection.Models;

namespace ForestFireDetection.Services
{
    /// <summary>
    /// Temporary service that simulates sensor data arriving continuously for 10 minutes.
    /// Sends data every 15 seconds for each sensor to mimic real MQTT data flow.
    /// DELETE THIS FILE when testing is complete.
    /// </summary>
    public class SimulatedSensorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SimulatedSensorService> _logger;

        private static readonly TimeSpan TotalDuration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan SendInterval = TimeSpan.FromSeconds(15);

        private static readonly (string Id, double Lat, double Lng)[] Sensors =
        [
            ("SNS-001", 40.7350, 31.6100),
            ("SNS-002", 40.7380, 31.6150),
            ("SNS-003", 40.7410, 31.6200),
            ("SNS-004", 40.7440, 31.6250),
            ("SNS-005", 40.7470, 31.6300),
            ("SNS-006", 40.7500, 31.6350),
        ];

        public SimulatedSensorService(
            IServiceScopeFactory scopeFactory,
            ILogger<SimulatedSensorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning("?? SimulatedSensorService STARTED — will send data for {Minutes} minutes", TotalDuration.TotalMinutes);

            var startTime = DateTime.UtcNow;
            var random = new Random();
            int cycle = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                var elapsed = DateTime.UtcNow - startTime;
                if (elapsed >= TotalDuration)
                {
                    _logger.LogWarning("? SimulatedSensorService COMPLETED — 10 minutes elapsed. Stopping simulation.");
                    break;
                }

                cycle++;
                // Progress through phases to create a realistic scenario
                double progress = elapsed.TotalMinutes / TotalDuration.TotalMinutes; // 0.0 ? 1.0

                foreach (var (sensorId, lat, lng) in Sensors)
                {
                    try
                    {
                        var data = GenerateSensorData(sensorId, lat, lng, random, progress, cycle);

                        using var scope = _scopeFactory.CreateScope();
                        var processor = scope.ServiceProvider.GetRequiredService<SensorDataProcessor>();
                        await processor.ProcessAsync(data);

                        _logger.LogInformation(
                            "?? [{Cycle}] {Sensor}: Temp={Temp:F1}°C, Hum={Hum:F1}%, Smoke={Smoke:F1}, Score={Score:F1}",
                            cycle, sensorId, data.Temperature, data.Humidity, data.Smoke, data.FireScore);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending simulated data for {Sensor}", sensorId);
                    }
                }

                var remaining = TotalDuration - elapsed;
                _logger.LogInformation("?? Cycle {Cycle} done. Remaining: {Min:F1} min", cycle, remaining.TotalMinutes);

                await Task.Delay(SendInterval, stoppingToken);
            }

            _logger.LogWarning("?? SimulatedSensorService STOPPED");
        }

        /// <summary>
        /// Data generation with AGGRESSIVE fire scenarios to trigger alerts quickly.
        /// 
        /// IsRealFireAsync requires ONE of:
        ///   • smoke >= 15  ? instant alert
        ///   • 2+ of: temp >= 50, humidity <= 25, tempRise >= 5
        /// 
        /// BatchSize = 4, so alerts fire every 4th reading (every ~60s).
        /// AlertRepeatMinutes = 5, so same sensor re-alerts every 5 min.
        /// 
        /// Timeline:
        ///   Cycle 1–4  (0:00–1:00): SNS-004 ramps 30?65°C, smoke 20?50 ? FIRST ALERT ~1 min
        ///   Cycle 1–4  (0:00–1:00): SNS-003 ramps 28?55°C, smoke 18?35 ? FIRST ALERT ~1 min
        ///   Cycle 5–8  (1:00–2:00): Both stay critical ? second alert batch
        ///   Cycle 9+   (2:00+):     Sustained fire with variation
        ///   Last 20%   (8:00–10:00): Cooling phase
        /// </summary>
        private static SensorData GenerateSensorData(
            string sensorId, double lat, double lng,
            Random random, double progress, int cycle)
        {
            float temp, humidity, smoke;
            double jitterLat = (random.NextDouble() - 0.5) * 0.0005;
            double jitterLng = (random.NextDouble() - 0.5) * 0.0005;

            switch (sensorId)
            {
                // ???????????????????????????????????????????????????
                // SNS-004: PRIMARY FIRE — starts dangerous IMMEDIATELY
                // ???????????????????????????????????????????????????
                case "SNS-004":
                    if (cycle <= 4)
                    {
                        // Rapid escalation: cycle 1?4 ramps up fast
                        // Each cycle adds ~8°C temp and ~8 smoke
                        float ramp = cycle / 4f; // 0.25 ? 1.0
                        temp = 30 + (ramp * 35); // 30 ? 65°C
                        humidity = 25 - (ramp * 12); // 25 ? 13%
                        smoke = 20 + (ramp * 30); // 20 ? 50 (well above 15 threshold)
                        temp += (float)(random.NextDouble() * 3);
                        smoke += (float)(random.NextDouble() * 5);
                    }
                    else if (progress < 0.8)
                    {
                        // Sustained critical fire
                        temp = 60 + (float)(random.NextDouble() * 15); // 60–75°C
                        humidity = 8 + (float)(random.NextDouble() * 7); // 8–15%
                        smoke = 45 + (float)(random.NextDouble() * 20); // 45–65
                    }
                    else
                    {
                        // Cooling down
                        float cool = (float)((progress - 0.8) / 0.2); // 0?1
                        temp = 60 - (cool * 30) + (float)(random.NextDouble() * 5); // 60?30
                        humidity = 15 + (cool * 40) + (float)(random.NextDouble() * 10); // 15?55
                        smoke = 45 - (cool * 35) + (float)(random.NextDouble() * 5); // 45?10
                    }
                    break;

                // ???????????????????????????????????????????????????
                // SNS-003: SECONDARY FIRE — also starts immediately
                // ???????????????????????????????????????????????????
                case "SNS-003":
                    if (cycle <= 4)
                    {
                        float ramp = cycle / 4f;
                        temp = 28 + (ramp * 27); // 28 ? 55°C
                        humidity = 28 - (ramp * 8); // 28 ? 20%
                        smoke = 18 + (ramp * 17); // 18 ? 35 (above 15)
                        temp += (float)(random.NextDouble() * 3);
                        smoke += (float)(random.NextDouble() * 4);
                    }
                    else if (progress < 0.7)
                    {
                        // Sustained warning/critical
                        temp = 50 + (float)(random.NextDouble() * 10); // 50–60°C
                        humidity = 18 + (float)(random.NextDouble() * 8); // 18–26%
                        smoke = 25 + (float)(random.NextDouble() * 15); // 25–40
                    }
                    else
                    {
                        float cool = (float)((progress - 0.7) / 0.3);
                        temp = 50 - (cool * 22) + (float)(random.NextDouble() * 4); // 50?28
                        humidity = 20 + (cool * 35) + (float)(random.NextDouble() * 8); // 20?55
                        smoke = 25 - (cool * 20) + (float)(random.NextDouble() * 3); // 25?5
                    }
                    break;

                // ???????????????????????????????????????????????????
                // SNS-002: DELAYED FIRE — joins at ~40% progress (~4 min)
                // ???????????????????????????????????????????????????
                case "SNS-002":
                    if (progress < 0.4)
                    {
                        // Normal
                        temp = 22 + (float)(random.NextDouble() * 6);
                        humidity = 55 + (float)(random.NextDouble() * 15);
                        smoke = (float)(random.NextDouble() * 3);
                    }
                    else if (progress < 0.75)
                    {
                        // Fire spreads here
                        float fireProgress = (float)((progress - 0.4) / 0.35); // 0?1
                        temp = 30 + (fireProgress * 25) + (float)(random.NextDouble() * 5); // 30?55
                        humidity = 30 - (fireProgress * 12) + (float)(random.NextDouble() * 5); // 30?18
                        smoke = 16 + (fireProgress * 20) + (float)(random.NextDouble() * 5); // 16?36
                    }
                    else
                    {
                        float cool = (float)((progress - 0.75) / 0.25);
                        temp = 52 - (cool * 25) + (float)(random.NextDouble() * 4);
                        humidity = 22 + (cool * 35) + (float)(random.NextDouble() * 8);
                        smoke = 30 - (cool * 25) + (float)(random.NextDouble() * 3);
                    }
                    break;

                // ???????????????????????????????????????????????????
                // SNS-006: Offline ? comes online at 50% ? brief spike
                // ???????????????????????????????????????????????????
                case "SNS-006":
                    if (progress < 0.5)
                    {
                        temp = 20 + (float)(random.NextDouble() * 3);
                        humidity = 65 + (float)(random.NextDouble() * 10);
                        smoke = (float)(random.NextDouble() * 1);
                    }
                    else if (progress < 0.65)
                    {
                        // Brief danger spike when it comes back online
                        float spike = (float)((progress - 0.5) / 0.15);
                        temp = 25 + (spike * 30) + (float)(random.NextDouble() * 4);
                        humidity = 30 - (spike * 10) + (float)(random.NextDouble() * 5);
                        smoke = 5 + (spike * 20) + (float)(random.NextDouble() * 5); // crosses 15
                    }
                    else
                    {
                        // Back to normal
                        temp = 24 + (float)(random.NextDouble() * 5);
                        humidity = 58 + (float)(random.NextDouble() * 12);
                        smoke = 2 + (float)(random.NextDouble() * 3);
                    }
                    break;

                // ???????????????????????????????????????????????????
                // SNS-001, SNS-005: Always safe (green baseline)
                // ???????????????????????????????????????????????????
                default:
                    temp = 20 + (float)(random.NextDouble() * 10);
                    humidity = 50 + (float)(random.NextDouble() * 25);
                    smoke = (float)(random.NextDouble() * 4);
                    break;
            }

            temp = Math.Clamp(temp, 10f, 80f);
            humidity = Math.Clamp(humidity, 5f, 100f);
            smoke = Math.Clamp(smoke, 0f, 70f);

            return new SensorData
            {
                Id = Guid.NewGuid(),
                SensorId = sensorId,
                Latitude = lat + jitterLat,
                Longitude = lng + jitterLng,
                Temperature = MathF.Round(temp, 1),
                Humidity = MathF.Round(humidity, 1),
                Smoke = MathF.Round(smoke, 1),
                Timestamp = DateTime.UtcNow,
            };
        }
    }
}