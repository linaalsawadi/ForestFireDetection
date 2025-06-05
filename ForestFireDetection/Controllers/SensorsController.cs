using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForestFireDetection.Data;
using ForestFireDetection.Models;
using Microsoft.AspNetCore.Authorization;
using ForestFireDetection.ViewModels;
using ForestFireDetection.Models.ViewModels;
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
            return View();
        }

        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> DailyReport()
        {
            return View();
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

        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetMonthlyOverview(string sensorId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var dailySummary = await _context.SensorHourlySummary
                .Where(d => d.SensorId == sensorId &&
                            d.Date >= startDate &&
                            d.Date <= endDate)
                .GroupBy(d => d.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    AvgTemperature = g.Average(x => x.AvgTemperature),
                    AvgHumidity = g.Average(x => x.AvgHumidity),
                    AvgSmoke = g.Average(x => x.AvgSmoke),
                    AvgFireScore = g.Average(x => x.AvgFireScore)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Json(dailySummary);
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetDailyDetail(string sensorId, DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            var data = await _context.SensorHourlySummary
                .Where(d => d.SensorId == sensorId &&
                            d.Date == date.Date)
                .OrderBy(d => d.Hour)
                .Select(d => new
                {
                    Hour = d.Hour,
                    Temperature = d.AvgTemperature,
                    Humidity = d.AvgHumidity,
                    Smoke = d.AvgSmoke,
                    FireScore = d.AvgFireScore
                })
                .ToListAsync();

            return Json(data);
        }


    }
}
