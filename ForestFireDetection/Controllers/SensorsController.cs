using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForestFireDetection.Data;
using ForestFireDetection.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using ForestFireDetection.Models.ViewModels;
using ForestFireDetection.ViewModels;
using ForestFireDetection.Models.DTOs;

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

            // جلب آخر توقيت لكل SensorId
            var latestDataPerSensor = await _context.SensorData
                .GroupBy(d => d.SensorId)
                .Select(g => g.OrderByDescending(d => d.Timestamp).FirstOrDefault())
                .ToListAsync();

            var sensorViewModels = sensors.Select(sensor =>
            {
                var lastData = latestDataPerSensor.FirstOrDefault(d => d.SensorId == sensor.SensorId);

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

            var allData = data
                .Concat(archive)
                .OrderBy(d => d.Timestamp)
                .ToList();

            return View(allData);
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetSensorData(string sensorId)
        {
            var latest10 = await _context.SensorData
                .Where(d => d.SensorId == sensorId && d.Timestamp <= DateTime.UtcNow)
                .OrderByDescending(d => d.Timestamp)
                .Take(10)
                .ToListAsync();

            var ordered = latest10
                .OrderBy(d => d.Timestamp)
                .Select(d => new
                {
                    d.Timestamp,
                    d.Temperature,
                    d.Humidity,
                    d.Smoke
                })
                .ToList();

            return Json(ordered);
        }


        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetSensors()
        {
            var sensors = await _context.Sensors.ToListAsync();

            var result = new List<SensorDto>();

            foreach (var sensor in sensors)
            {
                var lastData = await _context.SensorData
                    .Where(d => d.SensorId == sensor.SensorId)
                    .OrderByDescending(d => d.Timestamp)
                    .FirstOrDefaultAsync();

                result.Add(new SensorDto
                {
                    SensorId = sensor.SensorId,
                    SensorState = sensor.SensorState,
                    SensorPositioningDate = sensor.SensorPositioningDate,
                    SensorDangerSituation = sensor.SensorDangerSituation,
                    FireScore = lastData?.FireScore
                });
            }

            return Json(result);
        }


    }
}
