using System.Collections.Concurrent;
using ForestFireDetection.Data;
using ForestFireDetection.Models;
using ForestFireDetection.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ForestFireDetection.Services
{
    public class SensorDataProcessor
    {
        private readonly ForestFireDetectionDbContext _context;
        private readonly IHubContext<AlertHub> _alertHub;
        private readonly IHubContext<MapHub> _mapHub;
        private readonly IHubContext<ChartHub> _chartHub;
        private readonly FuzzyEngine _fuzzyEngine;

        private static readonly ConcurrentDictionary<string, List<SensorData>> _buffer = new();
        private static readonly ConcurrentDictionary<string, DateTime> _lastAlertTimes = new();
        private static readonly ConcurrentDictionary<string, DateTime> _fireStartTimes = new();

        private const int BatchSize = 4;
        private const int AlertRepeatMinutes = 5;

        public SensorDataProcessor(
            ForestFireDetectionDbContext context,
            IHubContext<AlertHub> alertHub,
            IHubContext<MapHub> mapHub,
            IHubContext<ChartHub> chartHub,
            FuzzyEngine fuzzyEngine)
        {
            _context = context;
            _alertHub = alertHub;
            _mapHub = mapHub;
            _chartHub = chartHub;
            _fuzzyEngine = fuzzyEngine;
        }

        public async Task ProcessAsync(SensorData data)
        {
            data.FireScore = Math.Clamp(
                _fuzzyEngine.ComputeFireScore(data.Temperature, data.Humidity, data.Smoke), 0, 100);

            string state = data.FireScore switch
            {
                >= 75 => "red",
                >= 50 => "yellow",
                _ => "green"
            };

            var sensor = await _context.Sensors.FindAsync(data.SensorId);
            if (sensor == null)
            {
                sensor = new Sensor
                {
                    SensorId = data.SensorId,
                    SensorPositioningDate = DateTime.UtcNow,
                    SensorState = state,
                    SensorDangerSituation = state != "green"
                };
                _context.Sensors.Add(sensor);
            }
            else
            {
                sensor.SensorState = state;
                sensor.SensorDangerSituation = state != "green";
                sensor.SensorPositioningDate = DateTime.UtcNow;
            }

            await _mapHub.Clients.All.SendAsync("UpdateSensor", new
            {
                sensorId = data.SensorId,
                temperature = data.Temperature,
                humidity = data.Humidity,
                smoke = data.Smoke,
                latitude = data.Latitude,
                longitude = data.Longitude,
                timestamp = data.Timestamp,
                sensorState = state,
            });

            var greenCount = await _context.Sensors.CountAsync(s => s.SensorState == "green");
            var yellowCount = await _context.Sensors.CountAsync(s => s.SensorState == "yellow");
            var redCount = await _context.Sensors.CountAsync(s => s.SensorState == "red");
            var offlineCount = await _context.Sensors.CountAsync(s => s.SensorState == "offline");

            await _chartHub.Clients.All.SendAsync("ReceiveSensorData", data.SensorId, new
            {
                timestamp = data.Timestamp,
                temperature = data.Temperature,
                humidity = data.Humidity,
                smoke = data.Smoke,
                fireScore = data.FireScore,
            }, state, sensor.SensorDangerSituation,
               greenCount, yellowCount, redCount, offlineCount,
               sensor.SensorPositioningDate);

            // Buffer data for batch averaging
            var buffer = _buffer.GetOrAdd(data.SensorId, _ => new List<SensorData>());
            lock (buffer)
            {
                buffer.Add(data);
                if (buffer.Count < BatchSize)
                    return;
            }

            List<SensorData> batch;
            lock (buffer)
            {
                batch = new List<SensorData>(buffer);
                buffer.Clear();
            }

            var avgData = new SensorData
            {
                Id = Guid.NewGuid(),
                SensorId = data.SensorId,
                Timestamp = DateTime.UtcNow,
                Latitude = data.Latitude,
                Longitude = data.Longitude,
                Temperature = batch.Average(d => d.Temperature),
                Humidity = batch.Average(d => d.Humidity),
                Smoke = batch.Average(d => d.Smoke),
                FireScore = Math.Round(data.FireScore, 2)
            };

            _context.SensorData.Add(avgData);
            await _context.SaveChangesAsync();

            if (!await IsRealFireAsync(avgData))
                return;

            var now = DateTime.UtcNow;
            bool shouldSendAlert = !_lastAlertTimes.TryGetValue(avgData.SensorId, out var lastAlert) ||
                                   (now - lastAlert).TotalMinutes >= AlertRepeatMinutes;

            if (!shouldSendAlert) return;
            _lastAlertTimes[avgData.SensorId] = now;
            _fireStartTimes.TryAdd(avgData.SensorId, now);

            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                SensorId = avgData.SensorId,
                Temperature = avgData.Temperature,
                Smoke = avgData.Smoke,
                Humidity = avgData.Humidity,
                Timestamp = now,
                Latitude = avgData.Latitude,
                Longitude = avgData.Longitude,
                Status = "NotReviewed",
                FireScore = avgData.FireScore,
            };

            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();

            var notReviewedCount = await _context.Alerts.CountAsync(a => a.Status == "NotReviewed");
            await _alertHub.Clients.All.SendAsync("UpdateAlertCount", notReviewedCount);
            await _alertHub.Clients.All.SendAsync("NewAlert", new
            {
                alert.Id,
                alert.SensorId,
                alert.Temperature,
                alert.Smoke,
                alert.Humidity,
                alert.Timestamp,
                alert.Latitude,
                alert.Longitude,
                alert.Status,
                FireScore = Math.Round(alert.FireScore, 2),
            });
        }

        private async Task<bool> IsRealFireAsync(SensorData data)
        {
            var lastReadings = await _context.SensorData
                .Where(d => d.SensorId == data.SensorId)
                .OrderByDescending(d => d.Timestamp)
                .Take(3)
                .ToListAsync();

            if (lastReadings.Count < 2) return false;

            var tempRise = data.Temperature - lastReadings[1].Temperature;

            if (data.Smoke >= 15) return true;

            int score = 0;
            if (data.Temperature >= 50) score++;
            if (data.Humidity <= 25) score++;
            if (tempRise >= 5) score++;

            return score >= 2;
        }
    }
}
