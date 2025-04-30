using ForestFireDetection.Data;
using ForestFireDetection.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ForestFireDetection.Controllers
{
    public class MapController : Controller
    {
        private readonly ForestFireDetectionDbContext _context;

        public MapController(ForestFireDetectionDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var sensors = _context.Sensors.ToList();

            var sensorData = _context.SensorData
                .GroupBy(d => d.SensorId)
                .Select(g => g.OrderByDescending(d => d.Timestamp).FirstOrDefault())
                .ToList();

            var sensorViewModels = (from s in sensors
                                    join d in sensorData on s.SensorId equals d.SensorId
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
