using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForestFireDetection.Data;
using ForestFireDetection.Models;

namespace ForestFireDetection.Controllers
{
    public class SensorsController : Controller
    {
        private readonly ForestFireDetectionDbContext _context;

        public SensorsController(ForestFireDetectionDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sensors = await _context.Sensors.ToListAsync();

            return View(sensors);
        }

        [HttpGet]
        public async Task<IActionResult> GetSensorData(Guid sensorId)
        {
            var data = await _context.SensorData
                .Where(d => d.SensorId == sensorId)
                .OrderByDescending(d => d.Timestamp)
                .Take(10)
                .OrderBy(d => d.Timestamp) 
                .Select(d => new
                {
                    d.Timestamp,
                    d.Temperature,
                    d.Humidity,
                    d.Smoke
                })
                .ToListAsync();

            return Json(data);
        }

		[HttpGet]
		public async Task<IActionResult> GetSensors()
		{
			var sensors = await _context.Sensors.ToListAsync();
			return Json(sensors);
		}
	}
}
