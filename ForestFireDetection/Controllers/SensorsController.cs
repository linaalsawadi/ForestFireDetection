using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForestFireDetection.Data;
using ForestFireDetection.Models.Enums;
using ForestFireDetection.Models.ViewModels;
using ForestFireDetection.Models.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace ForestFireDetection.Controllers
{
    public class SensorsController : Controller
    {
        private readonly ForestFireDetectionDbContext _context;

        public SensorsController(ForestFireDetectionDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Index()
        {
            var sensors = await _context.Sensors.ToListAsync();

            var latestDataPerSensor = await _context.SensorData
                .GroupBy(d => d.SensorId)
                .Select(g => g.OrderByDescending(d => d.Timestamp).FirstOrDefault())
                .ToListAsync();

            var sensorViewModels = sensors.Select(sensor =>
            {
                var lastData = latestDataPerSensor.FirstOrDefault(d => d != null && d.SensorId == sensor.SensorId);
                return new SensorWithLastDataViewModel
                {
                    Sensor = sensor,
                    LastData = lastData
                };
            }).ToList();

            return View(sensorViewModels);
        }

        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Reports()
        {
            var sensors = await _context.Sensors.ToListAsync();
            return View(sensors);
        }

        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> DailyReport(string sensorId, DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            var data = await _context.SensorData
                .Where(d => d.SensorId == sensorId && d.Timestamp >= start && d.Timestamp < end)
                .Select(d => new SensorDataViewModel
                {
                    Timestamp = d.Timestamp,
                    Temperature = d.Temperature,
                    Humidity = d.Humidity,
                    Smoke = d.Smoke,
                    FireScore = d.FireScore
                })
                .ToListAsync();

            var archive = await _context.SensorDataArchive
                .Where(d => d.SensorId == sensorId && d.Timestamp >= start && d.Timestamp < end)
                .Select(d => new SensorDataViewModel
                {
                    Timestamp = d.Timestamp,
                    Temperature = d.Temperature,
                    Humidity = d.Humidity,
                    Smoke = d.Smoke,
                    FireScore = d.FireScore
                })
                .ToListAsync();

            var allData = data.Concat(archive).OrderBy(d => d.Timestamp).ToList();
            return View(allData);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSensorData(string sensorId)
        {
            var latest = await _context.SensorData
                .Where(d => d.SensorId == sensorId && d.Timestamp <= DateTime.UtcNow)
                .OrderByDescending(d => d.Timestamp)
                .Take(15)
                .ToListAsync();

            var ordered = latest
                .OrderBy(d => d.Timestamp)
                .Select(d => new
                {
                    d.Timestamp,
                    d.Temperature,
                    d.Humidity,
                    d.Smoke,
                    d.FireScore
                })
                .ToList();

            return Json(ordered);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSensors()
        {
            var sensorsWithLastData = await _context.Sensors
                .GroupJoin(
                    _context.SensorData
                        .GroupBy(d => d.SensorId)
                        .Select(g => g.OrderByDescending(d => d.Timestamp).FirstOrDefault()),
                    s => s.SensorId,
                    d => d!.SensorId,
                    (s, dataGroup) => new { Sensor = s, LastData = dataGroup.FirstOrDefault() })
                .ToListAsync();

            var result = sensorsWithLastData.Select(x => new SensorDto
            {
                SensorId = x.Sensor.SensorId,
                SensorState = x.Sensor.SensorState,
                SensorPositioningDate = x.Sensor.SensorPositioningDate,
                SensorDangerSituation = x.Sensor.SensorDangerSituation,
                FireScore = x.LastData?.FireScore
            }).ToList();

            return Json(result);
        }
    }
}
