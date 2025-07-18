﻿using ForestFireDetection.Data;
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

        private static readonly Dictionary<string, List<SensorData>> _buffer = new();
        private static readonly Dictionary<string, DateTime> _lastAlertTimes = new();
        private static readonly Dictionary<string, DateTime> _fireStartTimes = new();

        private const int BATCH_SIZE = 4;
        private const int ALERT_REPEAT_MINUTES = 5;

        public SensorDataProcessor(
            ForestFireDetectionDbContext context,
            IHubContext<AlertHub> alertHub,
            IHubContext<MapHub> mapHub,
            IHubContext<ChartHub> chartHub)
        {
            _context = context;
            _alertHub = alertHub;
            _mapHub = mapHub;
            _chartHub = chartHub;
        }

        public async Task ProcessAsync(SensorData data)
        {
            // FireScore
            double tempNorm = Math.Min(data.Temperature / 40.0, 1.0);
            double smokeNorm = Math.Min(data.Smoke / 50.0, 1.0); 
            double humNorm = Math.Min(data.Humidity / 100.0, 1.0);

            data.FireScore = (tempNorm * 0.4 + smokeNorm * 0.5 + (1 - humNorm) * 0.3) * 100;
            data.FireScore = Math.Clamp(data.FireScore, 0, 100);

            string state;
            if (data.FireScore >= 75)
                state = "red";
            else if (data.FireScore >= 50)
                state = "yellow";
            else
                state = "green";

            var sensor = await _context.Sensors.FindAsync(data.SensorId);
            if (sensor == null)
            {
                sensor = new Sensor
                {
                    SensorId = data.SensorId,
                    SensorPositioningDate = DateTime.UtcNow,
                    SensorState = state,
                    SensorDangerSituation = (state != "green")
                };
                _context.Sensors.Add(sensor);
            }
            else
            {
                sensor.SensorState = state;
                sensor.SensorDangerSituation = (state != "green");
                sensor.SensorPositioningDate = DateTime.UtcNow;
                _context.Sensors.Update(sensor);
            }

            // ✅ تحديث الخريطة
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

            // ✅ تحديث العدادات
            var greenCount = await _context.Sensors.CountAsync(s => s.SensorState == "green");
            var yellowCount = await _context.Sensors.CountAsync(s => s.SensorState == "yellow");
            var redCount = await _context.Sensors.CountAsync(s => s.SensorState == "red");
            var offlineCount =await _context.Sensors.CountAsync(s => s.SensorState == "offline");

            await _chartHub.Clients.All.SendAsync("ReceiveSensorData", data.SensorId, new
            {
                timestamp = data.Timestamp,
                temperature = data.Temperature,
                humidity = data.Humidity,
                smoke = data.Smoke,
                fireScore = data.FireScore,
            }, state, sensor.SensorDangerSituation, greenCount, yellowCount, redCount, offlineCount, sensor.SensorPositioningDate);


            // ✅ تجميع البيانات
            if (!_buffer.ContainsKey(data.SensorId))
                _buffer[data.SensorId] = new List<SensorData>();

            _buffer[data.SensorId].Add(data);

            if (_buffer[data.SensorId].Count < BATCH_SIZE)
                return;

            // ✅ حساب المتوسط
            var batch = _buffer[data.SensorId];
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

            _buffer[data.SensorId].Clear();

            _context.SensorData.Add(avgData);
            await _context.SaveChangesAsync();


            // ✅ تحقق من وجود حريق فعلي
            bool isRealFire = await IsRealFireAsync(avgData);
            if (!isRealFire) return;

            // ✅ إعادة الإنذار إذا استمر الخطر
            bool shouldSendAlert = !_lastAlertTimes.ContainsKey(avgData.SensorId) ||
                                   (DateTime.UtcNow - _lastAlertTimes[avgData.SensorId]).TotalMinutes >= ALERT_REPEAT_MINUTES;

            if (!shouldSendAlert) return;
            _lastAlertTimes[avgData.SensorId] = DateTime.UtcNow;

            // ✅ حساب مدة استمرار الخطر
            if (!_fireStartTimes.ContainsKey(avgData.SensorId))
                _fireStartTimes[avgData.SensorId] = DateTime.UtcNow;

            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                SensorId = avgData.SensorId,
                Temperature = avgData.Temperature,
                Smoke = avgData.Smoke,
                Humidity = avgData.Humidity,
                Timestamp = DateTime.UtcNow,
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
                Status = alert.Status,
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

            var lastTemp = lastReadings[1].Temperature;
            var tempRise = data.Temperature - lastTemp;

            bool strongSmoke = data.Smoke >= 15;
            bool highTemp = data.Temperature >= 50;
            bool lowHumidity = data.Humidity <= 25;
            bool fastTempRise = tempRise >= 5;

            if (strongSmoke)
                return true;

            int score = 0;
            if (highTemp) score++;
            if (lowHumidity) score++;
            if (fastTempRise) score++;

            return score >= 2;

        }
    }
}
