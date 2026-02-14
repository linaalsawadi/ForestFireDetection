using ForestFireDetection.Data;
using ForestFireDetection.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForestFireDetection.Controllers
{
    public class MapController : Controller
    {
        private readonly ForestFireDetectionDbContext _context;

        public MapController(ForestFireDetectionDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var sensors = await _context.Sensors.ToListAsync();

            var latestDataPerSensor = await _context.SensorData
                .GroupBy(d => d.SensorId)
                .Select(g => g.OrderByDescending(d => d.Timestamp)
                              .Select(d => new
                              {
                                  d.SensorId,
                                  d.Latitude,
                                  d.Longitude,
                                  d.Temperature,
                                  d.Humidity,
                                  d.Smoke,
                                  d.Timestamp
                              })
                              .FirstOrDefault())
                .ToListAsync();

            var sensorViewModels =
                (from s in sensors
                 join d in latestDataPerSensor on s.SensorId equals d!.SensorId
                 select new SensorWithLatestDataViewModel
                 {
                     SensorId = s.SensorId,
                     SensorState = s.SensorState,
                     SensorPositioningDate = s.SensorPositioningDate,
                     SensorDangerSituation = s.SensorDangerSituation,
                     Latitude = d.Latitude,
                     Longitude = d.Longitude,
                     Temperature = d.Temperature,
                     Humidity = d.Humidity,
                     Smoke = d.Smoke,
                     Timestamp = d.Timestamp
                 }).ToList();

            return View(sensorViewModels);
        }
    }
}
